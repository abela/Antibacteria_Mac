"""
AntiBacteria intro - soundtrack for the animatic, synthesized (chip-flavoured, like the game's own audio).
Music bed (A minor), ambience and sound effects placed on the animatic's exact shot timings, then muxed onto the
video. Usage: python3 intro_audio.py <video in> <video out>
"""
import math, random, sys, subprocess, importlib.util
import numpy as np
from scipy.signal import lfilter, fftconvolve
from scipy.io import wavfile

SR = 44100
sys_argv = sys.argv[:]
sys.argv = ["x"]
spec = importlib.util.spec_from_file_location("a3", "/home/claude/intro/animatic3.py")
A3 = importlib.util.module_from_spec(spec); spec.loader.exec_module(A3)
sys.argv = sys_argv

# shot starts, as the animatic cuts them (whole frames at 15 fps)
STARTS, t = [], 0.0
for sh in A3.SHOTS:
    STARTS.append(t)
    t += int(sh[0] * A3.FPS) / A3.FPS
FLASH = STARTS[-1] + 2.4
TOTAL = FLASH + 1.8
N = int(TOTAL * SR)
NIGHT, LAB, SCOPE, EUREKA, SYRINGE, INJECT, REACT, DIVE = STARTS

music = np.zeros(N); sfx = np.zeros(N); amb = np.zeros(N)
rng = np.random.default_rng(7)


def midi(n):
    return 440.0 * 2 ** ((n - 69) / 12)


def tt(dur):
    return np.arange(int(dur * SR)) / SR


def osc(kind, f, dur, duty=0.5, f_end=None):
    n = int(dur * SR)
    fr = np.full(n, f) if f_end is None else np.geomspace(f, f_end, n)
    ph = np.cumsum(fr) / SR
    if kind == "square": return np.where(ph % 1 < duty, 1.0, -1.0)
    if kind == "tri": return 4 * np.abs(ph % 1 - 0.5) - 1
    if kind == "saw": return 2 * (ph % 1) - 1
    if kind == "sine": return np.sin(2 * np.pi * ph)
    raise ValueError(kind)


def noise(dur):
    return rng.uniform(-1, 1, int(dur * SR))


def env(n, a=0.01, d=0.1, s=0.7, r=0.2):
    a_, d_, r_ = int(a * SR), int(d * SR), int(r * SR)
    e = np.full(n, float(s))
    if a_: e[:min(a_, n)] = np.linspace(0, 1, a_)[:n]
    if d_ and a_ < n: e[a_:a_ + d_] = np.linspace(1, s, d_)[:max(0, min(d_, n - a_))]
    if r_: e[-min(r_, n):] *= np.linspace(1, 0, min(r_, n))
    return e


def lp(x, cutoff):
    a = 1 - math.exp(-2 * math.pi * cutoff / SR)
    return lfilter([a], [1, a - 1], x)


def hp(x, cutoff):
    return x - lp(x, cutoff)


def put(bus, at, x, gain=1.0):
    i = int(at * SR)
    if i >= len(bus) or i + len(x) <= 0: return
    j = max(0, -i); x = x[j:]; i = max(0, i)
    n = min(len(x), len(bus) - i)
    bus[i:i + n] += x[:n] * gain


# ------------------------------------------------------------------ instruments

def pad(at, notes, dur, gain=0.08, cutoff=1400, attack=1.2, release=1.5):
    out = np.zeros(int(dur * SR))
    for k, n in enumerate(notes):
        f = midi(n)
        for det in (-0.12, 0.12):  # two detuned pulses per note: a soft chorus
            out += osc("square", f * 2 ** (det / 12), dur, duty=0.25 + 0.1 * k % 0.2)
        out += osc("tri", f, dur) * 0.8
    out = lp(out / (len(notes) * 2.8), cutoff)
    put(music, at, out * env(len(out), attack, 0.1, 1.0, release), gain)


def bell(at, n, dur=2.4, gain=0.10):
    x = osc("tri", midi(n), dur) * 0.7 + osc("sine", midi(n) * 2.0, dur) * 0.3
    put(music, at, x * np.exp(-tt(dur) * 2.2) * env(len(x), 0.005, 0, 1, 0.05), gain)


def pluck(bus, at, n, dur=0.18, gain=0.07, duty=0.125):
    x = osc("square", midi(n), dur, duty=duty)
    put(bus, at, lp(x, 3500) * np.exp(-tt(dur) * 14), gain)


def bass(at, n, dur=0.3, gain=0.18):
    x = osc("tri", midi(n), dur)
    put(music, at, x * env(len(x), 0.005, 0.05, 0.8, 0.08), gain)


