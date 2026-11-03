"""
AntiBacteria intro - storyboard animatic v3: shaded, volumetric 8-bit.
Every solid is a lit 3D primitive (capsules, spheres, tapered limbs, planes) shaded by the lab lamp as a point
light, then ordered-dithered into the game's tones: black, dim grey, white (+ red accents). A cool rim light from
the window picks out the dark side. Reuses the v2 night, microscope, monitor and dive shots.
"""
import math, random, os, subprocess, sys
import numpy as np
from PIL import Image, ImageDraw

sys.argv = [sys.argv[0], "/home/claude/intro/_v2tmp"] + sys.argv[1:]
import animatic2 as v2  # noqa: E402  (its shots and helpers)
sys.argv = [sys.argv[0]] + sys.argv[2:]

W, H, FPS, SCALE = v2.W, v2.H, v2.FPS, v2.SCALE
BG, FG, RED, DIM = v2.BG, v2.FG, v2.RED, v2.DIM
YY, XX = v2.YY, v2.XX
B = v2.BAYER_FULL
OUT = "/home/claude/intro/out3"
os.makedirs(OUT + "/frames", exist_ok=True)
ang_pt, POSES, lerp_pose, ease = v2.ang_pt, v2.POSES, v2.lerp_pose, v2.ease


class Shade:
    """A lit buffer: v (0..1 brightness), cov (something drawn), red (accent overlay)."""

    def __init__(self, light=(170, 28, 40), falloff=150.0, ambient=0.05, key=1.25, rim_from=(-1, 0)):
        self.v = np.zeros((H, W))
        self.cov = np.zeros((H, W), bool)
        self.red = np.zeros((H, W), bool)
        self.light, self.falloff, self.ambient, self.key, self.rim_from = light, falloff, ambient, key, rim_from

    def _lit(self, px, py, nx, ny, nz, albedo, pz=0.0):
        lx, ly, lz = self.light
        dx, dy, dz = lx - px, ly - py, lz - pz
        dist = np.sqrt(dx * dx + dy * dy + dz * dz) + 1e-6
        diff = np.clip((nx * dx + ny * dy + nz * dz) / dist, 0, 1)
        fall = 1.0 / (1.0 + (dist / self.falloff) ** 2)
        return albedo * (self.ambient + self.key * diff * fall)

    def _bbox(self, x0, y0, x1, y1):
        x0, y0 = max(0, int(math.floor(x0))), max(0, int(math.floor(y0)))
        x1, y1 = min(W, int(math.ceil(x1)) + 1), min(H, int(math.ceil(y1)) + 1)
        return x0, y0, x1, y1

    def _put(self, sl, inside, val):
        vv = self.v[sl]; cc = self.cov[sl]
        vv[inside] = val[inside]
        cc[inside] = True

    def capsule(self, a, b, ra, albedo, rb=None, caps=True, flat=1.0):
        """A (tapered) limb: radius ra at a, rb at b, shaded as a cylinder with round caps."""
        rb = ra if rb is None else rb
        r = max(ra, rb)
        x0, y0, x1, y1 = self._bbox(min(a[0], b[0]) - r, min(a[1], b[1]) - r, max(a[0], b[0]) + r, max(a[1], b[1]) + r)
        if x1 <= x0 or y1 <= y0:
            return
        sl = (slice(y0, y1), slice(x0, x1))
        px, py = XX[sl] + 0.5, YY[sl] + 0.5
        ax, ay, bx, by = a[0], a[1], b[0], b[1]
        vx, vy = bx - ax, by - ay
        l2 = vx * vx + vy * vy + 1e-9
        t_raw = ((px - ax) * vx + (py - ay) * vy) / l2
        t = np.clip(t_raw, 0, 1)
        qx, qy = ax + t * vx, ay + t * vy
        rr = ra + (rb - ra) * t
        dx, dy = px - qx, py - qy
        d = np.sqrt(dx * dx + dy * dy)
        inside = d < rr
        if not caps:
            inside &= (t_raw >= 0) & (t_raw <= 1)
        k = np.clip(d / np.maximum(rr, 1e-6), 0, 1)
        nz = np.sqrt(1 - k * k) * flat + (1 - flat)
        nx, ny = dx / np.maximum(rr, 1e-6), dy / np.maximum(rr, 1e-6)
        n = np.sqrt(nx * nx + ny * ny + nz * nz)
        self._put(sl, inside, self._lit(px, py, nx / n, ny / n, nz / n, albedo, pz=nz * rr))

    def tube(self, pts, albedo):
        """One smooth limb through (x, y, radius) points: each pixel takes the segment it is deepest inside."""
        rmax = max(p[2] for p in pts)
        x0, y0, x1, y1 = self._bbox(min(p[0] for p in pts) - rmax, min(p[1] for p in pts) - rmax,
                                    max(p[0] for p in pts) + rmax, max(p[1] for p in pts) + rmax)
        if x1 <= x0 or y1 <= y0:
            return
        sl = (slice(y0, y1), slice(x0, x1))
        px, py = XX[sl] + 0.5, YY[sl] + 0.5
        best = np.full(px.shape, -1e9)
        bdx = np.zeros(px.shape); bdy = np.zeros(px.shape); brr = np.ones(px.shape)
        for (ax, ay, ra), (bx, by, rb) in zip(pts, pts[1:]):
            vx, vy = bx - ax, by - ay
            l2 = vx * vx + vy * vy + 1e-9
            t = np.clip(((px - ax) * vx + (py - ay) * vy) / l2, 0, 1)
            qx, qy = ax + t * vx, ay + t * vy
            rr = ra + (rb - ra) * t
            dx, dy = px - qx, py - qy
            score = rr - np.sqrt(dx * dx + dy * dy)
            m = score > best
            best[m] = score[m]; bdx[m] = dx[m]; bdy[m] = dy[m]; brr[m] = rr[m]
        inside = best > 0
        k = np.clip(np.sqrt(bdx ** 2 + bdy ** 2) / brr, 0, 1)
        nz = np.sqrt(1 - k * k)
        nx, ny = bdx / brr, bdy / brr
        n = np.sqrt(nx * nx + ny * ny + nz * nz) + 1e-9
        self._put(sl, inside, self._lit(px, py, nx / n, ny / n, nz / n, albedo, pz=nz * brr))

    def sphere(self, c, rx, ry, albedo, ang=0.0):
        r = max(rx, ry)
        x0, y0, x1, y1 = self._bbox(c[0] - r, c[1] - r, c[0] + r, c[1] + r)
        if x1 <= x0 or y1 <= y0:
            return
        sl = (slice(y0, y1), slice(x0, x1))
        px, py = XX[sl] + 0.5, YY[sl] + 0.5
        ca, sa = math.cos(ang), math.sin(ang)
        lx = ((px - c[0]) * ca + (py - c[1]) * sa) / rx
        ly = (-(px - c[0]) * sa + (py - c[1]) * ca) / ry
        k2 = lx * lx + ly * ly
        inside = k2 < 1
        nz = np.sqrt(np.clip(1 - k2, 0, 1))
        nx, ny = lx * ca - ly * sa, lx * sa + ly * ca
        self._put(sl, inside, self._lit(px, py, nx, ny, nz, albedo, pz=nz * r))

    def blob(self, pts, c, rx, ry, albedo):
        """A polygon outline (profile) shaded as if it were an ellipsoid centred on c: bumps outside it face outward."""
        m = Image.new("L", (W, H), 0)
        ImageDraw.Draw(m).polygon([tuple(p) for p in pts], fill=255)
        inside = np.asarray(m) > 0
        px, py = XX + 0.5, YY + 0.5
        lx, ly = (px - c[0]) / rx, (py - c[1]) / ry
        k = np.sqrt(lx * lx + ly * ly)
        nz = np.sqrt(np.clip(1 - np.minimum(k, 1) ** 2, 0, 1)) + 0.12
        kk = np.maximum(k, 1e-6)
        nx, ny = lx / np.maximum(kk, 1), ly / np.maximum(kk, 1)
        n = np.sqrt(nx * nx + ny * ny + nz * nz)
        val = self._lit(px, py, nx / n, ny / n, nz / n, albedo, pz=nz * max(rx, ry))
        self.v[inside] = val[inside]
        self.cov[inside] = True

    def poly(self, pts, albedo, normal=(0, 0, 1)):
        m = Image.new("L", (W, H), 0)
        ImageDraw.Draw(m).polygon([tuple(p) for p in pts], fill=255)
        inside = np.asarray(m) > 0
        n = np.array(normal, float); n /= np.linalg.norm(n)
        val = self._lit(XX + 0.5, YY + 0.5, n[0], n[1], n[2], albedo)
        self.v[inside] = val[inside]
        self.cov[inside] = True

    def rect(self, x0, y0, x1, y1, albedo, normal=(0, 0, 1)):
        self.poly([(x0, y0), (x1, y0), (x1, y1), (x0, y1)], albedo, normal)

    def render(self, img, rim=True, gamma=0.8, palette=None, black=0.0):
        v = np.clip(self.v - black, 0, 1) ** gamma * 2.0
        level = np.floor(v).astype(int)
        level += (v - np.floor(v)) > B
        level = np.clip(level, 0, 2)
        a = np.asarray(img).copy()
        cols = np.array(palette or [BG, DIM, FG], dtype=np.uint8)
        a[self.cov] = cols[level[self.cov]]
        if rim:  # cool window light on the far side: a dim rim where the shape meets darkness
            dx, dy = self.rim_from
            edge = self.cov & ~v2.shift(self.cov, -dx, -dy)
            a[edge & (level == 0)] = DIM
        a[self.red] = RED
        img.paste(Image.fromarray(a))


