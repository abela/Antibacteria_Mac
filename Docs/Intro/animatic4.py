"""
AntiBacteria intro - storyboard v4: no cure, no injection. The doctor calls it a day, leaves a sample under the
microscope, and in the dark one cell in it wakes up (the player). Reuses v3's shaded night, lab and doctor.

Shots: night city -> the doctor at work -> he stands, pulls the lamp chain, walks out, the door closes ->
the microscope alone in the moonlight -> through the eyepiece: one cell wakes, the bacteria turn on it,
the camera rushes in, white flash (the game loads Level 1 there).

usage: python3 animatic4.py [stills]
"""
import math, random, os, subprocess, sys, importlib.util
import numpy as np
from PIL import Image, ImageDraw

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)  # animatic3 imports animatic2 from this folder
_argv = sys.argv[:]
sys.argv = ["x"]
spec = importlib.util.spec_from_file_location("a3", os.path.join(HERE, "animatic3.py"))
A3 = importlib.util.module_from_spec(spec); spec.loader.exec_module(A3)
sys.argv = _argv
v2 = A3.v2

W, H, FPS, SCALE = A3.W, A3.H, A3.FPS, A3.SCALE
BG, FG, RED, DIM = A3.BG, A3.FG, A3.RED, A3.DIM
XX, YY = A3.XX, A3.YY
POSES, lerp_pose, ease, Shade = A3.POSES, A3.lerp_pose, A3.ease, A3.Shade
OUT = os.path.join(HERE, "out4")
os.makedirs(OUT + "/frames", exist_ok=True)


def clamp01(x):
    return max(0.0, min(1.0, x))


# ------------------------------------------------------------------ 3. he calls it a day

LAMP_OFF = 2.35          # the chain is pulled
WALK0, WALK1 = 2.7, 5.0  # walking out to the right
DOOR_OPEN, DOOR_SHUT = 4.5, 5.75


def walk_pose(ph):
    s = math.sin(ph)
    return dict(torso=4, head=6,
                fa=(180 - 22 * s, 172 - 22 * s), ba=(180 + 22 * s, 176 + 22 * s),
                fl=(180 + 24 * s, 186 + 24 * s + 8 * max(0.0, -s)),
                bl=(180 - 24 * s, 186 - 24 * s + 8 * max(0.0, s)))


def door_light(img, k):
    """The corridor light through the door on the right, k = how open (0..1)."""
    if k <= 0:
        return
    d = ImageDraw.Draw(img)
    x0 = W - 2 - 22 * k
    d.rectangle([x0, 56, W, 150], fill=FG)
    d.polygon([(x0, 150), (W, 150), (W, H), (x0 - 70 * k, H)], fill=DIM)
    v2.dither(img, np.clip(1 - (W - XX) / (60 * k + 1), 0, 1) * (YY > 150) * 0.8, FG)