def arpeggio(at, end, notes, step, gain=0.05, duty=0.125):
    k, t_ = 0, at
    while t_ < end:
        pluck(music, t_, notes[k % len(notes)], dur=step * 1.6, gain=gain, duty=duty)
        k += 1; t_ += step


# ------------------------------------------------------------------ sound effects

def thunder(at, gain=0.5):
    x = lp(noise(3.2), 220) * np.exp(-tt(3.2) * 1.4) * env(int(3.2 * SR), 0.25, 0, 1, 0.5)
    put(amb, at, x, gain)


def tick(at, gain=0.12):
    x = hp(noise(0.02), 2500) * np.exp(-tt(0.02) * 300)
    put(sfx, at, x, gain)


def blip(at, gain=0.035, n=84):
    x = osc("square", midi(n), 0.025, duty=0.25) * np.exp(-tt(0.025) * 120)
    put(sfx, at, x, gain)


def plip(at, gain=0.25):  # a drop landing
    x = osc("sine", 1500, 0.18, f_end=420) * np.exp(-tt(0.18) * 22)
    put(sfx, at, x, gain)


def pop(at, gain=0.12, pitch=1.0):
    x = (osc("square", 260 * pitch, 0.07, duty=0.3, f_end=70 * pitch) + lp(noise(0.07), 3000) * 0.6) * np.exp(-tt(0.07) * 45)
    put(sfx, at, x, gain)


def click(at, gain=0.2):
    x = lp(noise(0.03), 2500) * np.exp(-tt(0.03) * 160) + osc("square", 900, 0.03) * np.exp(-tt(0.03) * 200) * 0.3
    put(sfx, at, x, gain)


def glass(at, gain=0.12):
    x = sum(osc("sine", f, 1.0) * a for f, a in ((2630, 1), (4120, 0.6), (5400, 0.35))) * np.exp(-tt(1.0) * 6)
    put(sfx, at, x, gain)


def whoosh(at, dur, gain=0.3, lo=300, hi=3000, rev=False):
    n = int(dur * SR)
    x = noise(dur)
    cut = np.geomspace(lo, hi, n) if not rev else np.geomspace(hi, lo, n)
    # time-varying lowpass, in blocks
    out = np.zeros(n); blk = 512; state = 0.0
    for i in range(0, n, blk):
        a = 1 - math.exp(-2 * math.pi * cut[i] / SR)
        seg = x[i:i + blk]
        y, zf = lfilter([a], [1, a - 1], seg, zi=[state * (1 - a)])
        out[i:i + blk] = y; state = y[-1]
    put(sfx, at, out * np.sin(np.linspace(0, np.pi, n)) ** 1.5, gain)


def impact(at, gain=0.6):
    d = 2.2
    boom = osc("sine", 90, d, f_end=32) * np.exp(-tt(d) * 2.2)
    crack = lp(noise(d), 4000) * np.exp(-tt(d) * 9)
    put(sfx, at, boom * 0.9 + crack * 0.5, gain)


def heartbeat(at, gain=0.5, muffled=False):
    def thump(f0, dur):
        return osc("sine", f0, dur, f_end=f0 * 0.55) * np.exp(-tt(dur) * 18)
    x = thump(75, 0.22)
    y = thump(62, 0.2)
    put(sfx, at, lp(x, 400 if muffled else 900), gain)
    put(sfx, at + 0.17, lp(y, 400 if muffled else 900), gain * 0.7)


def beep(at, gain=0.06):
    x = osc("square", 1046, 0.09, duty=0.5) * env(int(0.09 * SR), 0.002, 0.01, 0.8, 0.02)
    put(sfx, at, x, gain)


def liquid_draw(at, dur, gain=0.08):
    x = osc("tri", 220, dur, f_end=660) * (0.6 + 0.4 * np.sin(2 * np.pi * 9 * tt(dur)))
    put(sfx, at, lp(x, 1800) * env(len(x), 0.2, 0, 1, 0.3), gain)
    for k in range(7):  # bubbles in the vial
        put(sfx, at + 0.3 + k * dur / 8 + random.uniform(-0.05, 0.05),
            osc("sine", random.uniform(500, 900), 0.06, f_end=random.uniform(1200, 1800)) * np.exp(-tt(0.06) * 40), gain * 1.2)


def riser(at, dur, f0, f1, gain=0.08):
    x = osc("saw", f0, dur, f_end=f1) + osc("saw", f0 * 1.5, dur, f_end=f1 * 1.5) * 0.5
    put(music, at, lp(x, 2200) * np.linspace(0, 1, len(x)) ** 2, gain)


def tinnitus(at, dur, gain=0.03):
    x = osc("sine", 5200, dur) * env(int(dur * SR), dur * 0.5, 0, 1, dur * 0.3)
    put(sfx, at, x, gain)


