"""Downwell-style pixel style target for AntiBacteria.

Draws one gameplay moment at native 320x180 using palette INDICES only
(0 = background, 1 = foreground white, 2 = accent), then maps the indices
through different palettes and upscales with nearest-neighbour.
"""
import math, random
import numpy as np
from PIL import Image, ImageDraw, ImageFont

W, H = 320, 180
BG, FG, AC = 0, 1, 2
img = np.zeros((H, W), dtype=np.uint8)
rnd = random.Random(7)


def px(x, y, c):
    x, y = int(round(x)), int(round(y))
    if 0 <= x < W and 0 <= y < H:
        img[y, x] = c


def circle(cx, cy, r, c, step=1):
    """1px outline (midpoint); step>1 draws a dotted outline."""
    pts = set()
    x, y, d = r, 0, 1 - r
    while x >= y:
        for a, b in ((x, y), (y, x), (-y, x), (-x, y), (-x, -y), (-y, -x), (y, -x), (x, -y)):
            pts.add((cx + a, cy + b))
        y += 1
        if d < 0:
            d += 2 * y + 1
        else:
            x -= 1
            d += 2 * (y - x) + 1
    for i, (a, b) in enumerate(sorted(pts, key=lambda p: math.atan2(p[1] - cy, p[0] - cx))):
        if i % step == 0:
            px(a, b, c)


def disc(cx, cy, r, c, dither=False):
    for y in range(-r, r + 1):
        for x in range(-r, r + 1):
            if x * x + y * y <= r * r + r * 0.6:
                if not dither or (cx + x + cy + y) % 2 == 0:
                    px(cx + x, cy + y, c)


def line(x0, y0, x1, y1, c):
    n = int(max(abs(x1 - x0), abs(y1 - y0))) or 1
    for i in range(n + 1):
        px(x0 + (x1 - x0) * i / n, y0 + (y1 - y0) * i / n, c)


def stamp(x, y, rows, cmap):
    for j, row in enumerate(rows):
        for i, ch in enumerate(row):
            if ch in cmap:
                px(x + i, y + j, cmap[ch])


FONT = {  # 3x5 pixel font
    '0': ["XXX", "X.X", "X.X", "X.X", "XXX"], '1': [".X.", "XX.", ".X.", ".X.", "XXX"],
    '2': ["XXX", "..X", "XXX", "X..", "XXX"], '3': ["XXX", "..X", ".XX", "..X", "XXX"],
    '4': ["X.X", "X.X", "XXX", "..X", "..X"], '5': ["XXX", "X..", "XXX", "..X", "XXX"],
    '6': ["XXX", "X..", "XXX", "X.X", "XXX"], '7': ["XXX", "..X", ".X.", ".X.", ".X."],
    '8': ["XXX", "X.X", "XXX", "X.X", "XXX"], '9': ["XXX", "X.X", "XXX", "..X", "XXX"],
    '-': ["...", "...", "XXX", "...", "..."], 'x': ["...", "X.X", ".X.", "X.X", "..."],
}


def text(x, y, s, c):
    for ch in s:
        stamp(x, y, FONT[ch], {'X': c})
        x += 4


# ---------------------------------------------------------------- background
for _ in range(int(W * H * 0.0007)):                # microscope dust (sparse)
    px(rnd.randrange(W), rnd.randrange(H), FG)
circle(58, 38, 34, FG, step=7)                      # faint giant cells (dotted = "dim" without a 4th colour)
circle(250, 160, 46, FG, step=8)

# arena wall on the right: a living membrane
for y in range(H):
    edge = int(300 + 2.2 * math.sin(y / 8.5) + 1.2 * math.sin(y / 3.1))
    for x in range(edge, W):
        px(x, y, FG)
    for x in range(edge - 3, edge):                 # dithered inner band
        if (x + y) % 2 == 0:
            px(x, y, FG)
    if y % 11 == 0:                                 # pores
        disc(edge + 7, y + 3, 2, BG)

# ---------------------------------------------------------------- gameplay
# player: solid white cell, nucleus offset toward the aim (up-right) instead of rotating the sprite
P = (150, 100)
circle(*P, 9, FG, step=3)                           # halo
disc(*P, 6, FG)
disc(P[0] + 2, P[1] - 2, 2, BG)
# player shots: solid white pellets stretched along the shot direction (never confused with dust)
def pellet(cx, cy, dx, dy, length=5):
    n = math.hypot(dx, dy); dx, dy = dx / n, dy / n
    for i in range(length):
        x, y = cx + dx * (i - length / 2), cy + dy * (i - length / 2)
        px(x, y, FG); px(x + 1, y, FG)

for k in (15, 28, 41):
    pellet(P[0] + k * 0.72, P[1] - k * 0.69, 0.72, -0.69)

# spiky enemies (BlueCorosia-like): white outline, red core = "this hurts"
def spiky(cx, cy, r=5):
    disc(cx, cy, r, BG)
    circle(cx, cy, r, FG)
    for a in range(8):
        t = a * math.pi / 4 + 0.3
        line(cx + math.cos(t) * r, cy + math.sin(t) * r, cx + math.cos(t) * (r + 3), cy + math.sin(t) * (r + 3), FG)
    disc(cx, cy, 2, AC)

