"""AntiBacteria sound effects and placeholder music, synthesized (the 2013 game shipped with no audio at all).
Chiptune-style to match the 3-colour pixel look: square / triangle / noise voices, pitch slides, short envelopes.
Usage: python3 audio_synth.py <out_dir>   -> <out_dir>/Sfx/sfx_*.wav and <out_dir>/Music/music_*.wav
Every sound is a few lines below; tweak and re-run, Unity re-imports the WAVs in place."""
import math, os, random, struct, sys, wave

SR = 44100
OUT = sys.argv[1]
rnd = random.Random(7)

# ------------------------------------------------------------------ voices
def square(phase, duty=0.5): return 1.0 if (phase % 1.0) < duty else -1.0
def triangle(phase): p = phase % 1.0; return 4 * p - 1 if p < 0.5 else 3 - 4 * p
def saw(phase): return 2 * (phase % 1.0) - 1
def sine(phase): return math.sin(2 * math.pi * phase)

class Noise:
    """Sample-and-hold noise: rate decides the pitch of the hiss (low = rumble)."""
    def __init__(self, seed=1): self.r = random.Random(seed); self.v = 0.0; self.acc = 1.0
    def __call__(self, rate):
        self.acc += rate / SR
        if self.acc >= 1.0: self.acc %= 1.0; self.v = self.r.uniform(-1, 1)
        return self.v

def tone(dur, f0, f1=None, wave='square', duty=0.5, vol=0.5, attack=0.002, curve=1.0, vib=0.0, vib_rate=0.0):
    """One voice sliding from f0 to f1 with a linear attack and a power-curve decay."""
    f1 = f0 if f1 is None else f1
    n = int(dur * SR); out = []; ph = 0.0
    for i in range(n):
        t = i / n
        f = f0 * (f1 / f0) ** t if f0 > 0 and f1 > 0 else f0 + (f1 - f0) * t
        if vib: f *= 1 + vib * math.sin(2 * math.pi * vib_rate * i / SR)
        ph += f / SR
        s = {'square': lambda: square(ph, duty), 'triangle': lambda: triangle(ph), 'saw': lambda: saw(ph),
             'sine': lambda: sine(ph)}[wave]()
        env = min(1.0, i / max(1, attack * SR)) * (1 - t) ** curve
        out.append(s * env * vol)
    return out

def noise(dur, rate0, rate1=None, vol=0.5, attack=0.001, curve=1.0, seed=1):
    rate1 = rate0 if rate1 is None else rate1
    n = int(dur * SR); nz = Noise(seed); out = []
    for i in range(n):
        t = i / n
        env = min(1.0, i / max(1, attack * SR)) * (1 - t) ** curve
        out.append(nz(rate0 + (rate1 - rate0) * t) * env * vol)
    return out

def mix(*parts, offsets=None):
    offsets = offsets or [0.0] * len(parts)
    n = max(int(o * SR) + len(p) for p, o in zip(parts, offsets))
    out = [0.0] * n
    for p, o in zip(parts, offsets):
        k = int(o * SR)
        for i, s in enumerate(p): out[k + i] += s
    return out

def seq(*parts, gap=0.0):
    out = []
    for p in parts: out += p + [0.0] * int(gap * SR)
    return out

def lowpass(x, cutoff):
    a = 1 - math.exp(-2 * math.pi * cutoff / SR); y = 0.0; out = []
    for s in x: y += a * (s - y); out.append(y)
    return out

def crush(x, levels=32):
    return [round(s * levels) / levels for s in x]

def write(path, x, peak=0.9):
    m = max(1e-9, max(abs(s) for s in x))
    g = min(1.0, peak / m)
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with wave.open(path, 'wb') as w:
        w.setnchannels(1); w.setsampwidth(2); w.setframerate(SR)
        w.writeframes(b''.join(struct.pack('<h', int(max(-1, min(1, s * g)) * 32767)) for s in x))