# ------------------------------------------------------------------ the score

# 1. Night: rain, distant thunder, a lonely pad and a few bells
amb_rain = hp(lp(noise(LAB + 0.8), 5000), 600) * 0.6
put(amb, 0, 0.7 * amb_rain * np.concatenate([np.linspace(0, 1, SR), np.ones(len(amb_rain) - 2 * SR), np.linspace(1, 0, SR)]), 0.10)
thunder(0.4, 0.3); thunder(3.4, 0.2)
pad(0.0, [45, 52, 57, 59, 60], SCOPE + 0.5, gain=0.045, cutoff=1100, attack=2.5)   # Am(add9)
put(music, 0, osc("tri", midi(33), SCOPE) * env(int(SCOPE * SR), 3, 0, 1, 1.5), 0.03)  # A1 drone
for at, n in ((1.8, 76), (3.6, 72), (6.2, 71), (8.4, 69)):
    bell(at, n)

# 2. The lab: lamp hum, clock, bubbling flask
hum = osc("sine", 60, SCOPE - LAB) * 0.6 + osc("sine", 120, SCOPE - LAB) * 0.3
put(amb, LAB, hum * env(len(hum), 0.4, 0, 1, 0.4), 0.015)
for k in range(5):
    tick(LAB + 0.2 + k, 0.10)
for k in range(9):
    put(sfx, LAB + 0.4 + k * 0.5 + random.uniform(0, 0.2),
        osc("sine", random.uniform(300, 600), 0.05, f_end=900) * np.exp(-tt(0.05) * 50), 0.05)

# 3. The microscope: focus click, the drop, bacteria popping as the cure spreads, a rising shimmer
click(SCOPE + 0.15, 0.18)
pad(SCOPE, [41, 53, 60, 65, 67], 2.9, gain=0.08, cutoff=1300)                     # F(add9)
pad(SCOPE + 2.8, [43, 55, 62, 64, 71], EUREKA - SCOPE - 2.6, gain=0.08, cutoff=1600)  # G6 lifting
plip(SCOPE + 1.4, 0.3)
rnd = random.Random(3)
cx, cy, R = 160, 84, 70
for _ in range(16):
    x, y, a, ln, sd = cx + rnd.uniform(-55, 55), cy + rnd.uniform(-55, 55), rnd.uniform(0, math.tau), rnd.uniform(8, 14), rnd.random() * 6
    if (x - cx) ** 2 + (y - cy) ** 2 > (R - 12) ** 2:
        continue
    ka = 1.4 + 0.4 + math.hypot(x - cx, y - cy) / 20
    if ka < EUREKA - SCOPE:
        pop(SCOPE + ka, 0.10, pitch=random.uniform(0.8, 1.3))
arpeggio(SCOPE + 1.6, EUREKA, [69, 72, 76, 79, 81, 79, 76, 72], 0.15, gain=0.035)

# 4. The realisation: a bright rising sting, a glass clink as he lifts the vial, the close-up glows
for k, n in enumerate((72, 76, 79, 84, 88)):
    pluck(music, EUREKA + 0.12 + k * 0.06, n, dur=0.6, gain=0.08, duty=0.25)
pad(EUREKA, [48, 64, 67, 72, 76], SYRINGE - EUREKA + 0.4, gain=0.10, cutoff=2400, attack=0.3)  # C major
glass(EUREKA + 1.3, 0.10)
for k in range(10):
    bell(EUREKA + 1.75 + k * 0.12, (84, 88, 91, 96)[k % 4], dur=0.8, gain=0.035)

# 5. The syringe: a needle into the cap, the cure drawing up, tension creeps in
click(SYRINGE + 0.1, 0.12)
liquid_draw(SYRINGE + 0.3, 3.0, 0.07)
pad(SYRINGE, [50, 65, 69, 74], 2.3, gain=0.09, cutoff=1200)                       # Dm
pad(SYRINGE + 2.1, [40, 56, 59, 64], INJECT - SYRINGE - 1.9, gain=0.09, cutoff=1500)  # E (dominant)
for k in range(int((INJECT - SYRINGE) / 0.4)):
    bass(SYRINGE + k * 0.4, 38 if k < 6 else 40, 0.3, 0.12)

# 6. The injection: a riser, the prick, the white flash hits, then the cure rushes up the vein
riser(INJECT, 1.6, 110, 440, 0.07)
pad(INJECT, [40, 41, 46, 52], 1.65, gain=0.07, cutoff=900, attack=0.8, release=0.05)   # E-F-Bb cluster
click(INJECT + 1.3, 0.25)
impact(INJECT + 1.6, 0.6)
whoosh(INJECT + 1.75, 1.6, 0.18, lo=200, hi=2500)
put(music, INJECT + 1.6, osc("tri", midi(33), REACT - INJECT) * env(int((REACT - INJECT) * SR), 0.2, 0, 1, 0.3), 0.12)