spiky(205, 45)
spiky(92, 62)
spiky(232, 118)

# big enemy (BigBlueCorosia-like): dithered body, red core, shooting
ENEMY_BULLET = [".XXX.", "X...X", "X.X.X", "X...X", ".XXX."]
B = (255, 70)
disc(*B, 13, FG, dither=True)
circle(*B, 13, FG)
disc(*B, 4, AC)
px(B[0] - 1, B[1] - 2, FG)
for i, (dx, dy) in enumerate(((-1.0, 0.55), (-0.55, 1.0), (-1.0, -0.2))):   # enemy bullets: red diamonds
    for k in (20, 34):
        ex, ey = int(B[0] + dx * k), int(B[1] + dy * k)
        disc(ex, ey, 3, BG)
        stamp(ex - 2, ey - 2, ENEMY_BULLET, {'X': AC})

# snake (Serpentus-like): chain of segments, head white with red eyes
seg = []
radii = [5, 4, 4, 4, 3, 3, 3, 2, 2]
x = 50.0
for i, r in enumerate(radii):
    seg.append((int(x), int(146 - 12 * math.sin(x / 16.0))))
    x += (radii[i] + radii[min(i + 1, len(radii) - 1)]) + 2
for i, (sx, sy) in reversed(list(enumerate(seg))):
    r = radii[i]
    disc(sx, sy, r + 1, BG)
    if i == 0:
        disc(sx, sy, r, FG)
        px(sx - 2, sy - 1, AC); px(sx - 2, sy + 1, AC)
    else:
        circle(sx, sy, r, FG)
        px(sx, sy, FG)

# a kill: dotted shockwave ring, debris, dropped vitamins (red capsules)
K = (118, 112)
circle(*K, 10, FG, step=3)
for a in range(7):
    t = a * 0.9
    px(K[0] + math.cos(t) * 14, K[1] + math.sin(t) * 14, FG)
VIT = [".XXX.", "XXWXX", ".XXX."]
for vx, vy in ((112, 120), (124, 106), (106, 104)):
    stamp(vx, vy, VIT, {'X': AC, 'W': FG})

# ---------------------------------------------------------------- HUD (inside a safe margin)
def clear(x0, y0, x1, y1):
    img[y0:y1, x0:x1] = BG

clear(5, 4, 38, 16); clear(127, 4, 194, 16); clear(264, 4, 298, 15); clear(5, 165, 22, 175)
HEART = [".XX.XX.", "XXXXXXX", "XXXXXXX", ".XXXXX.", "..XXX..", "...X..."]
HEART_EMPTY = [".XX.XX.", "X..X..X", "X.....X", ".X...X.", "..X.X..", "...X..."]
for i in range(3):
    stamp(8 + i * 9, 7, HEART if i < 2 else HEART_EMPTY, {'X': AC})
# slow-motion meter
for x in range(130, 191):
    px(x, 7, FG); px(x, 12, FG)
for y in range(7, 13):
    px(130, y, FG); px(190, y, FG)
for x in range(132, 174):
    for y in range(9, 11):
        px(x, y, FG)
# vitamins counter
stamp(268, 8, VIT, {'X': AC, 'W': FG})
text(275, 7, "x1250", FG)
# level
text(8, 168, "1-3", FG)

np.save('/home/claude/style/indices.npy', img)

# ---------------------------------------------------------------- palettes
PALETTES = [
    ("Classic", ["#0b0b10", "#f2f2f2", "#ff3b3b"]),
    ("Petri dish", ["#0f241b", "#d9f7c4", "#ff6fae"]),
    ("Gram stain", ["#1b1030", "#efe6ff", "#ffb41f"]),
    ("Colour-blind safe", ["#000000", "#ffffff", "#2fa8ff"]),
]


def render(pal, scale):
    rgb = np.array([[int(h[i:i + 2], 16) for i in (1, 3, 5)] for h in pal], dtype=np.uint8)
    return Image.fromarray(rgb[img]).resize((W * scale, H * scale), Image.NEAREST)


render(PALETTES[0][1], 4).save('/home/claude/style/antibacteria_style_target_downwell.png')

# 2x2 sheet of palette swaps
tile = 2
tw, th = W * tile, H * tile
pad, lab = 16, 28
sheet = Image.new("RGB", (pad + 2 * (tw + pad), pad + 2 * (th + lab + pad)), (255, 255, 255))
d = ImageDraw.Draw(sheet)
try:
    font = ImageFont.load_default(size=18)
except TypeError:
    font = ImageFont.load_default()
for i, (name, pal) in enumerate(PALETTES):
    cx, cy = pad + (i % 2) * (tw + pad), pad + (i // 2) * (th + lab + pad)
    sheet.paste(render(pal, tile), (cx, cy))
    for j, h in enumerate(pal):
        d.rectangle([cx + j * 22, cy + th + 6, cx + j * 22 + 16, cy + th + 22], fill=h, outline="#888888")
    d.text((cx + 76, cy + th + 5), name, fill=(30, 30, 30), font=font)
sheet.save('/home/claude/style/antibacteria_palette_swaps.png')
print("ok", np.bincount(img.ravel(), minlength=3))
