"""
AntiBacteria Steam art: every capsule, header and icon drawn the way the game draws itself.

- Sprites are the game's own PNGs (black = background, white = foreground, red = accent).
- Text is the game's font (m6x11) at whole-number sizes.
- Each image is drawn on a small low-res canvas in palette indices (0 bg, 1 fg, 2 accent, -1 transparent),
  then scaled up by a whole number with no smoothing and cropped to Steam's exact size.

Run:  python3 steamart.py <sprites dir> <font.ttf> <output dir> [palette]
"""
import math
import os
import sys

import numpy as np
from PIL import Image, ImageDraw, ImageFont

PALETTES = {
    "classic": [(11, 11, 16), (242, 242, 242), (255, 59, 59)],
}

SPRITES = FONT = None
_cache = {}


# ---------------------------------------------------------------- basics

def canvas(w, h, fill=0):
    return np.full((h, w), fill, dtype=np.int8)


def sprite(name):
    if name not in _cache:
        sub = "Bosses/" if (name.startswith("boss_") or name.startswith("ana_") or name.startswith("eye_boss") or name.startswith("eye_frunctus")) else ""
        if name.startswith("fx_"): sub = "Fx/"
        if name.startswith("bonus_"): sub = "Bonus/"
        if name.startswith("mode_"): sub = "Modes/"
        im = Image.open(os.path.join(SPRITES, sub + name + ".png")).convert("RGBA")
        a = np.array(im)
        idx = np.full(a.shape[:2], -1, dtype=np.int8)
        op = a[:, :, 3] > 127
        red = (a[:, :, 0] > 127) & (a[:, :, 1] < 128)
        white = (a[:, :, 0] > 127) & (a[:, :, 1] > 127)
        idx[op] = 0
        idx[op & white] = 1
        idx[op & red] = 2
        _cache[name] = idx
    return _cache[name]


def blit(c, spr, x, y, center=True, flip=False):
    if isinstance(spr, str): spr = sprite(spr)
    if flip: spr = spr[:, ::-1]
    h, w = spr.shape
    if center: x, y = x - w // 2, y - h // 2
    x, y = int(round(x)), int(round(y))
    H, W = c.shape
    x0, y0, x1, y1 = max(0, x), max(0, y), min(W, x + w), min(H, y + h)
    if x0 >= x1 or y0 >= y1: return
    sub = spr[y0 - y:y1 - y, x0 - x:x1 - x]
    dst = c[y0:y1, x0:x1]
    m = sub >= 0
    dst[m] = sub[m]


def put(c, x, y, v):
    x, y = int(round(x)), int(round(y))
    if 0 <= y < c.shape[0] and 0 <= x < c.shape[1]: c[y, x] = v


def dust(c, density, rng, value=1):
    H, W = c.shape
    n = int(W * H * density)
    for _ in range(n):
        x, y = rng.integers(0, W), rng.integers(0, H)
        if c[y, x] == 0: c[y, x] = value


def dotted_circle(c, cx, cy, r, step=4.0, value=1, phase=0.0):
    n = max(8, int(2 * math.pi * r / step))
    for i in range(n):
        a = phase + 2 * math.pi * i / n
        put(c, cx + r * math.cos(a), cy + r * math.sin(a), value)


def plus(c, x, y, v=1):
    for dx, dy in ((0, 0), (1, 0), (-1, 0), (0, 1), (0, -1)): put(c, x + dx, y + dy, v)


def shots(c, x0, y0, x1, y1, first=10, spacing=9, last=8):
    """Player shots (white crosses) flying from the player toward a target."""
    d = math.hypot(x1 - x0, y1 - y0)
    ux, uy = (x1 - x0) / d, (y1 - y0) / d
    t = first
    while t < d - last:
        plus(c, x0 + ux * t, y0 + uy * t)
        t += spacing


def orbs(c, pts):
    for x, y in pts: blit(c, "enemy_bullet", x, y)


def pop(c, x, y, r=5):
    """Kill effect: dotted ring + debris."""
    dotted_circle(c, x, y, r, step=2.5)
    plus(c, x, y, 1)


def snake(c, head, segs, x, y, ang, gap=None, wobble=0.35, eye_flip=False):
    """Segmented enemy: head then shrinking ring segments trailing behind along a curve."""
    pts = [(x, y)]
    a = ang + math.pi
    px, py = x, y
    sizes = [sprite(s).shape[0] for s in segs]
    prev = sprite(head).shape[0]
    for i, s in enumerate(segs):
        step = (prev + sizes[i]) / 2 + (gap if gap is not None else 1)
        a += wobble * math.sin(i * 0.9)
        px, py = px + step * math.cos(a), py + step * math.sin(a)
        pts.append((px, py))
        prev = sizes[i]
    for (sx, sy), s in reversed(list(zip(pts[1:], segs))): blit(c, s, sx, sy)
    blit(c, head, x, y, flip=eye_flip)