# ------------------------------------------------------------------ the doctor, shaded

COAT, SKIN, HAIR, PANTS, SHOE = 0.95, 0.72, 0.22, 0.32, 0.12


def doctor(S, pose, hip, s=1.0, breathe=0.0):
    P = POSES[pose] if isinstance(pose, str) else pose
    J = {}
    neck = ang_pt(hip, 35 * s + breathe, P["torso"])
    sh = ang_pt(hip, 31 * s + breathe, P["torso"])
    hem = ang_pt(hip, -21 * s, P["torso"])

    def arm(key, alb_sleeve):
        ua, fa = P[key]
        elbow = ang_pt(sh, 17 * s, ua)
        wrist = ang_pt(elbow, 15 * s, fa)
        S.capsule(sh, elbow, 3.4 * s, alb_sleeve, rb=3.0 * s)
        S.capsule(elbow, wrist, 3.0 * s, alb_sleeve, rb=2.6 * s)
        hand = ang_pt(wrist, 3.2 * s, fa)
        S.capsule(wrist, hand, 2.0 * s, SKIN, rb=2.4 * s)
        J[key + "_hand"] = hand

    def leg(key):
        th, shn = P[key]
        knee = ang_pt(hip, 24 * s, th)
        ankle = ang_pt(knee, 23 * s, shn)
        S.capsule(hip, knee, 3.6 * s, PANTS, rb=3.0 * s)
        S.capsule(knee, ankle, 3.0 * s, PANTS, rb=2.5 * s)
        S.capsule((ankle[0] - 1.5 * s, ankle[1] + 1 * s), (ankle[0] + 6.5 * s, ankle[1] + 1.5 * s), 1.9 * s, SHOE)

    arm("ba", COAT * 0.8)
    leg("bl")
    leg("fl")
    # torso and the lab coat: chest, then a flared skirt to the knee (no round end)
    S.capsule(sh, hip, 7.6 * s, COAT, rb=7.2 * s)
    S.capsule(hip, hem, 7.6 * s, COAT, rb=10.5 * s, caps=False, flat=0.7)
    S.capsule(neck, sh, 3.2 * s, SKIN)
    # collar: a darker shirt V under the coat lapels
    hc = ang_pt(neck, 7.5 * s, P["head"])
    ha = math.radians(P["head"])
    face = (math.cos(ha), math.sin(ha)); up = (math.sin(ha), -math.cos(ha))

    def hp(f, u):
        return (hc[0] + face[0] * f * s + up[0] * u * s, hc[1] + face[1] * f * s + up[1] * u * s)
    S.sphere(hp(-0.8, 0.6), 6.6 * s, 7.0 * s, HAIR, ha)                   # hair (the back of the head)
    S.sphere(hp(0.8, -0.4), 6.0 * s, 6.8 * s, SKIN, ha)                   # face / skull
    S.capsule(hp(3.5, -4.2), hp(4.4, -6.6), 2.6 * s, SKIN)                # jaw and chin
    S.capsule(hp(5.6, 0.2), hp(7.6, -1.6), 1.2 * s, SKIN, rb=0.9 * s)     # nose
    S.sphere(hp(-0.6, 0.2), 1.5 * s, 2.0 * s, SKIN * 0.8, ha)             # ear
    J["eye"] = hp(4.0, 1.4)
    J["glass"] = [hp(2.2, 2.4), hp(5.6, 2.4), hp(5.6, 0.4), hp(2.2, 0.4)]
    J["temple"] = (hp(2.2, 1.6), hp(-0.6, 1.4))
    arm("fa", COAT)
    J["hc"] = hc
    return J