def s_leave(img, t, dur):
    on = t < LAMP_OFF or (LAMP_OFF + 0.08 < t < LAMP_OFF + 0.16)   # one dying flicker
    ambient = 0.05 if on else 0.075                                  # moonlight once it's off
    # the doctor: up from the bench, a long stretch towards the lamp chain, then out
    if t < 1.1:
        k = ease(clamp01((t - 0.25) / 0.85))
        pose, hip = lerp_pose(POSES["hunch"], POSES["stand"], k), (114 + 10 * k, 100 - 1 * k)
    elif t < 2.2:
        k = ease(clamp01((t - 1.1) / 0.7))
        pose, hip = lerp_pose(POSES["stand"], POSES["raise"], k), (124 + 6 * k, 99)
    elif t < WALK0:
        k = ease(clamp01((t - LAMP_OFF) / 0.35))
        pose, hip = lerp_pose(POSES["raise"], POSES["stand"], k), (130, 99)
    else:
        u = (t - WALK0) / (WALK1 - WALK0)
        ph = (t - WALK0) * 2 * math.pi * 1.25
        pose = walk_pose(ph)
        hip = (130 + u * 215, 99 - abs(math.sin(ph)) * 1.5)

    S = Shade(ambient=ambient)
    lx, glass = A3.lab(S, t, swing=0.6 if t < LAMP_OFF else 0.15, flicker=1.0 if on else 0.0)
    S.render(img, black=0.035 if on else 0.0)
    A3.red_details(img, glass)
    d = ImageDraw.Draw(img)
    # the lamp's pull chain (jerks down when pulled)
    pull = 3 if LAMP_OFF - 0.1 < t < LAMP_OFF + 0.2 else 0
    cx = lx - 8
    for y in range(31, 41 + pull, 2):
        d.point((cx, y), fill=DIM if not on else FG)
    d.rectangle([cx - 1, 41 + pull, cx + 1, 43 + pull], fill=DIM if not on else FG)
    if not on:
        # moonlight through the blinds: stripes across the floor and the bench
        stripes = ((XX * 0.55 + YY) // 5 % 2 == 0) & (YY > 108) & (XX > 20) & (XX < 240)
        v2.dither(img, stripes * 0.22, DIM)
        if int(t * 2) % 2 == 0:
            d.point((152, 106), fill=RED)     # the microscope's standby light
    door_k = 0.0
    if DOOR_OPEN < t < DOOR_SHUT:
        door_k = ease(clamp01((t - DOOR_OPEN) / 0.3)) * (1 - ease(clamp01((t - (DOOR_SHUT - 0.45)) / 0.45)))
    door_light(img, door_k)
    S2 = Shade(ambient=ambient, light=(lx, 30, 40))
    S2.key = 2.0 if on else 0.0
    S2.falloff = 60
    J = A3.doctor(S2, pose, hip, breathe=math.sin(t * 2) * 0.6 if t < 0.3 else 0.0)
    S2.render(img, black=0.035 if on else 0.0, rim=True)
    if on:
        A3.doctor_details(img, J)
    elif door_k > 0:   # a silhouette against the corridor light
        a = np.asarray(img).copy()
        a[S2.cov] = BG
        img.paste(Image.fromarray(a))


# ------------------------------------------------------------------ 4. the sample, left behind

def big_microscope(S, x, y):
    """A side-view microscope about 115 px tall: base, C-arm, stage with a slide, nosepiece, tube, eyepiece."""
    m = 0.5
    S.rect(x - 34, y - 7, x + 42, y, m, normal=(0, -1, 0.45))                  # base
    S.capsule((x - 34, y - 3), (x + 42, y - 3), 3.5, m * 0.9)
    S.tube([(x + 32, y - 7, 7.5), (x + 37, y - 44, 6.5), (x + 30, y - 82, 5.5), (x + 14, y - 94, 4.5)], m)   # arm
    S.sphere((x + 36, y - 30), 7, 7, 0.75)                                      # focus knob
    S.sphere((x + 36, y - 30), 3, 3, 0.4)
    S.rect(x - 30, y - 47, x + 30, y - 42, 0.65, normal=(0, -1, 0.5))           # stage
    S.capsule((x - 4, y - 41), (x - 4, y - 33), 4, 0.35)                        # condenser under it
    S.rect(x - 26, y - 49, x + 16, y - 47, 0.95, normal=(0, -1, 0.8))           # the glass slide
    S.rect(x - 28, y - 51, x - 20, y - 49, 0.8)                                 # stage clips
    S.rect(x + 12, y - 51, x + 20, y - 49, 0.8)
    S.sphere((x - 2, y - 68), 10, 5, 0.6)                                       # nosepiece
    S.capsule((x - 4, y - 66), (x - 4, y - 56), 3, 0.55, rb=2.2)                # objectives
    S.capsule((x + 4, y - 66), (x + 9, y - 58), 2.5, 0.45, rb=1.8)
    S.capsule((x - 10, y - 66), (x - 15, y - 59), 2.5, 0.45, rb=1.8)
    S.capsule((x + 2, y - 72), (x - 18, y - 106), 5.5, m, rb=4.8)              # body tube
    S.capsule((x - 18, y - 106), (x - 23, y - 116), 3.8, 0.3)                   # eyepiece
    return (x - 4, y - 50)                                                      # where the sample sits


def s_left(img, t, dur):
    LIGHT = (20, -40, 150)
    S = Shade(light=LIGHT, falloff=500, ambient=0.02, key=0.9)
    S.rect(0, 150, W, 156, 0.9, normal=(0, -1, 0.35))                   # bench top edge
    S.render(img, rim=False)
    # moonlight through the blinds: a slanted patch of slats on the dark wall
    u = (XX - 30) - (YY - 20) * 0.35
    patch = (u > 0) & (u < 150) & (YY > 14) & (YY < 132) & ((YY // 3) % 3 != 0)
    soft = np.clip(1 - np.abs(u - 75) / 90, 0, 1)
    v2.dither(img, patch * soft * 0.55, DIM)
    S2 = Shade(light=LIGHT, falloff=500, ambient=0.07, key=2.1)
    sx, sy = big_microscope(S2, 140, 152)
    S2.sphere((228, 150), 30, 6.5, 0.7)                                 # a petri dish beside it
    S2.sphere((228, 148.5), 26.5, 4.5, 0.2)
    S2.render(img, rim=True)
    d = ImageDraw.Draw(img)
    rnd = random.Random(8)
    for _ in range(10):                                                 # specks in the dish
        d.point((228 + rnd.randint(-20, 20), 148 + rnd.randint(-2, 2)), fill=RED)
    # specks on the slide; one of them twitches, and a faint glow gathers under the objective
    for k, px in enumerate((sx - 18, sx - 10, sx - 1, sx + 7, sx + 15)):
        dx = (1 if (k == 2 and t > 2.4 and int(t * 6) % 3 == 0) else 0)
        d.point((px + dx, sy), fill=RED)
    if t > 2.4:
        glow = 0.3 + 0.2 * math.sin(t * 5)
        r = np.hypot(XX - (sx - 1), (YY - sy) * 2.2)
        v2.dither(img, np.clip(1 - r / 13, 0, 1) * glow * (YY < sy) * (YY > sy - 12), FG)
    if int(t * 2) % 2 == 0:
        d.point((140 - 30, 152 - 4), fill=RED)                          # standby light on the base
    # slow push in towards the slide
    z = 1 + ease(t / dur) * 0.45
    cx, cy = sx, sy
    box = (int(cx - cx / z), int(cy - cy / z), int(cx + (W - cx) / z), int(cy + (H - cy) / z))
    img.paste(img.crop(box).resize((W, H), Image.NEAREST))


# ------------------------------------------------------------------ 5. something wakes up

WAKE = 2.4       # the eye opens
TURN = 2.9       # the bacteria notice
RUSH = 5.6       # the camera rushes in
FLASH = 7.8      # white


def player(img, x, y, r, lit, eye, tail_dir, t):
    d = ImageDraw.Draw(img)
    col = FG if lit else DIM
    if eye:   # the tail of little rings, like in the game
        for k in range(1, 6):
            tx = x - math.cos(tail_dir) * (r + k * 3.2) + math.sin(t * 9 + k) * 0.8
            ty = y - math.sin(tail_dir) * (r + k * 3.2) + math.cos(t * 9 + k) * 0.8
            rr = max(1, 2.2 - k * 0.25)
            d.ellipse([tx - rr, ty - rr, tx + rr, ty + rr], outline=col)
    d.ellipse([x - r, y - r, x + r, y + r], fill=col)
    if eye:
        ex, ey = x + math.cos(tail_dir) * r * 0.35, y + math.sin(tail_dir) * r * 0.35
        d.rectangle([ex - 1, ey - 1, ex + 1, ey + 1], fill=BG)


def s_wake(img, t, dur):
    cx, cy, R = 160, 84, 70
    px, py = cx, cy
    rnd = random.Random(5)
    cells = []
    while len(cells) < 18:
        x, y = cx + rnd.uniform(-60, 60), cy + rnd.uniform(-60, 60)
        if (x - cx) ** 2 + (y - cy) ** 2 > (R - 10) ** 2 or math.hypot(x - cx, y - cy) < 20:
            continue
        cells.append((x, y, rnd.uniform(0, math.tau), rnd.uniform(8, 14), rnd.random() * 6))
    S = Shade(light=(110, 30, 60), falloff=200, ambient=0.08, key=1.5)
    live = []
    for x, y, a, ln, sd in cells:
        drift = (math.cos(a) * t * 0.8, math.sin(a) * t * 0.8)
        bx, by = x + drift[0], y + drift[1]
        ang = a + math.sin(t * 0.7 + sd) * 0.15
        if t > TURN:   # they notice: turn towards the new cell and close in, stopping in a ring around it
            k = ease(clamp01((t - TURN) / 0.8))
            to = math.atan2(py - by, px - bx)
            dang = (to - ang + math.pi) % math.tau - math.pi
            ang += dang * k
            dist = math.hypot(px - bx, py - by)
            stop = 19 + (int(sd * 10) % 3) * 6
            nd = max(stop, dist - (t - TURN) ** 1.6 * 9)
            bx, by = px - math.cos(to) * nd, py - math.sin(to) * nd
        A3.shaded_bacterium(S, bx, by, ang, ln, t, sd)
        live.append((bx, by, ang, ln, sd))
    S.render(img, rim=False, palette=[BG, RED, FG])
    d = ImageDraw.Draw(img)
    for bx, by, ang, ln, sd in live:   # flagella: limp while asleep, whipping once they turn
        ca, sa = math.cos(ang), math.sin(ang)
        speed = 2.0 if t < TURN else 11.0
        amp = 0.8 if t < TURN else 2.2
        tail = [(bx - ca * (ln / 2 + 3 + k * 1.6) - sa * math.sin(k * 0.9 - t * speed + sd) * amp,
                 by - sa * (ln / 2 + 3 + k * 1.6) + ca * math.sin(k * 0.9 - t * speed + sd) * amp) for k in range(10)]
        d.line(tail, fill=RED)
    # the cell: dormant grey, a few pulses, then the eye opens and it lights up
    if t < WAKE:
        beat = 0.0
        for b in (0.9, 1.5, 1.95, 2.25):
            if b <= t < b + 0.12:
                beat = 1.0
        player(img, px, py, 6 + beat, beat > 0, False, 0.0, t)
    else:
        look = -math.pi / 2 + math.sin((t - WAKE) * 3) * 1.2
        wob = math.sin(t * 7) * 0.6
        player(img, px + wob, py, 7, True, True, look, t)
        rr = (t - WAKE) * 45   # the ping as it wakes
        if rr < 60:
            n = int(18 + rr)
            for k in range(n):
                a = k / n * math.tau
                d.point((px + math.cos(a) * rr, py + math.sin(a) * rr), fill=FG)
    # the eyepiece
    r = np.hypot(XX - cx, YY - cy)
    a_ = np.asarray(img).copy()
    a_[r > R] = BG
    img.paste(Image.fromarray(a_))
    v2.dither(img, np.clip((r - (R - 10)) / 10, 0, 1) * (r <= R), BG)
    d = ImageDraw.Draw(img)
    d.ellipse([cx - R - 1, cy - R - 1, cx + R + 1, cy + R + 1], outline=DIM)
    d.ellipse([cx - R - 4, cy - R - 4, cx + R + 4, cy + R + 4], outline=DIM)
    # the rush in
    if t > RUSH:
        z = 1 + ease(clamp01((t - RUSH) / (FLASH - RUSH))) ** 1.6 * 5
        box = (int(px - px / z), int(py - py / z), int(px + (W - px) / z), int(py + (H - py) / z))
        img.paste(img.crop(box).resize((W, H), Image.NEAREST))
    if t >= FLASH:
        img.paste(FG, (0, 0, W, H))


# ------------------------------------------------------------------ the cut

SHOTS = [
    # (seconds, shot, caption, caption end (s, default the shot's end), caption y)
    (5.0, A3.s_night, "Late at night, one lab still had its lights on."),
    (5.0, A3.s_lab, "For years, the doctor had searched for a cure."),
    (6.0, s_leave, "At last, he called it a day and went home.", 5.6),
    (4.5, s_left, "Under the microscope, he left a sample behind."),
    (FLASH + 0.2, s_wake, "And in the dark, something woke up.", 5.4),
]


def render(only=None):
    n = 0
    for i, shot in enumerate(SHOTS):
        dur, fn, cap = shot[:3]
        cap_end = shot[3] if len(shot) > 3 else dur
        cy = shot[4] if len(shot) > 4 else 166
        for f in range(int(dur * FPS)):
            t = f / FPS
            if only is not None and (i, f) not in only:
                n += 1
                continue
            img = Image.new("RGB", (W, H), BG)
            fn(img, t, dur)
            if t < cap_end:
                v2.caption(img, cap, t, cap_end, cy)
            hd = ImageDraw.Draw(img)
            hd.text((W - 6 - hd.textlength("Hold to skip", font=v2.FONT), 4), "Hold to skip", font=v2.FONT, fill=DIM)
            img.resize((W * SCALE, H * SCALE), Image.NEAREST).save(f"{OUT}/frames/{n:05d}.png")
            n += 1
    return n


if __name__ == "__main__":
    if "stills" in sys.argv:
        picks = {(2, 5), (2, 25), (2, 34), (2, 48), (2, 72), (2, 82), (3, 10), (3, 55), (4, 10), (4, 33), (4, 50), (4, 80), (4, 100), (4, 114)}
        render(picks)
        print("stills ok")
    else:
        print("frames", render())
        subprocess.run(["ffmpeg", "-y", "-loglevel", "error", "-framerate", str(FPS), "-i", f"{OUT}/frames/%05d.png",
                        "-c:v", "libx264", "-preset", "veryfast", "-pix_fmt", "yuv420p", "-r", "30",
                        f"{OUT}/intro_animatic_v4.mp4"], check=True)
        print("ok")