# 7. The reaction: the heart speeds up, the monitor beeps with it, a ringing in the ears, the lamp buzzes
beats = [0.1, 0.9, 1.25, 1.58, 1.88]
tl = 2.1
REACT_LEN = DIVE - REACT
while tl < REACT_LEN:
    beats.append(tl)
    tl += max(0.3, 0.55 - (tl - 2.1) * 0.08)
for b in beats:
    heartbeat(REACT + b, 0.55)
    if b > 1.8:
        beep(REACT + b + 0.02, 0.05)
tinnitus(REACT + 0.6, DIVE - REACT - 0.4, 0.025)
buzz = osc("saw", 120, DIVE - REACT - 1.8) * (rng.random(int((DIVE - REACT - 1.8) * SR)) > 0.0)
gate = np.repeat((np.random.default_rng(3).random(int((DIVE - REACT - 1.8) * 12) + 1) > 0.3).astype(float), SR // 12 + 1)[:len(buzz)]
put(amb, REACT + 1.8, lp(buzz, 800) * gate, 0.03)
put(music, REACT, lp(osc("saw", midi(33), DIVE - REACT), 300) * env(int((DIVE - REACT) * SR), 1.5, 0, 1, 0.3), 0.08)

# 8. The dive: a rushing tunnel with a muffled heart, the flash, the arena forms, the stinger
whoosh(DIVE, 2.3, 0.35, lo=150, hi=4000)
for k in range(6):
    heartbeat(DIVE + 0.1 + k * 0.38, 0.35, muffled=True)
for k in range(8):  # red cells whipping past
    whoosh(DIVE + 0.2 + k * 0.26, 0.25, 0.06, lo=800, hi=3000, rev=True)
impact(DIVE + 2.2, 0.5)
# ------------------------------------------------------------------ stems
ir_t = tt(1.8)
ir = rng.uniform(-1, 1, len(ir_t)) * np.exp(-ir_t * 3.2); ir[0] = 0
mus = music + fftconvolve(music * 0.8, ir)[:N] * 0.05
fx = sfx + amb + fftconvolve(sfx * 0.5, ir)[:N] * 0.05
tail = np.ones(N); i0 = int((FLASH + 0.2) * SR)
tail[i0:] = np.linspace(1, 0, N - i0) ** 1.5
mus *= tail; fx *= tail
both = mus + fx
peak = np.max(np.abs(np.tanh(both * 1.6) / np.tanh(1.6)))
g = 0.89 / max(1e-9, np.max(np.abs(both)))
def write(name, x):
    x = np.clip(x * g, -1, 1)
    st = np.stack([x, np.concatenate([[0] * 220, x[:-220]])], axis=1)
    wavfile.write("/home/claude/intro/export/" + name, SR, (st * 32767).astype(np.int16))
write("intro_music.wav", mus); write("intro_sfx.wav", fx)
print("stems", round(N / SR, 2), "s, flash at", round(FLASH, 3))

# the final hit, played by Level 1 as it fades in from white: walls rumble, a hit, the big chord, the pulse starts
N2 = int(3.4 * SR)
music = np.zeros(N2); sfx = np.zeros(N2); amb = np.zeros(N2)
N = N2
whoosh(0.0, 0.8, 0.15, lo=200, hi=900, rev=True)
impact(0.15, 0.3)
pad(0.15, [45, 57, 60, 64, 69, 76], 3.2, gain=0.12, cutoff=3000, attack=0.02, release=1.4)
for n in (57, 64, 69):
    put(music, 0.15, lp(osc("saw", midi(n), 1.2), 2500) * np.exp(-tt(1.2) * 2.5), 0.06)
for k in range(8):
    bass(0.4 + k * 0.25, (45, 45, 52, 45)[k % 4], 0.2, 0.16 * (1 - k / 9))
sting = music + sfx + fftconvolve((music + sfx) * 0.6, ir)[:N2] * 0.05
sting[-int(1.2 * SR):] *= np.linspace(1, 0, int(1.2 * SR))
sting = sting * g
st = np.stack([sting, np.concatenate([[0] * 220, sting[:-220]])], axis=1)
wavfile.write("/home/claude/intro/export/intro_sting.wav", SR, (np.clip(st, -1, 1) * 32767).astype(np.int16))
print("sting ok, peak", round(float(np.max(np.abs(sting))), 3))