def doctor_details(img, J, s=1.0):
    d = ImageDraw.Draw(img)
    gx = [p[0] for p in J["glass"]]; gy = [p[1] for p in J["glass"]]
    ex, ey = J["eye"]
    d.point((ex, ey), fill=BG)
    d.point((ex + 1, ey - 1), fill=FG)   # the glint on his glasses


# ------------------------------------------------------------------ the lab, shaded

def lab(S, t, lx=148, swing=0.0, flicker=1.0):
    lx = lx + math.sin(t * 1.6) * swing
    S.light = (lx, 30, 40)
    # back wall and floor (receive the lamp)
    S.falloff = 60
    S.key = 2.0 * flicker
    S.rect(0, 0, W, 150, 0.14, normal=(0, 0, 1))
    S.rect(0, 150, W, H, 0.2, normal=(0, -1, 0.15))
    # window: night outside, blinds catch a little light
    S.rect(22, 30, 70, 92, 0.05)
    for by in range(32, 92, 4):
        S.rect(23, by, 69, by + 1, 0.55, normal=(0, -0.4, 1))
    S.rect(20, 92, 72, 95, 0.6, normal=(0, -1, 0.4))
    # clock
    S.sphere((100, 32), 8, 8, 0.85)
    # shelves with glassware
    for sy in (46, 70):
        S.rect(218, sy, 306, sy + 3, 0.7, normal=(0, -1, 0.5))
    glass = [(228, 46, 0, 12), (242, 46, 1, 12), (256, 46, 2, 9), (270, 46, 3, 12), (290, 46, 0, 10),
             (226, 70, 2, 8), (240, 70, 0, 11), (258, 70, 3, 11), (280, 70, 1, 12), (296, 70, 2, 9)]
    for x, y, k, h in glass:
        if k == 0:
            S.poly([(x - 5, y), (x + 5, y), (x + 1.5, y - h + 4), (x + 1.5, y - h), (x - 1.5, y - h), (x - 1.5, y - h + 4)], 0.2)
        elif k == 1:
            S.sphere((x, y - 4), 4.5, 4.5, 0.25)
            S.rect(x - 1.2, y - h, x + 1.2, y - 7, 0.25)
        elif k == 2:
            S.rect(x - 4, y - h + 3, x + 4, y, 0.2)
        else:
            for j in range(4):
                S.capsule((x - 6 + j * 4, y - h + 2), (x - 6 + j * 4, y - 3), 1.3, 0.3)
            S.rect(x - 8, y - 5, x + 8, y - 3, 0.6)
    # bench: lit top, shadowed front, drawers, legs
    S.rect(96, 108, 306, 111, 0.9, normal=(0, -1, 0.35))
    S.rect(96, 111, 306, 114, 0.6, normal=(0, 0, 1))
    S.rect(230, 114, 300, 138, 0.45, normal=(0, 0, 1))
    S.rect(98, 114, 103, 150, 0.4)
    S.rect(298, 114, 303, 150, 0.4)
    # microscope (metal, darker), notes, mug
    microscope(S, 150, 108)
    S.rect(182, 106, 197, 108, 0.95, normal=(0, -1, 0.6))
    S.capsule((209, 101), (209, 107), 3.2, 0.6, caps=False)
    S.capsule((213.5, 103), (213.5, 105), 1.2, 0.6)
    # lamp shade (lit inside), cord
    S.rect(lx - 0.5, 0, lx + 0.5, 24, 0.3)
    S.poly([(lx - 3, 23), (lx + 3, 23), (lx + 9, 31), (lx - 9, 31)], 0.35, normal=(0, -1, 0.3))
    return lx, glass


def microscope(S, x, y, s=1.0):
    m = 0.55
    S.rect(x - 2 * s, y - 3 * s, x + 18 * s, y, m, normal=(0, -1, 0.5))
    S.capsule((x + 15 * s, y - 3 * s), (x + 15 * s, y - 19 * s), 2.4 * s, m)
    S.capsule((x + 15 * s, y - 19 * s), (x + 11 * s, y - 29 * s), 2.2 * s, m)
    S.rect(x + 2 * s, y - 13 * s, x + 14 * s, y - 11 * s, 0.7, normal=(0, -1, 0.6))
    S.capsule((x + 8 * s, y - 15 * s), (x + 8 * s, y - 20 * s), 1.7 * s, m)
    S.capsule((x + 10 * s, y - 23 * s), (x - 1 * s, y - 36 * s), 2.3 * s, m)
    S.capsule((x - 1 * s, y - 36 * s), (x - 4 * s, y - 40 * s), 1.7 * s, 0.3)
    S.sphere((x + 16 * s, y - 15 * s), 2.3 * s, 2.3 * s, 0.7)