def player(c, x, y, aim):
    blit(c, "player_cell", x, y)
    blit(c, "player_nucleus", x + round(2 * math.cos(aim)), y + round(2 * math.sin(aim)))


# ---------------------------------------------------------------- logo

def text_mask(text, scale=1):
    im = Image.new("1", (len(text) * 12 + 8, 24), 0)
    d = ImageDraw.Draw(im)
    d.fontmode = "1"
    d.text((2, 2), text, font=FONT, fill=1)
    a = np.array(im, dtype=bool)
    ys, xs = np.nonzero(a)
    a = a[ys.min():ys.max() + 1, xs.min():xs.max() + 1]
    return np.kron(a, np.ones((scale, scale), dtype=bool))


def dilate(m, r=1):
    out = m.copy()
    for dy in range(-r, r + 1):
        for dx in range(-r, r + 1):
            if dx == dy == 0: continue
            s = np.roll(np.roll(m, dy, 0), dx, 1)
            out |= s
    return out


def logo_masks(scale, stacked=False, gap=None):
    """(anti, bacteria) masks positioned in one frame: side by side, or ANTI above BACTERIA."""
    anti, bact = text_mask("ANTI", scale), text_mask("BACTERIA", scale)
    if not stacked:
        whole = text_mask("ANTIBACTERIA", scale)
        h, w = whole.shape
        split = w - bact.shape[1]
        A = np.zeros_like(whole); B = np.zeros_like(whole)
        A[:, :split] = whole[:, :split]; B[:, split:] = whole[:, split:]
        return A, B
    g = gap if gap is not None else 2 * scale
    w = bact.shape[1]
    h = anti.shape[0] + g + bact.shape[0]
    A = np.zeros((h, w), bool); B = np.zeros((h, w), bool)
    ax = (w - anti.shape[1]) // 2
    A[:anti.shape[0], ax:ax + anti.shape[1]] = anti
    B[anti.shape[0] + g:, :] = bact
    return A, B


def draw_logo(c, scale, cx, top, stacked=False, outline=2, depth=None, gap=None, value_map=None):
    """
    ANTI in accent + BACTERIA in foreground, extruded downward in red, with a
    background-coloured outline so it reads over the art. Returns the box (x0, y0, x1, y1) on the canvas.
    """
    A, B = logo_masks(scale, stacked, gap)
    d = depth if depth is not None else scale
    pad = outline + d + 1
    h, w = A.shape
    Am = np.zeros((h + 2 * pad, w + 2 * pad), bool)
    Bm = Am.copy()
    Am[pad:pad + h, pad:pad + w] = A
    Bm[pad:pad + h, pad:pad + w] = B
    letters = Am | Bm
    shA = np.zeros_like(letters); shB = np.zeros_like(letters)
    for k in range(1, d + 1):
        shA |= np.roll(Am, k, 0); shB |= np.roll(Bm, k, 0)
    shA &= ~letters; shB &= ~letters
    halo = dilate(letters | shA | shB, outline)
    x0, y0 = int(cx - Am.shape[1] // 2), int(top - pad)
    H, W = c.shape
    for mask, val in ((halo, 0), (shA, 2), (shB, 2), (Bm, 1), (Am, 2)):
        ys, xs = np.nonzero(mask)
        ys, xs = ys + y0, xs + x0
        ok = (ys >= 0) & (ys < H) & (xs >= 0) & (xs < W)
        c[ys[ok], xs[ok]] = val
    ys, xs = np.nonzero(halo)
    return x0 + xs.min(), y0 + ys.min(), x0 + xs.max() + 1, y0 + ys.max() + 1


# ---------------------------------------------------------------- output

def export(c, W, H, s, path, palette="classic", transparent=False, fmt=None):
    pal = PALETTES[palette]
    h, w = c.shape
    rgba = np.zeros((h, w, 4), np.uint8)
    for i, col in enumerate(pal):
        rgba[c == i, :3] = col
        rgba[c == i, 3] = 255
    if not transparent:
        rgba[c < 0, :3] = pal[0]
        rgba[c < 0, 3] = 255
    im = Image.fromarray(rgba, "RGBA").resize((w * s, h * s), Image.NEAREST)
    ox, oy = (im.width - W) // 2, (im.height - H) // 2
    im = im.crop((ox, oy, ox + W, oy + H))
    if path is None: return im.convert("RGBA" if transparent else "RGB")  # in memory only
    os.makedirs(os.path.dirname(path), exist_ok=True)
    if transparent:
        im.save(path)
    else:
        im = im.convert("RGB")
        if path.lower().endswith(".jpg"): im.save(path, quality=95, subsampling=0)
        else: im.save(path)
    return im


def size_for(W, H, s):
    return math.ceil(W / s), math.ceil(H / s)