# ------------------------------------------------------------------ sound effects
SFX = {
    # the player's gun: short, bright, fired 10x a second, so quiet and tiny (pitch varies in the game)
    'shoot': lambda: crush(mix(tone(0.055, 1250, 520, 'square', 0.25, 0.35, curve=1.6),
                               noise(0.02, 9000, vol=0.12))),
    # Circle / Windmill volleys: a thicker thump
    'shoot_heavy': lambda: crush(mix(tone(0.12, 420, 140, 'square', 0.5, 0.45, curve=1.4),
                                     noise(0.06, 5000, 1500, vol=0.25))),
    'enemy_hit': lambda: crush(mix(noise(0.035, 12000, 4000, vol=0.45, curve=1.5),
                                   tone(0.035, 1400, 700, 'square', 0.25, 0.25))),
    # hit on a shield / body that takes no damage: a metallic tink
    'shield_hit': lambda: mix(tone(0.07, 2100, 1900, 'triangle', vol=0.4, curve=2.0),
                              tone(0.05, 3150, None, 'triangle', vol=0.15, curve=2.5)),
    'enemy_kill': lambda: crush(mix(lowpass(noise(0.22, 7000, 900, vol=0.7, curve=1.3, seed=3), 5000),
                                    tone(0.18, 520, 70, 'square', 0.5, 0.35, curve=1.2))),
    'boss_kill': lambda: mix(lowpass(noise(1.6, 4000, 120, vol=0.9, curve=1.1, seed=9), 2500),
                             tone(1.4, 90, 30, 'sine', vol=0.8, curve=0.8),
                             tone(0.5, 600, 60, 'square', 0.5, 0.3, curve=1.0),
                             offsets=[0, 0, 0.05]),
    'wall_hit': lambda: crush(noise(0.03, 3000, 1000, vol=0.35, curve=2.0, seed=5)),
    'bullet_cancel': lambda: crush(tone(0.06, 700, 1500, 'square', 0.25, 0.3, curve=1.5)),
    'enemy_shoot': lambda: crush(tone(0.09, 330, 190, 'square', 0.5, 0.35, curve=1.2)),
    'player_hurt': lambda: crush(mix(tone(0.32, 480, 90, 'saw', vol=0.5, curve=0.9, vib=0.08, vib_rate=30),
                                     noise(0.15, 6000, 800, vol=0.35))),
    'player_die': lambda: crush(mix(tone(0.9, 700, 45, 'square', 0.5, 0.45, curve=0.8, vib=0.05, vib_rate=12),
                                    lowpass(noise(0.9, 5000, 200, vol=0.5, curve=1.0, seed=4), 3000))),
    'vitamin': lambda: tone(0.06, 1568, 2093, 'square', 0.25, 0.25, curve=1.4),
    'bonus_pickup': lambda: seq(*[tone(0.055, f, None, 'square', 0.25, 0.35, curve=0.6) for f in (523, 659, 784, 1047, 1319)]),
    'boom': lambda: mix(lowpass(noise(0.9, 6000, 150, vol=1.0, curve=1.2, seed=11), 3000),
                        tone(0.8, 70, 28, 'sine', vol=0.9, curve=0.9),
                        tone(0.25, 300, 60, 'square', 0.5, 0.4)),
    'slowmo_on': lambda: lowpass(tone(0.45, 900, 160, 'saw', vol=0.5, curve=0.7), 2500),
    'slowmo_off': lambda: lowpass(tone(0.35, 160, 900, 'saw', vol=0.5, curve=0.7), 2500),
    'get_ready': lambda: seq(tone(0.09, 660, None, 'square', 0.5, 0.35, curve=0.4),
                             tone(0.09, 660, None, 'square', 0.5, 0.35, curve=0.4),
                             tone(0.2, 988, None, 'square', 0.5, 0.35, curve=0.8), gap=0.06),
    'level_win': lambda: seq(*[tone(d, f, None, 'square', 0.5, 0.35, curve=0.5) for f, d in
                              ((523, 0.1), (659, 0.1), (784, 0.1), (1047, 0.35))], gap=0.02),
    'game_over': lambda: seq(*[tone(d, f, None, 'triangle', vol=0.6, curve=0.6, vib=0.01, vib_rate=6) for f, d in
                              ((392, 0.22), (330, 0.22), (262, 0.22), (196, 0.7))], gap=0.03),
    'boss_appear': lambda: mix(lowpass(noise(1.8, 300, 1200, vol=0.6, attack=1.2, curve=0.3, seed=21), 900),
                               tone(1.8, 55, 49, 'saw', vol=0.5, attack=0.8, curve=0.6)),
    'boss_shield': lambda: crush(tone(0.6, 280, 980, 'square', 0.5, 0.35, curve=0.5, vib=0.12, vib_rate=24)),
    'ui_move': lambda: tone(0.03, 1100, None, 'square', 0.5, 0.22, curve=1.5),
    'ui_confirm': lambda: seq(tone(0.04, 880, None, 'square', 0.5, 0.3, curve=0.8), tone(0.06, 1320, None, 'square', 0.5, 0.3)),
    'ui_back': lambda: seq(tone(0.04, 880, None, 'square', 0.5, 0.3, curve=0.8), tone(0.06, 587, None, 'square', 0.5, 0.3)),
    # A bonus has appeared: a bright up-and-down sparkle, unlike the pickup arpeggio.
    'bonus_appear': lambda: mix(seq(*[tone(0.05, f, None, 'triangle', vol=0.4, curve=0.8) for f in (1568, 2093, 2637, 2093, 3136)]),
                                tone(0.4, 784, 1568, 'square', 0.125, 0.12, curve=1.6)),
    'ui_error': lambda: tone(0.16, 150, 120, 'square', 0.5, 0.4, curve=0.6),
}