def red_details(img, glass):
    d = ImageDraw.Draw(img)
    for x, y, k, h in glass[::3]:
        d.line([(x - 2, y - 2), (x + 2, y - 2)], fill=RED)


# ------------------------------------------------------------------ close-ups

PROFILE = [(-9, -6), (-6, -10.5), (-1, -12), (4, -11), (7.5, -8.5), (9, -5), (9.6, -3.4), (9.1, -2.1), (9.6, -0.6),
           (11.7, 3.3), (11.3, 4.2), (9.9, 4.5), (10.0, 5.6), (9.5, 6.3), (9.9, 7.1), (9.1, 8.1), (9.4, 9.6),
           (8.5, 11.2), (5, 11.7), (1.5, 10.3), (-1, 8.6), (-5.5, 5.5), (-9, 2), (-10, -2)]
HAIRLINE = [(-9.7, -6), (-6.4, -11.1), (-1, -12.7), (4.5, -11.7), (8.1, -8.7), (7.6, -7.0), (3.4, -7.6), (0.4, -5.6),
            (-1.6, -2.6), (-3.2, 1.2), (-5.6, 5.6), (-9.4, 2.3), (-10.5, -2)]


def face_closeup(S, t, cx, cy, s, light, look=0.0):
    """A big profile head facing right, from a real profile outline shaded as a rounded form."""
    S.light = light
    S.falloff = 220

    def P(pts):
        return [(cx + x * s, cy + y * s) for x, y in pts]
    S.capsule((cx + 1.5 * s, cy + 9 * s), (cx + 0.5 * s, cy + 24 * s), 4.6 * s, SKIN * 0.5, rb=5.4 * s)    # neck (in the face's shadow)
    S.capsule((cx - 26 * s, cy + 30 * s), (cx + 14 * s, cy + 33 * s), 10 * s, COAT)                       # shoulders
    S.poly(P([(-1, 21), (8, 21), (3.5, 30)]), 0.12)                                                         # shirt V
    S.poly(P([(-3, 20), (3.5, 30), (-1, 34), (-6, 22)]), COAT * 0.8, normal=(-0.4, -0.3, 1))               # lapel
    S.blob(P(PROFILE), (cx + 1 * s, cy - 0.5 * s), 10.5 * s, 11.5 * s, SKIN)
    S.blob(P(HAIRLINE), (cx - 1.5 * s, cy - 3 * s), 10.5 * s, 10.5 * s, HAIR)
    S.sphere((cx - 1.6 * s, cy + 1.2 * s), 1.9 * s, 2.9 * s, SKIN * 0.85)                                 # ear


def face_details(img, cx, cy, s, pupil=1.0, sweat=0.0, veins=False, glow_below=False):
    d = ImageDraw.Draw(img)

    def p(x, y):
        return (cx + x * s, cy + y * s)
    # eyebrow, eye (white almond + iris/pupil looking forward), lids
    d.line([p(6.0, -4.5), p(7.6, -4.9), p(9.1, -4.3)], fill=BG, width=max(1, int(s * 0.5)))
    d.polygon([p(6.6, -2.2), p(7.8, -2.9), p(8.9, -2.1), p(7.8, -1.5)], fill=FG)
    ir = max(1.0, 0.42 * s)
    ix, iy = p(8.15, -2.2)
    d.ellipse([ix - ir, iy - ir, ix + ir * 0.6, iy + ir], fill=DIM)
    pr = max(1.0, 0.24 * s * pupil)
    d.ellipse([ix - pr, iy - pr, ix + pr * 0.6, iy + pr], fill=BG)
    d.line([p(6.6, -2.3), p(7.8, -3.0), p(8.9, -2.2)], fill=BG)
    if veins:
        d.line([p(6.8, -2.1), p(7.3, -2.35)], fill=RED)
        d.line([p(6.8, -2.0), p(7.4, -1.8)], fill=RED)
    # glasses: round lens, bridge over the nose, the arm back over the ear, a glint
    d.ellipse([*p(5.9, -4.0), *p(10.1, -0.4)], outline=FG)
    d.line([p(10.1, -2.4), p(10.6, -1.9)], fill=FG)
    d.line([p(5.9, -2.6), p(-1.2, -1.0)], fill=DIM)
    d.line([p(8.8, -3.6), p(9.4, -3.1)], fill=FG)
    # nostril and mouth line
    d.line([p(10.3, 3.9), p(10.8, 4.0)], fill=BG)
    d.line([p(9.5, 6.4), p(8.3, 6.6)], fill=BG)
    # stubble on the jaw
    rnd = random.Random(2)
    for _ in range(int(s * 9)):
        x, y = rnd.uniform(2, 9), rnd.uniform(6.5, 11)
        if (x - 3) ** 2 / 49 + (y - 4) ** 2 / 50 < 1.1:
            d.point(p(x, y), fill=BG)
    if sweat > 0:
        sx, sy = p(4.5, -6 + sweat * 10)
        d.polygon([(sx, sy - 1.4 * s), (sx + 0.7 * s, sy), (sx, sy + 0.7 * s), (sx - 0.7 * s, sy)], fill=FG)


# ------------------------------------------------------------------ shots

def s_lab(img, t, dur):
    S = Shade()
    lx, glass = lab(S, t, swing=0.6)
    J = doctor(S, "hunch", (114, 100), breathe=math.sin(t * 2) * 0.6)
    S.render(img, black=0.035)
    red_details(img, glass)
    doctor_details(img, J)
    if int(t * 3) % 2 == 0:
        img.putpixel((209, 97 - int(t * 4) % 3), DIM)


