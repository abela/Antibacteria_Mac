"""
AntiBacteria intro - storyboard animatic, v2: more realistic 8-bit shapes.
Shapes are drawn 4x supersampled from a jointed figure rig and real object proportions, then snapped to the
320x180 pixel grid. Palette: black, white, red + one dim grey (the game's dim role). Lighting: black silhouettes
with a white rim on the side facing the light and a dim outline elsewhere; light pools are ordered-dithered.
"""
import math, random, os, subprocess, sys
import numpy as np
from PIL import Image, ImageDraw, ImageFont

W, H, FPS, SCALE, SS = 320, 180, 15, 4, 4
BG, FG, RED, DIM = (14, 14, 18), (238, 238, 232), (230, 58, 58), (88, 88, 92)
FONT = ImageFont.load_default_imagefont()
OUT = sys.argv[1] if len(sys.argv) > 1 else "/home/claude/intro/out2"
os.makedirs(OUT + "/frames", exist_ok=True)
BAYER = np.array([[0, 8, 2, 10], [12, 4, 14, 6], [3, 11, 1, 9], [15, 7, 13, 5]]) / 16.0 + 1 / 32
BAYER_FULL = np.tile(BAYER, (H // 4 + 1, W // 4 + 1))[:H, :W]


def ease(x):
    x = max(0.0, min(1.0, x))
    return x * x * (3 - 2 * x)


# ------------------------------------------------------------------ supersampled shape layers

class Layer:
    """A 1-bit shape drawn at 4x and snapped to the pixel grid."""

    def __init__(self):
        self.im = Image.new("L", (W * SS, H * SS), 0)
        self.d = ImageDraw.Draw(self.im)

    def _p(self, p):
        return (p[0] * SS, p[1] * SS)

    def capsule(self, a, b, r, v=255):
        a, b = self._p(a), self._p(b)
        r *= SS
        self.d.line([a, b], fill=v, width=int(r * 2))
        for c in (a, b):
            self.d.ellipse([c[0] - r, c[1] - r, c[0] + r, c[1] + r], fill=v)

    def ellipse(self, c, rx, ry, ang=0.0, v=255):
        pts = []
        for k in range(36):
            t = k / 36 * math.tau
            x, y = math.cos(t) * rx, math.sin(t) * ry
            ca, sa = math.cos(ang), math.sin(ang)
            pts.append(self._p((c[0] + x * ca - y * sa, c[1] + x * sa + y * ca)))
        self.d.polygon(pts, fill=v)

    def poly(self, pts, v=255):
        self.d.polygon([self._p(p) for p in pts], fill=v)

    def rect(self, x0, y0, x1, y1, v=255):
        self.d.rectangle([x0 * SS, y0 * SS, x1 * SS, y1 * SS], fill=v)

    def line(self, pts, w=1.0, v=255):
        self.d.line([self._p(p) for p in pts], fill=v, width=max(1, int(w * SS)))

    def mask(self):
        small = np.asarray(self.im.resize((W, H), Image.BOX))
        return small >= 110


def shift(m, dx, dy):
    out = np.zeros_like(m)
    ys = slice(max(0, dy), H + min(0, dy)); yd = slice(max(0, -dy), H + min(0, -dy))
    xs = slice(max(0, dx), W + min(0, dx)); xd = slice(max(0, -dx), W + min(0, -dx))
    out[ys, xs] = m[yd, xd]
    return out


def paint(img, m, color):
    a = np.asarray(img).copy()
    a[m] = color
    img.paste(Image.fromarray(a))


def silhouette(img, m, light=(1, -1), rim=FG, outline=DIM, fill=BG):
    """Solid shape: dim outline outside, white rim on the edge facing the light."""
    grown = m | shift(m, 1, 0) | shift(m, -1, 0) | shift(m, 0, 1) | shift(m, 0, -1)
    lx, ly = light
    lit = m & ((~shift(m, -lx, 0) if lx else False) | (~shift(m, 0, -ly) if ly else False))
    paint(img, grown & ~m, outline)
    paint(img, m, fill)
    paint(img, lit, rim)


def dither(img, field, color, where=None):
    """field: HxW array 0..1 of brightness; lit pixels painted 'color'."""
    m = field > BAYER_FULL
    if where is not None:
        m &= where
    paint(img, m, color)


YY, XX = np.mgrid[0:H, 0:W].astype(float)


def text(img, s, y, color=FG, reveal=1.0, x=None):
    if reveal < 1:
        s = s[:max(0, int(len(s) * reveal + 0.999))]
    d = ImageDraw.Draw(img)
    w = d.textlength(s, font=FONT)
    d.text(((W - w) // 2 if x is None else x, y), s, font=FONT, fill=color)


def caption(img, s, t, dur, y=166):
    if not s:
        return
    rev = min(1.0, t / max(0.4, len(s) * 0.035))
    # a black band behind the line so it reads over anything
    ImageDraw.Draw(img).rectangle([0, y - 3, W, y + 12], fill=BG)
    text(img, s, y, DIM if t > dur - 0.4 else FG, rev)


# ------------------------------------------------------------------ the doctor: a jointed side-view figure

def ang_pt(p, length, deg):
    a = math.radians(deg)  # 0 = up, 90 = forward (right), 180 = down
    return (p[0] + math.sin(a) * length, p[1] - math.cos(a) * length)


POSES = {
    #          torso head  front arm (upper, fore)  back arm      front leg (thigh, shin)  back leg
    "hunch": dict(torso=30, head=38, fa=(150, 88), ba=(165, 128), fl=(178, 182), bl=(192, 186)),
    "stand": dict(torso=3, head=4, fa=(178, 174), ba=(186, 182), fl=(176, 182), bl=(188, 184)),
    "raise": dict(torso=-6, head=-24, fa=(28, 8), ba=(188, 184), fl=(174, 180), bl=(192, 186)),
    "chest": dict(torso=-14, head=22, fa=(170, 315), ba=(212, 238), fl=(158, 200), bl=(196, 172)),
    "slump": dict(torso=46, head=60, fa=(140, 120), ba=(150, 140), fl=(160, 196), bl=(196, 176)),
}


def lerp_pose(a, b, k):
    out = {}
    for key in a:
        va, vb = a[key], b[key]
        out[key] = tuple(x + (y - x) * k for x, y in zip(va, vb)) if isinstance(va, tuple) else va + (vb - va) * k
    return out


def doctor(pose, hip, scale=1.0, breathe=0.0, hold_vial=False):
    """Returns (body mask, joints). ~100 px tall at scale 1: lab coat, glasses, hair, shoes."""
    P = POSES[pose] if isinstance(pose, str) else pose
    s = scale
    L = Layer()
    joints = {}
    # legs (back leg first, a touch thinner reads as depth)
    for key, r in (("bl", 3.0), ("fl", 3.2)):
        th, sh = P[key]
        knee = ang_pt(hip, 24 * s, th)
        ankle = ang_pt(knee, 23 * s, sh)
        L.capsule(hip, knee, r * s)
        L.capsule(knee, ankle, 2.7 * s)
        toe = (ankle[0] + 7 * s, ankle[1] + 0.5 * s)
        L.poly([(ankle[0] - 2.5 * s, ankle[1] - 2 * s), (toe[0], toe[1] - 2 * s), (toe[0] + 1 * s, toe[1] + 1.5 * s),
                (ankle[0] - 2.5 * s, ankle[1] + 1.5 * s)])
    neck = ang_pt(hip, 35 * s + breathe, P["torso"])
    sh = ang_pt(hip, 31 * s + breathe, P["torso"])
    joints["neck"] = neck
    # torso + lab coat (open, to the knee, flaring at the hem)
    t = math.radians(P["torso"])
    fwd = (math.cos(t), math.sin(t))  # perpendicular to the spine, toward the front
    hem = ang_pt(hip, -20 * s, P["torso"])
    L.poly([
        (sh[0] + fwd[0] * 7 * s, sh[1] + fwd[1] * 7 * s),
        (hip[0] + fwd[0] * 8 * s, hip[1] + fwd[1] * 8 * s),
        (hem[0] + fwd[0] * 10 * s, hem[1] + fwd[1] * 10 * s + 1),
        (hem[0] - fwd[0] * 11 * s, hem[1] - fwd[1] * 11 * s + 1),
        (hip[0] - fwd[0] * 8 * s, hip[1] - fwd[1] * 8 * s),
        (sh[0] - fwd[0] * 7 * s, sh[1] - fwd[1] * 7 * s),
        (neck[0] - fwd[0] * 4 * s, neck[1] - fwd[1] * 4 * s),
        (neck[0] + fwd[0] * 4 * s, neck[1] + fwd[1] * 4 * s),
    ])
    # head: skull, jaw, nose, ear, hair at the back
    hc = ang_pt(neck, 7.5 * s, P["head"])
    ha = math.radians(P["head"])
    L.capsule(neck, hc, 3 * s)
    L.ellipse(hc, 6.2 * s, 7.2 * s, ha)
    face = (math.cos(ha), math.sin(ha))
    up = (math.sin(ha), -math.cos(ha))

    def hp(f, u):
        return (hc[0] + face[0] * f * s + up[0] * u * s, hc[1] + face[1] * f * s + up[1] * u * s)
    L.poly([hp(5.5, 0.5), hp(8.6, -1.5), hp(5.8, -3.0)])          # nose
    L.poly([hp(2, -5), hp(5.6, -5.4), hp(4.4, -8.2), hp(0, -8.4)])  # jaw / chin
    L.ellipse(hp(-5.4, 3.2), 3.0 * s, 2.6 * s, ha)                   # hair tuft at the back
    joints["eye"] = hp(3.6, 1.2)
    joints["face"] = face
    # arms: back arm first, then the front one (coat sleeves a little wider at the shoulder)
    for key, r in (("ba", 2.8), ("fa", 3.0)):
        ua, fa_ = P[key]
        elbow = ang_pt(sh, 17 * s, ua)
        wrist = ang_pt(elbow, 15 * s, fa_)
        L.capsule(sh, elbow, r * s)
        L.capsule(elbow, wrist, 2.4 * s)
        hand = ang_pt(wrist, 3.5 * s, fa_)
        L.ellipse(hand, 2.6 * s, 2.2 * s, math.radians(fa_ - 90))
        joints[key + "_hand"] = hand
    m = L.mask()
    return m, joints


def draw_doctor(img, pose, hip, light=(1, -1), scale=1.0, breathe=0.0, details=True):
    m, j = doctor(pose, hip, scale, breathe)
    silhouette(img, m, light)
    if details:
        # glasses glint and the coat's pocket pen: single accent pixels sell the character
        ex, ey = int(round(j["eye"][0])), int(round(j["eye"][1]))
        if 0 <= ex < W and 0 <= ey < H:
            img.putpixel((ex, ey), FG)
            if 0 <= ex + 1 < W: img.putpixel((ex + 1, ey), FG)
    return j


# ------------------------------------------------------------------ props

def microscope(L, x, y, s=1.0):
    """Side view, eyepiece toward the left (the user stands left). (x, y) = base front, on the bench."""
    L.rect(x - 2 * s, y - 3 * s, x + 18 * s, y)                                  # base
    L.poly([(x + 12 * s, y - 3 * s), (x + 17 * s, y - 3 * s), (x + 18 * s, y - 20 * s),
            (x + 14 * s, y - 30 * s), (x + 10 * s, y - 29 * s), (x + 13 * s, y - 20 * s)])  # C-shaped arm
    L.rect(x + 2 * s, y - 13 * s, x + 14 * s, y - 11 * s)                       # stage
    L.capsule((x + 8 * s, y - 15 * s), (x + 8 * s, y - 19 * s), 1.6 * s)          # objective
    L.rect(x + 5 * s, y - 22 * s, x + 12 * s, y - 19 * s)                        # nosepiece
    L.capsule((x + 10 * s, y - 23 * s), (x - 1 * s, y - 36 * s), 2.2 * s)         # body tube
    L.capsule((x - 1 * s, y - 36 * s), (x - 4 * s, y - 40 * s), 1.6 * s)          # eyepiece
    L.ellipse((x + 15 * s, y - 16 * s), 2.2 * s, 2.2 * s)                         # focus knob


def flask(L, x, y, kind, h=12):
    if kind == 0:   # Erlenmeyer
        L.poly([(x - 5, y), (x + 5, y), (x + 1.5, y - h + 4), (x + 1.5, y - h), (x - 1.5, y - h), (x - 1.5, y - h + 4)])
    elif kind == 1:  # round-bottom
        L.ellipse((x, y - 4), 4.5, 4.5)
        L.rect(x - 1.2, y - h, x + 1.2, y - 6)
    elif kind == 2:  # beaker
        L.rect(x - 4, y - h + 3, x + 4, y)
    else:            # test tubes in a rack
        for k in range(4):
            L.capsule((x - 6 + k * 4, y - h + 2), (x - 6 + k * 4, y - 3), 1.2)
        L.rect(x - 8, y - 5, x + 8, y - 3)


def lab_scene(img, t, lamp_x=172, swing=0.0, light_k=1.0, flicker=False):
    """Back wall, window with blinds and rain, shelves of glassware, wall clock, the bench, the hanging lamp."""
    d = ImageDraw.Draw(img)
    lx = lamp_x + math.sin(t * 1.6) * swing
    ly = 30
    # window (left), rain behind the blinds
    d.rectangle([22, 30, 70, 92], outline=DIM)
    rnd = random.Random(int(t * FPS) // 2)
    for _ in range(14):
        rx, ry = rnd.randrange(24, 69), rnd.randrange(32, 90)
        d.line([(rx, ry), (rx - 1, ry + 3)], fill=DIM)
    for by in range(34, 92, 5):
        d.line([(23, by), (69, by)], fill=DIM if by % 10 else BG)
    # wall clock: a quarter past two
    d.ellipse([92, 24, 108, 40], outline=DIM)
    d.line([(100, 32), (100, 27)], fill=DIM)
    d.line([(100, 32), (104, 32)], fill=DIM)
    # shelves + glassware (right)
    shelf = Layer()
    for sy in (46, 70):
        shelf.rect(220, sy, 304, sy + 1)
    kinds = [(228, 46, 0, 12), (242, 46, 1, 12), (256, 46, 2, 9), (270, 46, 3, 12), (290, 46, 0, 10),
             (226, 70, 2, 8), (240, 70, 0, 11), (258, 70, 3, 11), (280, 70, 1, 12), (296, 70, 2, 9)]
    for x, y, k, h in kinds:
        flask(shelf, x, y, k, h)
    silhouette(img, shelf.mask(), light=(-1, -1), rim=FG)
    for x, y, k, h in kinds[::3]:  # coloured liquid in a few of them
        d.line([(x - 2, y - 2), (x + 2, y - 2)], fill=RED)
    # light pool from the lamp: on the wall, the bench top and the floor
    r = np.hypot((XX - lx) / 1.0, (YY - ly) * 0.8)
    cone = np.clip(1 - np.abs(XX - lx) / np.maximum(1, 6 + (YY - ly) * 0.62), 0, 1) * (YY > ly + 6)
    field = (0.62 * cone * np.clip(1 - (YY - ly) / 170, 0, 1)) * light_k
    if flicker:
        field *= 0.6 + 0.4 * (int(t * 12) % 3 != 0)
    dither(img, field, DIM)
    dither(img, field - 0.33, FG)
    # bench: top, drawers, legs
    bench = Layer()
    bench.rect(96, 110, 304, 113)
    bench.rect(98, 113, 102, 150)
    bench.rect(298, 113, 302, 150)
    bench.rect(232, 113, 296, 136)
    silhouette(img, bench.mask(), light=(0, -1))
    for dy in (120, 128):
        d.line([(258, dy), (270, dy)], fill=DIM)
    d.line([(0, 150), (W, 150)], fill=DIM)
    # lamp: cord, shade, bulb
    d.line([(lx, 0), (lx, ly - 6)], fill=DIM)
    lamp = Layer()
    lamp.poly([(lx - 3, ly - 6), (lx + 3, ly - 6), (lx + 9, ly + 2), (lx - 9, ly + 2)])
    silhouette(img, lamp.mask(), light=(0, -1), rim=DIM)
    d.line([(lx - 3, ly + 3), (lx + 3, ly + 3)], fill=FG)
    return lx


# ------------------------------------------------------------------ microscope view creatures

def bacterium(d, x, y, ang, length, t, seed, color=RED):
    """A rod bacterium with a wavy flagellum, outlined, with two specks inside."""
    ca, sa = math.cos(ang), math.sin(ang)
    hl = length / 2
    pts = []
    for k in range(20):
        a = k / 20 * math.tau
        px = math.cos(a) * (hl + 3) if abs(math.cos(a)) > 0.01 else 0
        px = max(-hl - 3, min(hl + 3, math.cos(a) * (hl + 3)))
        py = math.sin(a) * 3.2
        pts.append((x + px * ca - py * sa, y + px * sa + py * ca))
    d.polygon(pts, outline=color)
    for f in (-0.3, 0.25):
        d.point((x + ca * hl * f * 2, y + sa * hl * f * 2), fill=color)
    tail = []
    for k in range(10):
        u = hl + 3 + k * 1.6
        w = math.sin(k * 0.9 - t * 9 + seed) * 2
        tail.append((x - ca * u - sa * w, y - sa * u + ca * w))
    d.line(tail, fill=color)


def red_cell(d, x, y, r, tilt):
    """A red blood cell: a biconcave disc seen at an angle (outlined ring, dark centre)."""
    ry = max(1.0, r * abs(math.cos(tilt)))
    d.ellipse([x - r, y - ry, x + r, y + ry], outline=RED, fill=BG)
    if ry > 2:
        d.ellipse([x - r * 0.4, y - ry * 0.4, x + r * 0.4, y + ry * 0.4], outline=RED)


def player_cell(d, cx, cy, r=7):
    d.ellipse([cx - r, cy - r, cx + r, cy + r], fill=FG)
    d.ellipse([cx - 2, cy - 2, cx + 2, cy + 2], fill=BG)


# ------------------------------------------------------------------ shots

RAIN = [(random.Random(k).randrange(W), random.Random(k + 99).randrange(H), random.Random(k + 7).uniform(110, 160)) for k in range(90)]


def s_night(img, t, dur):
    d = ImageDraw.Draw(img)
    rnd = random.Random(4)
    # far skyline (dim, flat), near buildings with window grids, one lit lab window
    for bx in range(0, W, 14):
        h = rnd.randrange(30, 70)
        d.rectangle([bx, 150 - h, bx + 13, 150], fill=BG, outline=None)
        d.line([(bx, 150 - h), (bx + 13, 150 - h)], fill=DIM)
    near = [(8, 52, 78), (66, 40, 104), (114, 84, 120), (206, 46, 92), (258, 58, 70)]
    city = Layer()
    for bx, bw, bh in near:
        city.rect(bx, 150 - bh, bx + bw, 150)
    city.rect(150, 22, 152, 30)          # antenna on the lab building
    city.rect(76, 40, 90, 46)            # water tank
    city.rect(78, 46, 79, 50); city.rect(87, 46, 88, 50)
    silhouette(img, city.mask(), light=(0, -1), rim=DIM)
    for bx, bw, bh in near:
        for wy in range(150 - bh + 6, 144, 9):
            for wx in range(bx + 4, bx + bw - 4, 8):
                lit = rnd.random() < 0.08
                d.rectangle([wx, wy, wx + 3, wy + 4], fill=FG if lit else None, outline=None if lit else DIM)
    # the lab window: lit, the doctor bent over his bench inside
    wx0, wy0 = 148, 64
    d.rectangle([wx0, wy0, wx0 + 18, wy0 + 13], fill=FG)
    tiny = Layer()
    tiny.ellipse((wx0 + 7, wy0 + 6), 2, 2.3)
    tiny.poly([(wx0 + 4, wy0 + 13), (wx0 + 10, wy0 + 13), (wx0 + 9, wy0 + 8), (wx0 + 5, wy0 + 8)])
    paint(img, tiny.mask(), BG)
    # street lamp with its pool of light, rain streaks through it
    d.line([(236, 150), (236, 108)], fill=DIM)
    d.line([(236, 108), (226, 104)], fill=DIM)
    pool = np.clip(1 - np.abs(XX - 226) / np.maximum(1, 2 + (YY - 104) * 0.5), 0, 1) * (YY > 104) * (YY < 150) * 0.35
    dither(img, pool, DIM)
    for x, y, v in RAIN:
        yy = (y + t * v) % H
        d.line([(x, yy), (x - 2, yy + 4)], fill=DIM)
    d.line([(0, 150), (W, 150)], fill=DIM)
    # slow push toward the window
    z = 1 + ease(t / dur) * 0.9
    cx, cy = wx0 + 9, wy0 + 6
    box = (int(cx - cx / z), int(cy - cy / z), int(cx + (W - cx) / z), int(cy + (H - cy) / z))
    img.paste(img.crop(box).resize((W, H), Image.NEAREST))


def s_lab(img, t, dur):
    lx = lab_scene(img, t, swing=0.6)
    scope = Layer()
    microscope(scope, 150, 110, 1.0)
    silhouette(img, scope.mask(), light=(1, -1))
    draw_doctor(img, "hunch", (114, 101), light=(1, -1), breathe=math.sin(t * 2) * 0.6)
    # notes and a mug on the bench
    d = ImageDraw.Draw(img)
    d.rectangle([182, 108, 196, 109], fill=FG)
    d.rectangle([206, 102, 212, 109], outline=FG)
    d.line([(213, 104), (214, 106)], fill=FG)
    if int(t * 3) % 2 == 0:
        d.point((209, 99 - int(t * 4) % 3), fill=DIM)


def s_scope(img, t, dur):
    d = ImageDraw.Draw(img)
    cx, cy, R = 160, 84, 70
    # faint field texture
    rnd = random.Random(2)
    for _ in range(60):
        px, py = rnd.uniform(cx - R, cx + R), rnd.uniform(cy - R, cy + R)
        d.point((px, py), fill=DIM)
    drop_t = 1.4
    rnd = random.Random(3)
    cells = [(cx + rnd.uniform(-55, 55), cy + rnd.uniform(-55, 55), rnd.uniform(0, math.tau), rnd.uniform(8, 14), rnd.random() * 6)
             for _ in range(16)]
    for x, y, a, ln, s in cells:
        if (x - cx) ** 2 + (y - cy) ** 2 > (R - 12) ** 2:
            continue
        dist = math.hypot(x - cx, y - cy)
        killed_at = drop_t + 0.4 + dist / 20
        xx, yy = x + math.cos(a) * t * 3, y + math.sin(a) * t * 3
        if t < killed_at:
            bacterium(d, xx, yy, a + math.sin(t + s) * 0.2, ln, t, s)
        elif t < killed_at + 0.35:
            k = (t - killed_at) / 0.35
            for f in range(7):  # it bursts into fragments
                fa = f / 7 * math.tau + s
                d.point((xx + math.cos(fa) * (3 + k * 8), yy + math.sin(fa) * (3 + k * 8)), fill=RED)
        else:  # a white cure cell has taken its place
            d.ellipse([xx - 2, yy - 2, xx + 2, yy + 2], fill=FG)
    if t > drop_t:
        rr = (t - drop_t) * 20
        n = int(14 + rr * 1.5)
        for k in range(n):
            a = k / n * math.tau + t
            d.point((cx + math.cos(a) * rr, cy + math.sin(a) * rr), fill=FG)
        d.ellipse([cx - 3, cy - 3, cx + 3, cy + 3], fill=FG)
    elif t > drop_t - 0.7:
        yy = cy - 70 + (t - (drop_t - 0.7)) / 0.7 * 70
        d.polygon([(cx, yy - 5), (cx + 3, yy), (cx, yy + 3), (cx - 3, yy)], fill=FG)
    # eyepiece: hard circular mask, a soft dithered edge, the tube's rim
    r = np.hypot(XX - cx, YY - cy)
    a = np.asarray(img).copy()
    a[r > R] = BG
    img.paste(Image.fromarray(a))
    dither(img, np.clip((r - (R - 8)) / 8, 0, 1) * (r <= R), BG)
    d.ellipse([cx - R - 1, cy - R - 1, cx + R + 1, cy + R + 1], outline=DIM)
    d.ellipse([cx - R - 4, cy - R - 4, cx + R + 4, cy + R + 4], outline=DIM)


def s_eureka(img, t, dur):
    lab_scene(img, t, swing=0.6, light_k=1.0)
    scope = Layer()
    microscope(scope, 150, 110, 1.0)
    silhouette(img, scope.mask(), light=(1, -1))
    k = ease((t - 0.8) / 0.7)
    pose = lerp_pose(POSES["hunch"], POSES["raise"], k)
    j = draw_doctor(img, pose, (114 - 6 * k, 101 - 1 * k), light=(1, -1))
    d = ImageDraw.Draw(img)
    if 0.25 < t < 0.9:  # the moment he realises
        ex, ey = j["eye"]
        d.rectangle([ex + 2, ey - 20, ex + 4, ey - 12], fill=RED)
        d.rectangle([ex + 2, ey - 9, ex + 4, ey - 7], fill=RED)
    if k > 0.6:  # the vial held up to the lamp, glowing
        hx, hy = j["fa_hand"]
        d.rectangle([hx - 2, hy - 11, hx + 2, hy - 2], outline=FG)
        d.rectangle([hx - 2, hy - 13, hx + 2, hy - 12], fill=DIM)
        d.rectangle([hx - 1, hy - 7, hx + 1, hy - 3], fill=FG)
        if int(t * 6) % 2 == 0:
            for a in range(0, 360, 45):
                rr = 8 + (a % 90 == 0) * 3
                d.point((hx + math.cos(math.radians(a)) * rr, hy - 7 + math.sin(math.radians(a)) * rr), fill=FG)


def hand_layer(L, wrist, ang_deg, s=1.0, grip=True, thumb_up=False):
    """A big close-up hand: palm, four fingers wrapped (grip) or open, thumb."""
    a = math.radians(ang_deg)
    f = (math.cos(a), math.sin(a)); n = (-math.sin(a), math.cos(a))

    def p(u, v):
        return (wrist[0] + f[0] * u * s + n[0] * v * s, wrist[1] + f[1] * u * s + n[1] * v * s)
    L.poly([p(0, -9), p(22, -11), p(26, -4), p(26, 8), p(20, 11), p(0, 9)])   # palm
    for k in range(4):
        base = p(24, -8 + k * 5.2)
        tip = p(33 if not grip else 30, -9 + k * 5.6) if not grip else p(30, -6 + k * 5.6)
        L.capsule(base, tip, 2.4 * s)
        if grip:
            L.capsule(tip, p(25, -4 + k * 5.6), 2.2 * s)
    L.capsule(p(8, -9), p(18, -19 if thumb_up else -15), 2.6 * s)            # thumb
    L.capsule(p(18, -19 if thumb_up else -15), p(25, -21 if thumb_up else -15), 2.3 * s)
    L.capsule(p(-30, 0), p(0, 0), 9 * s)                                      # forearm
    # the gaps between the fingers, drawn dim over the silhouette
    return [(p(22, -5.4 + k * 5.4), p(31, -5.0 + k * 5.6)) for k in range(3)]


def syringe(d, x, top, bottom, fill_k, plunger_y=None, w=8):
    """Vertical syringe, needle up at (x, top - needle). fill_k: 0..1 of the barrel filled with the white cure."""
    bx0, bx1 = x - w // 2, x + w // 2
    d.line([(x, top - 16), (x, top - 4)], fill=FG)                    # needle
    d.point((x, top - 17), fill=DIM)
    d.rectangle([x - 2, top - 4, x + 2, top], fill=DIM)               # hub
    d.rectangle([bx0, top, bx1, bottom], outline=FG)                  # barrel
    liquid = int((bottom - top - 2) * fill_k)
    if liquid > 0:
        d.rectangle([bx0 + 1, top + 1, bx1 - 1, top + liquid], fill=FG)
        d.line([(bx0 + 1, top + liquid + 1), (bx1 - 1, top + liquid + 1)], fill=DIM)  # meniscus
    py = top + liquid + 2 if plunger_y is None else plunger_y
    d.rectangle([bx0 + 1, py, bx1 - 1, py + 3], fill=DIM)            # rubber stopper
    for k in range(top + 6, bottom - 2, 6):
        d.line([(bx1 - 2, k), (bx1 - 1, k)], fill=DIM)               # graduations
    d.line([(x, py + 4), (x, bottom + 26)], fill=DIM, width=2)        # rod
    d.rectangle([x - 7, bottom + 26, x + 7, bottom + 28], fill=DIM)  # thumb press
    d.rectangle([bx0 - 6, bottom, bx1 + 6, bottom + 2], fill=FG)     # finger flanges


def s_syringe(img, t, dur):
    d = ImageDraw.Draw(img)
    # a dim light pool behind
    dither(img, np.clip(1 - np.hypot(XX - 160, YY - 80) / 120, 0, 1) * 0.35, DIM)
    k = ease(t / (dur * 0.75))
    # the vial, upside down: glass, liquid draining, crimped metal cap, a label
    vx, vy0, vy1 = 160, 10, 44
    d.rectangle([vx - 11, vy0, vx + 11, vy1], outline=FG)
    level = int((vy1 - vy0 - 4) * (1 - k))
    if level > 0:
        d.rectangle([vx - 10, vy1 - level, vx + 10, vy1 - 1], fill=FG)
    d.rectangle([vx - 11, vy0 + 8, vx + 11, vy0 + 18], fill=BG, outline=DIM)
    text(img, "AB-1", vy0 + 9, RED)
    d.rectangle([vx - 6, vy1, vx + 6, vy1 + 5], fill=DIM)
    # the syringe below, filling
    syringe(d, vx, 66, 126, k)
    # left hand holding the vial from the left, right hand gripping the barrel from the right
    hl = Layer()
    gaps = hand_layer(hl, (122, 24), 0, 0.9, grip=True)
    hr = Layer()
    gaps += hand_layer(hr, (198, 100), 180, 0.95, grip=True)
    m = hl.mask() | hr.mask()
    silhouette(img, m, light=(0, -1))
    for a, b in gaps:
        d.line([a, b], fill=DIM)


def s_inject(img, t, dur):
    d = ImageDraw.Draw(img)
    dither(img, np.clip(1 - np.hypot(XX - 170, YY - 70) / 140, 0, 1) * 0.3, DIM)
    # forearm palm-up across the frame: rolled-up sleeve at the left, wrist and a loose hand at the right
    arm = Layer()
    arm.poly([(0, 96), (60, 100), (150, 104), (215, 108), (238, 104), (262, 104), (280, 112), (276, 128),
              (246, 132), (220, 130), (150, 136), (60, 142), (0, 146)])
    for k in range(4):
        arm.capsule((272, 108 + k * 5), (292 + (k == 0) * 2, 106 + k * 6), 2.6)
    am = arm.mask()
    sleeve = Layer()
    sleeve.poly([(0, 88), (38, 92), (44, 100), (40, 150), (0, 156)])
    sm = sleeve.mask()
    silhouette(img, am & ~sm, light=(0, -1))
    silhouette(img, sm, light=(0, -1))
    for k in range(3):  # folds in the rolled sleeve
        d.line([(10 + k * 10, 94 + k), (14 + k * 10, 148 - k)], fill=DIM)
    # the vein: red, then the cure travels up it, white, toward the body
    vein = [(40, 122), (90, 120), (140, 117), (180, 116), (214, 118)]
    push = ease((t - 1.6) / 0.9) if t > 1.6 else 0.0
    d.line(vein, fill=RED)
    if push > 0:
        travel = ease((t - 1.8) / 1.0)
        n = int(len(vein) * 0 + 1)
        x_end = 180 - travel * 140
        d.line([(x, y) for x, y in vein if x >= x_end and x <= 180] or [(180, 116)], fill=FG)
    # the syringe, coming in at 30 degrees from the upper right
    k = ease(t / 1.3)
    tip = (178 + (1 - k) * 34, 113 - (1 - k) * 22)
    a = math.radians(-30)
    u = (math.cos(a), math.sin(a)); nrm = (-math.sin(a), math.cos(a))

    def at(s, o=0.0):
        return (tip[0] + u[0] * s + nrm[0] * o, tip[1] + u[1] * s + nrm[1] * o)
    d.line([at(0 if k < 1 else 4), at(16)], fill=FG)        # needle (into the skin once in)
    d.polygon([at(16, -1.5), at(20, -1.5), at(20, 1.5), at(16, 1.5)], fill=DIM)
    d.polygon([at(20, -5), at(84, -5), at(84, 5), at(20, 5)], fill=BG, outline=FG)
    liquid = 1 - push
    if liquid > 0.02:
        d.polygon([at(21, -4), at(21 + 62 * liquid, -4), at(21 + 62 * liquid, 4), at(21, 4)], fill=FG)
    d.polygon([at(22 + 62 * liquid, -4), at(25 + 62 * liquid, -4), at(25 + 62 * liquid, 4), at(22 + 62 * liquid, 4)], fill=DIM)
    d.line([at(26 + 62 * liquid), at(112 - 60 * push + 60 * (1 - liquid) * 0)], fill=DIM, width=2)
    d.polygon([at(84, -9), at(86, -9), at(86, 9), at(84, 9)], fill=FG)  # finger flange
    # the thumb pressing (right hand at the top right)
    th = Layer()
    tp = at(112 - 60 * push)
    th.capsule(tp, (tp[0] + 26, tp[1] - 26), 5)
    th.ellipse((tp[0] + 2, tp[1] - 2), 5.5, 4.5)
    silhouette(img, th.mask(), light=(0, -1))
    if 1.6 < t < 1.75:
        img.paste(FG, (0, 0, W, H))


def ecg_points(t, x0, x1, base, amp):
    pts = []
    for x in range(x0, x1):
        tt = t - (x1 - x) / 120.0
        rate = 1.0 if tt < 1.0 else min(3.5, 1.0 + (tt - 1.0) * 2.2)
        ph = (tt * rate) % 1.0 if tt > 0 else 0.5
        y = base
        if 0.18 < ph < 0.24: y = base - amp * 0.15 * math.sin((ph - 0.18) / 0.06 * math.pi)   # P
        elif 0.30 < ph < 0.32: y = base + amp * 0.15                                             # Q
        elif 0.32 < ph < 0.35: y = base - amp                                                    # R
        elif 0.35 < ph < 0.37: y = base + amp * 0.35                                             # S
        elif 0.50 < ph < 0.62: y = base - amp * 0.25 * math.sin((ph - 0.5) / 0.12 * math.pi)   # T
        pts.append((x, y))
    return pts


def s_reaction(img, t, dur):
    racing = t > 1.4
    shake = (random.randint(-2, 2), random.randint(-1, 1)) if racing else (0, 0)
    lab_scene(img, t, swing=3.5 if racing else 0.6, flicker=racing)
    scope = Layer()
    microscope(scope, 150, 110, 1.0)
    silhouette(img, scope.mask(), light=(1, -1))
    k = ease((t - 0.3) / 1.4)
    pose = lerp_pose(POSES["stand"], POSES["chest"], k)
    draw_doctor(img, pose, (100 + shake[0] - 10 * k, 101 + shake[1] + 4 * k), light=(1, -1))
    # heart monitor overlay along the top, with a readout
    d = ImageDraw.Draw(img)
    d.rectangle([0, 0, W, 34], fill=BG)
    d.line([(0, 34), (W, 34)], fill=DIM)
    pts = ecg_points(t, 0, W - 46, 20, 13)
    for i in range(1, len(pts)):
        fade = i / len(pts)
        col = (RED if racing and fade > 0.55 else FG) if fade > 0.25 else DIM
        d.line([pts[i - 1], pts[i]], fill=col)
    bpm = int(72 if t < 1.0 else min(190, 72 + (t - 1.0) * 70))
    text(img, str(bpm), 8, RED if racing else FG, x=W - 38)
    text(img, "BPM", 20, DIM, x=W - 38)
    # vision closing in, pulsing with the heartbeat
    if racing:
        pulse = 0.15 * (int(t * 6) % 2)
        close = ease((t - 1.4) / (dur - 1.4))
        r = np.hypot((XX - 160) / 160, (YY - 100) / 90)
        dither(img, np.clip((r - (1.25 - close * 0.75 - pulse)) * 2.5, 0, 1) * (YY > 35), BG)


def s_dive(img, t, dur):
    d = ImageDraw.Draw(img)
    if t < 2.4:
        # the vessel: dithered tunnel walls rushing outward, red blood cells tumbling past
        r = np.hypot((XX - 160) / 1.0, (YY - 90) / 0.62)
        wall = 0.5 + 0.5 * np.sin(np.log(np.maximum(r, 1)) * 9 - t * 14)
        field = wall * np.clip((r - 30) / 120, 0, 1) * 0.55
        dither(img, field, DIM)
        dither(img, field - 0.32, RED)
        rnd = random.Random(11)
        for k in range(22):
            a = rnd.random() * math.tau
            s = ((rnd.random() + t * 0.8) % 1.0) ** 2.2
            x, y = 160 + math.cos(a) * s * 230, 90 + math.sin(a) * s * 140
            red_cell(d, x, y, 2 + s * 14, t * 3 + k)
        # the white cure ahead of us, getting closer
        cr = 2 + ease(t / 2.4) * 6
        d.ellipse([160 - cr, 90 - cr, 160 + cr, 90 + cr], fill=FG)
        if t > 2.2:
            img.paste(FG, (0, 0, W, H))
    else:
        tt = t - 2.4
        # the arena forms: walls slide in, specks settle, the player's cell lands, bacteria arrive
        wall = ease(tt / 0.8)
        for side in (0, 1):
            edge = 18 if side == 0 else W - 19
            off = int((1 - wall) * (-50 if side == 0 else 50))
            for y in range(H):
                jag = int(2 * math.sin(y * 0.7) + (y * 7 % 5 > 2))
                x = edge + off + (jag if side == 0 else -jag)
                if side == 0:
                    d.line([(0, y), (x - 3, y)], fill=FG)
                    if y % 2 == 0: d.point((x - 1, y), fill=FG)
                else:
                    d.line([(x + 3, y), (W, y)], fill=FG)
                    if y % 2 == 0: d.point((x + 1, y), fill=FG)
        rnd = random.Random(5)
        for _ in range(40):
            d.point((rnd.randrange(30, 290), rnd.randrange(4, 176)), fill=DIM)
        if tt > 0.5:
            drop = ease((tt - 0.5) / 0.5)
            player_cell(d, 160, int(96 - (1 - drop) * 40), 7)
            if tt < 1.6:
                r = (tt - 0.5) * 40
                d.ellipse([160 - r, 96 - r, 160 + r, 96 + r], outline=FG)
        if tt > 1.3:
            rnd = random.Random(9)
            for k in range(6):
                bx, by = rnd.choice([(rnd.randrange(40, 280), 12), (rnd.randrange(40, 280), 168),
                                     (40, rnd.randrange(20, 160)), (280, rnd.randrange(20, 160))])
                if int((tt - 1.3) * 10) > k:
                    bacterium(d, bx, by, math.atan2(96 - by, 160 - bx), 9, t, k)
        if tt > 2.0:
            tmp = Image.new("RGB", (W // 2, 20), BG)
            td = ImageDraw.Draw(tmp)
            td.text(((W // 2 - td.textlength("YOU ARE THE CURE", font=FONT)) // 2, 4), "YOU ARE THE CURE", font=FONT, fill=RED)
            big = tmp.resize((W, 40), Image.NEAREST)
            img.paste(big, (0, 34), big.convert("L").point(lambda v: 255 if v > 40 else 0))


SHOTS = [
    (5.0, s_night, "Late at night, one lab still had its lights on."),
    (5.0, s_lab, "For years, the doctor had searched for a cure."),
    (5.5, s_scope, "Tonight, one finally worked."),
    (2.8, s_eureka, ""),
    (4.2, s_syringe, "There was no one else to test it on.", 150),
    (3.4, s_inject, ""),
    (4.8, s_reaction, "His body answered at once."),
    (6.5, s_dive, ""),
]


def render(only=None):
    n = 0
    for i, shot in enumerate(SHOTS):
        dur, fn, cap = shot[:3]
        cy = shot[3] if len(shot) > 3 else 166
        for f in range(int(dur * FPS)):
            t = f / FPS
            if only is not None and (i, f) not in only:
                n += 1
                continue
            img = Image.new("RGB", (W, H), BG)
            fn(img, t, dur)
            caption(img, cap, t, dur, cy)
            hd = ImageDraw.Draw(img)
            hd.text((W - 6 - hd.textlength("Hold to skip", font=FONT), 4), "Hold to skip", font=FONT, fill=DIM)
            img.resize((W * SCALE, H * SCALE), Image.NEAREST).save(f"{OUT}/frames/{n:05d}.png")
            n += 1
    return n


if __name__ == "__main__":
    if len(sys.argv) > 2 and sys.argv[2] == "stills":
        picks = {(0, 60), (1, 40), (2, 45), (3, 30), (4, 30), (5, 30), (6, 50), (7, 20), (7, 90)}
        render(picks)
    else:
        print("frames", render())
        subprocess.run(["ffmpeg", "-y", "-loglevel", "error", "-framerate", str(FPS), "-i", f"{OUT}/frames/%05d.png",
                        "-c:v", "libx264", "-preset", "veryfast", "-pix_fmt", "yuv420p", "-r", "30",
                        f"{OUT}/intro_animatic_v2.mp4"], check=True)
        print("ok")