# ------------------------------------------------------------------ placeholder music
NOTE = {n: i for i, n in enumerate(['C', 'C#', 'D', 'D#', 'E', 'F', 'F#', 'G', 'G#', 'A', 'A#', 'B'])}
def hz(name):  # 'A3', 'C#4'
    return 440.0 * 2 ** ((NOTE[name[:-1]] + 12 * (int(name[-1]) + 1) - 69) / 12)

def render_track(bpm, bars, layers, seconds_per_bar=None):
    """layers: list of (events, voice) with events = [(beat, beats, note_or_None, vol)]. Loops cleanly:
    anything that rings past the end is folded back to the start."""
    beat = 60.0 / bpm
    total = int(bars * 4 * beat * SR)
    out = [0.0] * total
    for events, voice in layers:
        for b, length, note, vol in events:
            s = voice(length * beat, note, vol)
            k = int(b * beat * SR)
            for i, v in enumerate(s): out[(k + i) % total] += v
    return out

def v_bass(d, n, vol): return tone(d * 0.95, hz(n), None, 'square', 0.5, vol, attack=0.004, curve=0.6)
def v_lead(d, n, vol): return tone(d * 0.9, hz(n), None, 'square', 0.25, vol, attack=0.004, curve=0.9, vib=0.004, vib_rate=5)
def v_pad(d, n, vol): return tone(d, hz(n), None, 'triangle', vol=vol, attack=0.05, curve=0.4)
def v_kick(d, n, vol): return tone(0.16, 140, 40, 'sine', vol=vol, curve=1.2)
def v_snare(d, n, vol): return noise(0.12, 9000, 3000, vol=vol, curve=1.4, seed=13)
def v_hat(d, n, vol): return noise(0.03, 14000, vol=vol, curve=2.0, seed=17)

def drums(bars, kick, snare, hat, kv=0.7, sv=0.35, hv=0.12):
    ev_k, ev_s, ev_h = [], [], []
    for bar in range(bars):
        for x in kick: ev_k.append((bar * 4 + x, 0.25, None, kv))
        for x in snare: ev_s.append((bar * 4 + x, 0.25, None, sv))
        for x in hat: ev_h.append((bar * 4 + x, 0.25, None, hv))
    return [(ev_k, v_kick), (ev_s, v_snare), (ev_h, v_hat)]

def arp(chords, bars_per_chord, step, pattern, vol, octave_shift=0):
    ev = []; t = 0.0
    for chord in chords:
        notes = chord.split()
        n = int(bars_per_chord * 4 / step)
        for i in range(n):
            nm = notes[pattern[i % len(pattern)] % len(notes)]
            if octave_shift: nm = nm[:-1] + str(int(nm[-1]) + octave_shift)
            ev.append((t + i * step, step, nm, vol))
        t += bars_per_chord * 4
    return ev

def bassline(roots, bars_per_root, step, vol, octave_jumps=True):
    ev = []; t = 0.0
    for r in roots:
        n = int(bars_per_root * 4 / step)
        for i in range(n):
            nm = r if not (octave_jumps and i % 2) else r[:-1] + str(int(r[-1]) + 1)
            ev.append((t + i * step, step, nm, vol))
        t += bars_per_root * 4
    return ev

def v_pluck(d, n, vol): return tone(min(d * 2.5, 1.2), hz(n), None, 'triangle', vol=vol, attack=0.006, curve=3.0)
def v_softpad(d, n, vol):
    a = tone(d, hz(n), None, 'sine', vol=vol, attack=min(0.6, d * 0.3), curve=0.7)
    b = tone(d, hz(n) * 1.003, None, 'triangle', vol=vol * 0.35, attack=min(0.6, d * 0.3), curve=0.7)
    return [x + y for x, y in zip(a, b)]
def v_subbass(d, n, vol): return tone(d * 0.98, hz(n), None, 'sine', vol=vol, attack=0.02, curve=0.8)
def v_bell(d, n, vol):
    a = tone(max(d, 1.6), hz(n), None, 'sine', vol=vol, attack=0.004, curve=2.2)
    b = tone(max(d, 1.6), hz(n) * 2, None, 'sine', vol=vol * 0.25, attack=0.004, curve=3.5)
    return [x + y for x, y in zip(a, b)]

def echo(x, delay_s, feedback, wet):
    """Circular feedback delay, so the loop point stays seamless."""
    d = int(delay_s * SR); n = len(x); buf = [0.0] * n
    for _ in range(2):  # second pass lets the tail wrap around the loop point
        for i in range(n):
            buf[i] = x[i] + feedback * buf[(i - d) % n]
    return [a + wet * (b - a) for a, b in zip(x, buf)]