def s_eureka(img, t, dur):
    split = 1.7
    if t < split:
        S = Shade()
        lx, glass = lab(S, t, swing=0.6)
        k = ease((t - 0.6) / 0.8)
        pose = lerp_pose(POSES["hunch"], POSES["raise"], k)
        J = doctor(S, pose, (114 - 6 * k, 100 - k))
        S.render(img, black=0.035)
        red_details(img, glass)
        doctor_details(img, J)
        d = ImageDraw.Draw(img)
        if 0.15 < t < 0.7:
            ex, ey = J["eye"]
            d.rectangle([ex + 2, ey - 20, ex + 4, ey - 12], fill=RED)
            d.rectangle([ex + 2, ey - 9, ex + 4, ey - 7], fill=RED)
        if k > 0.6:
            hx, hy = J["fa_hand"]
            d.rectangle([hx - 2, hy - 11, hx + 2, hy - 2], outline=FG)
            d.rectangle([hx - 1, hy - 7, hx + 1, hy - 3], fill=FG)
    else:
        # close-up: his face lit from below by the glowing vial, eyes wide behind the glasses
        tt = t - split
        S = Shade(light=(230, 140, 45), falloff=260, ambient=0.03, key=1.35)
        S.rect(0, 0, W, H, 0.10)
        cx, cy, s = 140, 84, 5.0
        face_closeup(S, tt, cx, cy, s, light=(230, 140, 45))
        S.render(img, rim=False)
        face_details(img, cx, cy, s, pupil=1.4 - ease(tt / 0.6) * 0.6)
        d = ImageDraw.Draw(img)
        # the vial rising into the frame, glowing
        vy = 190 - ease(tt / 0.7) * 60
        d.rectangle([240, vy, 252, vy + 34], outline=FG)
        d.rectangle([241, vy + 10, 251, vy + 33], fill=FG)
        d.rectangle([238, vy - 4, 254, vy], fill=DIM)
        if int(tt * 8) % 2 == 0:
            for a in range(0, 360, 30):
                r = 18 + (a % 60 == 0) * 6
                d.point((246 + math.cos(math.radians(a)) * r, vy + 20 + math.sin(math.radians(a)) * r), fill=FG)


def chain(S, pts, alb):
    """A limb through (x, y, radius) points, as one smooth tapered form."""
    S.tube(pts, alb)


def wrap_fingers(S, x_from, x_to, ys, r, alb, curl=3.0):
    """Fingers wrapped across the front of a vertical object: one bent capsule each, tips curling down."""
    sign = 1 if x_to > x_from else -1
    for k, y in enumerate(ys):
        length = abs(x_to - x_from) - (2 if k in (0, 3) else 0)
        mid = x_from + sign * length * 0.62
        tip = x_from + sign * length
        S.capsule((x_from, y), (mid, y + 0.6), r, alb, rb=r * 0.95)
        S.capsule((mid, y + 0.6), (tip, y + curl), r * 0.95, alb * 0.92, rb=r * 0.85)


def finger_gaps(img, x_from, x_to, ys):
    d = ImageDraw.Draw(img)
    for a, b in zip(ys, ys[1:]):
        y = (a + b) / 2
        d.line([(x_from + (x_to - x_from) * 0.1, y), (x_from + (x_to - x_from) * 0.85, y + 1)], fill=BG)


def dark_bg(img, light, cx, cy, k=0.10):
    S = Shade(light=light, falloff=300, ambient=0.0, key=1.0)
    r = np.hypot((XX - cx) / 160, (YY - cy) / 110)
    S.v[:] = np.clip(1 - r, 0, 1) * k
    S.cov[:] = True
    S.render(img, rim=False)


def s_syringe(img, t, dur):
    k = ease(t / (dur * 0.75))
    LIGHT = (230, -30, 110)
    dark_bg(img, LIGHT, 170, 70, 0.12)
    d = ImageDraw.Draw(img)
    # vial upside down (glass, the cure draining, label, crimped cap) and the syringe filling
    vx, vy0, vy1 = 160, 8, 44
    d.rectangle([vx - 11, vy0, vx + 11, vy1], outline=FG, fill=BG)
    level = int((vy1 - vy0 - 4) * (1 - k))
    if level > 0:
        d.rectangle([vx - 10, vy1 - level, vx + 10, vy1 - 1], fill=FG)
    d.line([(vx + 8, vy0 + 2), (vx + 8, vy1 - 2)], fill=DIM)
    d.rectangle([vx - 6, vy1, vx + 6, vy1 + 5], fill=DIM)
    v2.syringe(d, vx, 66, 126, k)
    S = Shade(light=LIGHT, falloff=300, ambient=0.05, key=1.45)
    # left hand from the upper left: coat cuff, forearm, back of the hand, fingers wrapped round the vial
    S.capsule((-40, -10), (42, 6), 15, COAT, rb=13)
    chain(S, [(30, 6, 10.5), (95, 14, 8.5), (130, 17, 7.5)], SKIN)
    S.sphere((140, 18), 10, 10.5, SKIN)                         # back of the hand
    S.capsule((137, 9), (146, 2), 3.2, SKIN, rb=2.8)             # thumb, over the top
    ys_l = [10.0, 15.4, 20.8, 26.2]
    wrap_fingers(S, 146, 172, ys_l, 2.9, SKIN)
    # right hand from the lower right: cuff, forearm, back of the hand, fingers round the barrel
    S.capsule((360, 200), (292, 160), 15, COAT, rb=13)
    chain(S, [(300, 166, 10.5), (220, 120, 8.5), (184, 104, 7.5)], SKIN)
    S.sphere((174, 100), 10, 11.5, SKIN)
    S.capsule((176, 90), (168, 80), 3.2, SKIN, rb=2.8)           # thumb up the side of the barrel
    ys_r = [90.0, 95.6, 101.2, 106.8]
    wrap_fingers(S, 170, 150, ys_r, 2.9, SKIN)
    S.render(img, rim=False)
    finger_gaps(img, 146, 172, ys_l)
    finger_gaps(img, 170, 150, ys_r)
    # the label stays readable between the fingers
    ImageDraw.Draw(img).rectangle([vx - 10, 31, vx + 10, 41], fill=BG)
    v2.text(img, "AB-1", 31, RED)


def s_inject(img, t, dur):
    LIGHT = (190, -20, 120)
    dark_bg(img, LIGHT, 170, 100, 0.12)
    S = Shade(light=LIGHT, falloff=280, ambient=0.05, key=1.5)
    # upper arm out of the rolled-up coat sleeve, the inner elbow, the forearm narrowing to the wrist
    chain(S, [(10, 112, 17), (70, 113, 16), (102, 114, 15), (140, 115, 15.2), (195, 117, 12), (240, 118, 8.6)], SKIN)
    # palm-up hand: heel, thumb pad and thumb (the top side), the palm, four relaxed curled fingers
    S.sphere((258, 120), 12.5, 10.5, SKIN)
    S.sphere((254, 111), 7, 5.5, SKIN)
    S.capsule((252, 107), (265, 99), 3.8, SKIN, rb=3.4)
    S.capsule((265, 99), (274, 97), 3.4, SKIN, rb=3.0)
    ys = [110.5, 116.0, 121.5, 126.5]
    for j, y in enumerate(ys):
        ln = [12, 13.5, 13, 10][j]
        S.capsule((268, y), (268 + ln * 0.62, y + 0.8), 2.9, SKIN, rb=2.8)
        S.capsule((268 + ln * 0.62, y + 0.8), (268 + ln, y + 4.2), 2.7, SKIN * 0.9, rb=2.4)
    # the rolled sleeve: thick fabric rolls
    S.capsule((-40, 112), (22, 112), 24, COAT * 0.9, rb=22)
    for x in (2, 14, 26):
        S.capsule((x, 90), (x + 3, 134), 5.5, COAT)
    # the syringe's hand, from the upper right
    push = ease((t - 1.6) / 0.9) if t > 1.6 else 0.0
    k = ease(t / 1.3)
    tip = (150 + (1 - k) * 36, 114 - (1 - k) * 18)
    a = math.radians(-25)
    u = (math.cos(a), math.sin(a)); nrm = (-math.sin(a), math.cos(a))

    def at(s_, o=0.0):
        return (tip[0] + u[0] * s_ + nrm[0] * o, tip[1] + u[1] * s_ + nrm[1] * o)
    S2 = Shade(light=LIGHT, falloff=280, ambient=0.05, key=1.5)
    S2.capsule(at(130, -26), at(230, -60), 13, COAT, rb=15)                 # cuff
    chain(S2, [(*at(98, -14), 8.0), (*at(140, -28), 10)], SKIN)            # wrist / forearm
    S2.sphere(at(96, -12), 11, 9, SKIN, ang=a)                            # back of the hand
    for j, off in enumerate((-2.0, 3.6)):                                  # index + middle under the flange
        S2.capsule(at(92, -6 + off * 0.3), at(82, 2 + off), 2.9, SKIN, rb=2.7)
        S2.capsule(at(82, 2 + off), at(78, 8 + off), 2.7, SKIN * 0.9, rb=2.4)
    thumb_end = at(113 - 60 * push, 0)
    S2.capsule(at(100, -16), thumb_end, 3.4, SKIN, rb=3.2)                 # thumb on the plunger
    S.render(img, rim=False)
    d = ImageDraw.Draw(img)
    for x in (2, 14, 26):
        d.line([(x + 6, 92), (x + 9, 132)], fill=BG)                      # fold shadows
    d.line([(100, 102), (101, 108)], fill=BG)                              # elbow crease
    d.line([(100, 120), (102, 126)], fill=BG)
    for a_, b_ in zip(ys, ys[1:]):
        y = (a_ + b_) / 2
        d.line([(270, y + 0.5), (279, y + 2)], fill=BG)
    # veins: up the forearm and across the inner elbow (median cubital)
    vein = [(236, 121), (205, 119), (170, 117), (150, 115), (128, 113), (106, 110)]
    d.line(vein, fill=RED)
    d.line([(150, 115), (135, 120), (112, 123)], fill=RED)
    if push > 0:
        travel = ease((t - 1.8) / 1.2)
        x_end = 150 - travel * 50
        pts = [(x, y) for x, y in vein if x_end <= x <= 150]
        if len(pts) >= 2:
            d.line([(150, 115)] + pts, fill=FG)
    # the syringe itself (crisp glass and steel)
    d.line([at(0 if k < 1 else 5), at(16)], fill=FG)
    d.polygon([at(16, -1.5), at(20, -1.5), at(20, 1.5), at(16, 1.5)], fill=DIM)
    d.polygon([at(20, -5), at(84, -5), at(84, 5), at(20, 5)], fill=BG, outline=FG)
    liquid = 1 - push
    if liquid > 0.02:
        d.polygon([at(21, -4), at(21 + 62 * liquid, -4), at(21 + 62 * liquid, 4), at(21, 4)], fill=FG)
    d.polygon([at(22 + 62 * liquid, -4), at(25 + 62 * liquid, -4), at(25 + 62 * liquid, 4), at(22 + 62 * liquid, 4)], fill=DIM)
    d.line([at(26 + 62 * liquid), at(112 - 60 * push)], fill=DIM, width=2)
    d.polygon([at(84, -9), at(86, -9), at(86, 9), at(84, 9)], fill=FG)
    S2.render(img, rim=False)
    if 1.6 < t < 1.75:
        img.paste(FG, (0, 0, W, H))