def menu_track():
    """Menu: calm A minor, 80 bpm, 32 bars (~96 s) so it doesn't repeat every few seconds.
    Bars 1-8 sparse, 9-16 full arpeggio, 17-24 adds a bell melody, 25-32 thins out and turns back to Am.
    Soft voices only (sine / triangle), no drums, gentle echo and a low-pass on top: present but not pushy."""
    bpm, bars = 80, 32
    beat = 60.0 / bpm
    prog = ['A3 C4 E4', 'F3 A3 C4', 'C4 E4 G4', 'G3 B3 D4']
    last = ['A3 C4 E4', 'F3 A3 C4', 'C4 E4 G4', 'E3 G#3 B3']   # E major turnaround into the loop
    chords = prog * 3 + last
    roots = ['A2', 'F2', 'C3', 'G2'] * 3 + ['A2', 'F2', 'C3', 'E2']

    pad = [(i * 8, 8, n, 0.05) for i, c in enumerate(chords) for n in c.split()]
    bass = []
    for i, r in enumerate(roots):
        bass += [(i * 8, 3.5, r, 0.20), (i * 8 + 4, 3.5, r, 0.15)]
    arp_ev = []
    full = [0, 1, 2, 1, 2, 0, 1, 2]            # eighth notes
    sparse = [0, None, 2, None, 1, None, None, None]
    for i, c in enumerate(chords):
        notes = c.split()
        bar = i * 2
        pattern = full if 4 <= bar < 24 else sparse
        for k in range(16):                    # 2 bars of eighths
            step = pattern[k % 8]
            if step is None: continue
            nm = notes[step]; nm = nm[:-1] + str(int(nm[-1]) + 1)   # an octave up, under the echo
            vol = 0.07 if k % 2 == 0 else 0.05
            arp_ev.append((i * 8 + k * 0.5, 0.5, nm, vol))
    mel = []
    phrases = {16: [(0, 'E5'), (1.5, 'D5'), (3, 'C5')], 18: [(0, 'A4'), (2, 'C5')],
               20: [(0, 'G4'), (1.5, 'E4'), (3, 'G4')], 22: [(0, 'D5'), (2, 'B4')],
               24: [(0, 'E5'), (3, 'C5')], 26: [(0, 'A4')], 28: [(0, 'G4'), (2, 'E4')], 30: [(0, 'B4'), (2, 'G#4')]}
    for bar, notes in phrases.items():
        for t, n in notes: mel.append((bar * 4 + t, 1.5, n, 0.09))

    base = render_track(bpm, bars, [(pad, v_softpad), (bass, v_subbass)])
    arps = echo(render_track(bpm, bars, [(arp_ev, v_pluck)]), beat * 0.75, 0.38, 0.35)
    bell = echo(render_track(bpm, bars, [(mel, v_bell)]), beat * 1.5, 0.3, 0.3)
    out = [a + b + c for a, b, c in zip(base, arps, bell)]
    return [v * 1.3 for v in lowpass(out + out, 3200)[len(out):]]  # filter state carried over the loop point

def music():
    tracks = {}
    tracks['menu'] = menu_track()
    # Level: driving 128 bpm, octave bass, square arpeggio lead, four-on-the-floor
    ch = ['A3 C4 E4 A4', 'F3 A3 C4 F4', 'D3 F3 A3 D4', 'E3 G#3 B3 E4'] * 2
    tracks['level'] = render_track(128, 16, [
        (bassline(['A2', 'F2', 'D2', 'E2'] * 2, 2, 0.5, 0.2), v_bass),
        (arp(ch, 2, 0.25, [0, 1, 2, 3, 2, 1, 2, 3], 0.09, 1), v_lead),
    ] + drums(16, [0, 1, 2, 3], [1, 3], [0.5, 1.5, 2.5, 3.5]))
    # Boss: 140 bpm, E phrygian, half-step menace, busier drums
    ch = ['E3 G3 B3', 'F3 A3 C4', 'E3 G3 B3', 'D3 F3 A3'] * 2
    tracks['boss'] = render_track(140, 16, [
        (bassline(['E2', 'F2', 'E2', 'D2'] * 2, 2, 0.25, 0.2), v_bass),
        (arp(ch, 2, 0.25, [0, 2, 1, 2, 0, 2, 1, 0], 0.08, 1), v_lead),
    ] + drums(16, [0, 0.75, 2, 2.5], [1, 3], [0.5, 1.5, 2.5, 3.5, 3.75], kv=0.8, sv=0.4))
    return tracks

if __name__ == '__main__':
    for name, make in SFX.items():
        write(f'{OUT}/Sfx/sfx_{name}.wav', make())
    for name, x in music().items():
        write(f'{OUT}/Music/music_{name}.wav', x, peak=0.8)
    print(len(SFX), 'sfx, 3 music loops')