def s_reaction(img, t, dur):
    split = 1.8
    racing = t > 0.6
    if t < split:
        # close-up: the heartbeat hits - pupil blows wide, a vein flushes red, sweat runs
        beat = int(t * (3 if racing else 1.2)) % 2 == 0
        S = Shade(light=(270, 40, 70), falloff=240, ambient=0.03, key=1.5 if not beat else 1.2)
        S.rect(0, 0, W, H, 0.10)
        cx, cy, s = 140 + (random.randint(-1, 1) if racing else 0), 86, 5.0
        face_closeup(S, t, cx, cy, s, light=(270, 40, 70))
        S.render(img, rim=False)
        face_details(img, cx, cy, s, pupil=0.8 + ease(t / 1.2) * 1.4, sweat=ease(t / split), veins=racing)
        if racing and beat:
            d = ImageDraw.Draw(img)
            d.rectangle([0, 0, W - 1, H - 1], outline=RED)
            d.rectangle([1, 1, W - 2, H - 2], outline=RED)
    else:
        tt = t - split
        shake = (random.randint(-2, 2), random.randint(-1, 1))
        flick = 0.6 + 0.4 * (int(t * 12) % 3 != 0)
        S = Shade()
        lx, glass = lab(S, t, swing=3.5, flicker=flick)
        k = ease(tt / 1.2)
        pose = lerp_pose(POSES["stand"], POSES["chest"], k)
        J = doctor(S, pose, (100 + shake[0] - 10 * k, 100 + shake[1] + 4 * k))
        S.render(img, black=0.035)
        red_details(img, glass)
        doctor_details(img, J)
        d = ImageDraw.Draw(img)
        d.rectangle([0, 0, W, 34], fill=BG)
        d.line([(0, 34), (W, 34)], fill=DIM)
        pts = v2.ecg_points(t, 0, W - 46, 20, 13)
        for i in range(1, len(pts)):
            fade = i / len(pts)
            d.line([pts[i - 1], pts[i]], fill=(RED if fade > 0.45 else FG) if fade > 0.2 else DIM)
        bpm = int(min(190, 72 + t * 45))
        v2.text(img, str(bpm), 8, RED, x=W - 38)
        v2.text(img, "BPM", 20, DIM, x=W - 38)
        pulse = 0.15 * (int(t * 6) % 2)
        close = ease(tt / (dur - split))
        r = np.hypot((XX - 160) / 160, (YY - 100) / 90)
        v2.dither(img, np.clip((r - (1.2 - close * 0.7 - pulse)) * 2.5, 0, 1) * (YY > 35), BG)


def s_night(img, t, dur):
    S = Shade(light=(236, 102, 14), falloff=60, ambient=0.06, key=1.6)
    rnd = random.Random(4)
    S.rect(0, 0, W, H, 0.0)                        # sky
    for bx in range(0, W, 14):                       # far skyline
        h_ = rnd.randrange(30, 70)
        S.rect(bx, 150 - h_, bx + 13, 150 - h_ + 1, 0.2)
    near = [(8, 52, 78), (66, 40, 104), (114, 84, 120), (206, 46, 92), (258, 58, 70)]
    for bx, bw, bh in near:
        S.rect(bx, 150 - bh, bx + bw, 150, 0.0)
        S.rect(bx, 150 - bh - 1, bx + bw, 150 - bh, 0.35)
    S.capsule((80, 40), (88, 40), 5, 0.15, caps=False)  # water tank
    S.rect(150, 22, 152, 30, 0.5)
    S.rect(0, 150, W, H, 0.4, normal=(0, -1, 0.3))   # wet street, the lamp pools on it
    S.capsule((236, 150), (236, 106), 1.2, 0.5)
    S.capsule((236, 106), (226, 103), 1.2, 0.5)
    S.render(img, rim=False)
    d = ImageDraw.Draw(img)
    for bx, bw, bh in near:
        d.rectangle([bx, 150 - bh, bx + bw, 150], outline=DIM)
        for wy in range(150 - bh + 6, 144, 9):
            for wx in range(bx + 4, bx + bw - 4, 8):
                q = rnd.random()
                if q < 0.08:
                    d.rectangle([wx, wy, wx + 3, wy + 4], fill=FG)
                elif q < 0.5:
                    d.line([(wx, wy + 4), (wx + 3, wy + 4)], fill=DIM)
    wx0, wy0 = 148, 64
    d.rectangle([wx0, wy0, wx0 + 18, wy0 + 13], fill=FG)
    tiny = v2.Layer()
    tiny.ellipse((wx0 + 7, wy0 + 6), 2, 2.3)
    tiny.poly([(wx0 + 4, wy0 + 13), (wx0 + 10, wy0 + 13), (wx0 + 9, wy0 + 8), (wx0 + 5, wy0 + 8)])
    v2.paint(img, tiny.mask(), BG)
    d.point((226, 104), fill=FG)
    for x, y, v in v2.RAIN:
        yy = (y + t * v) % H
        d.line([(x, yy), (x - 2, yy + 4)], fill=DIM)
    z = 1 + ease(t / dur) * 0.9
    cx, cy = wx0 + 9, wy0 + 6
    box = (int(cx - cx / z), int(cy - cy / z), int(cx + (W - cx) / z), int(cy + (H - cy) / z))
    img.paste(img.crop(box).resize((W, H), Image.NEAREST))


def shaded_bacterium(S, x, y, ang, length, t, seed):
    ca, sa = math.cos(ang), math.sin(ang)
    hl = length / 2
    S.capsule((x - ca * hl, y - sa * hl), (x + ca * hl, y + sa * hl), 3.6, 0.9)


def s_scope(img, t, dur):
    cx, cy, R = 160, 84, 70
    S = Shade(light=(110, 30, 60), falloff=200, ambient=0.08, key=1.5)
    rnd = random.Random(3)
    cells = [(cx + rnd.uniform(-55, 55), cy + rnd.uniform(-55, 55), rnd.uniform(0, math.tau), rnd.uniform(8, 14), rnd.random() * 6)
             for _ in range(16)]
    drop_t = 1.4
    alive, dead = [], []
    for x, y, a, ln, sd in cells:
        if (x - cx) ** 2 + (y - cy) ** 2 > (R - 12) ** 2:
            continue
        killed_at = drop_t + 0.4 + math.hypot(x - cx, y - cy) / 20
        xx, yy = x + math.cos(a) * t * 3, y + math.sin(a) * t * 3
        if t < killed_at:
            shaded_bacterium(S, xx, yy, a + math.sin(t + sd) * 0.2, ln, t, sd)
            alive.append((xx, yy, a, ln, sd))
        else:
            dead.append((xx, yy, sd, t - killed_at))
    S.render(img, rim=False, palette=[BG, RED, FG])
    d = ImageDraw.Draw(img)
    for xx, yy, a, ln, sd in alive:   # flagella
        ca, sa = math.cos(a), math.sin(a)
        tail = [(xx - ca * (ln / 2 + 3 + k * 1.6) - sa * math.sin(k * 0.9 - t * 9 + sd) * 2,
                 yy - sa * (ln / 2 + 3 + k * 1.6) + ca * math.sin(k * 0.9 - t * 9 + sd) * 2) for k in range(10)]
        d.line(tail, fill=RED)
    S2 = Shade(light=(110, 30, 60), falloff=200, ambient=0.1, key=1.6)
    for xx, yy, sd, age in dead:
        if age < 0.35:
            for f in range(7):
                fa = f / 7 * math.tau + sd
                d.point((xx + math.cos(fa) * (3 + age / 0.35 * 8), yy + math.sin(fa) * (3 + age / 0.35 * 8)), fill=RED)
        else:
            S2.sphere((xx, yy), 3, 3, 1.0)
    if t > drop_t:
        rr = (t - drop_t) * 20
        n = int(14 + rr * 1.5)
        for k in range(n):
            a = k / n * math.tau + t
            d.point((cx + math.cos(a) * rr, cy + math.sin(a) * rr), fill=FG)
        S2.sphere((cx, cy), 4, 4, 1.0)
    elif t > drop_t - 0.7:
        S2.sphere((cx, cy - 70 + (t - (drop_t - 0.7)) / 0.7 * 70), 3, 4, 1.0)
    S2.render(img, rim=False)
    r = np.hypot(XX - cx, YY - cy)
    a_ = np.asarray(img).copy()
    a_[r > R] = BG
    img.paste(Image.fromarray(a_))
    v2.dither(img, np.clip((r - (R - 10)) / 10, 0, 1) * (r <= R), BG)
    d.ellipse([cx - R - 1, cy - R - 1, cx + R + 1, cy + R + 1], outline=DIM)
    d.ellipse([cx - R - 4, cy - R - 4, cx + R + 4, cy + R + 4], outline=DIM)


def s_dive(img, t, dur):
    if t >= 2.4:
        return v2.s_dive(img, t, dur)
    r = np.hypot(XX - 160, (YY - 90) / 0.62)
    wall = 0.5 + 0.5 * np.sin(np.log(np.maximum(r, 1)) * 9 - t * 14)
    v2.dither(img, wall * np.clip((r - 30) / 120, 0, 1) * 0.5, DIM)
    S = Shade(light=(160, 90, 120), falloff=180, ambient=0.1, key=1.4)
    rnd = random.Random(11)
    cells = []
    for k in range(22):
        a = rnd.random() * math.tau
        sp = ((rnd.random() + t * 0.8) % 1.0) ** 2.2
        cells.append((sp, 160 + math.cos(a) * sp * 230, 90 + math.sin(a) * sp * 140, k))
    for sp, x, y, k in sorted(cells):
        rr = 2 + sp * 14
        tilt = t * 3 + k
        ry = max(1.0, rr * abs(math.cos(tilt)))
        S.sphere((x, y), rr, ry, 0.9, ang=k)
        S.sphere((x, y), rr * 0.45, ry * 0.45, 0.35, ang=k)   # the dimple
    S.render(img, rim=False, palette=[BG, RED, FG])
    cr = 2 + ease(t / 2.4) * 6
    S2 = Shade(light=(150, 70, 60), falloff=200, ambient=0.1, key=1.6)
    S2.sphere((160, 90), cr, cr, 1.0)
    S2.render(img, rim=False)
    if t > 2.2:
        img.paste(FG, (0, 0, W, H))


SHOTS = [
    (5.0, s_night, "Late at night, one lab still had its lights on."),
    (5.0, s_lab, "For years, the doctor had searched for a cure."),
    (5.5, s_scope, "Tonight, one finally worked."),
    (3.4, s_eureka, ""),
    (4.2, s_syringe, "There was no one else to test it on.", 150),
    (3.4, s_inject, ""),
    (5.0, s_reaction, "His body answered at once.", 166),
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
            v2.caption(img, cap, t, dur, cy)
            hd = ImageDraw.Draw(img)
            hd.text((W - 6 - hd.textlength("Hold to skip", font=v2.FONT), 4), "Hold to skip", font=v2.FONT, fill=DIM)
            img.resize((W * SCALE, H * SCALE), Image.NEAREST).save(f"{OUT}/frames/{n:05d}.png")
            n += 1
    return n


if __name__ == "__main__":
    if "stills" in sys.argv:
        render({(1, 40), (3, 15), (3, 40), (4, 30), (5, 30), (6, 12), (6, 50)})
    else:
        print("frames", render())
        subprocess.run(["ffmpeg", "-y", "-loglevel", "error", "-framerate", str(FPS), "-i", f"{OUT}/frames/%05d.png",
                        "-c:v", "libx264", "-preset", "veryfast", "-pix_fmt", "yuv420p", "-r", "30",
                        f"{OUT}/intro_animatic_v3.mp4"], check=True)
        print("ok")
