"""
Localized pixel capsules (Japanese, Korean, Simplified and Traditional Chinese) for Steam.
Same scenes as make_all.py; the ANTIBACTERIA logo stays and the localized title sits under it, drawn the same
way as BACTERIA (white, red extrusion, dark outline; a red face would fill the counters of dense glyphs) from Noto Sans CJK Bold as 1-bit pixels.

Run: python3 make_loc.py [lang ...]   -> Steam/Art/Localized/<lang>/<asset>_<lang>.png
"""
import os
import shutil
import sys

import numpy as np
from PIL import Image, ImageDraw, ImageFont

import make_all as ma
import steamart as sa

NOTO = "/usr/share/fonts/opentype/noto/NotoSansCJK-Bold.ttc"
if not os.path.exists(NOTO):  # macOS fallback is not the same face; run this where Noto CJK is installed
    raise SystemExit("Needs Noto Sans CJK Bold at " + NOTO)

# Steam language -> (red part, white part, Noto collection index)
TITLES = {
    "japanese": ("アンチ", "バクテリア", 0),
    "koreana":  ("안티", "박테리아", 1),
    "schinese": ("抗菌", "大作战", 2),
    "tchinese": ("抗菌", "大作戰", 3),
}
ROOT = os.path.join(ma.OUT, "Localized")
LANG = None          # set per language while drawing
PX = 16              # title glyph size in canvas pixels
_draw_logo = sa.draw_logo


def title_masks(px):
    """White title mask (all in B) at px pixels, cropped to the ink."""
    red, white, idx = TITLES[LANG]
    font = ImageFont.truetype(NOTO, px, index=idx)
    def m(text):
        im = Image.new("1", (int(font.getlength(text)) + 8, px * 2), 0)
        d = ImageDraw.Draw(im)
        d.fontmode = "1"
        d.text((4, px // 2), text, font=font, fill=1)
        return np.array(im, bool)
    whole = m(red + white)
    split = 4 + round(font.getlength(red))
    ys, xs = np.nonzero(whole)
    whole = whole[ys.min():ys.max() + 1]
    A = np.zeros_like(whole); B = whole.copy()   # all white (see the module note)
    xs0, xs1 = xs.min(), xs.max() + 1
    return A[:, xs0:xs1], B[:, xs0:xs1]


def draw_masks(c, A, B, cx, top, outline, d):
    """steamart.draw_logo's drawing, for any pair of masks."""
    pad = outline + d + 1
    h, w = A.shape
    Am = np.zeros((h + 2 * pad, w + 2 * pad), bool); Bm = Am.copy()
    Am[pad:pad + h, pad:pad + w] = A; Bm[pad:pad + h, pad:pad + w] = B
    letters = Am | Bm
    sh = np.zeros_like(letters)
    for k in range(1, d + 1): sh |= np.roll(letters, k, 0)
    sh &= ~letters
    halo = sa.dilate(letters | sh, outline)
    x0, y0 = int(cx - Am.shape[1] // 2), int(top - pad)
    H, W = c.shape
    for mask, val in ((halo, 0), (sh, 2), (Bm, 1), (Am, 2)):
        ys, xs = np.nonzero(mask)
        ys, xs = ys + y0, xs + x0
        ok = (ys >= 0) & (ys < H) & (xs >= 0) & (xs < W)
        c[ys[ok], xs[ok]] = val
    ys, xs = np.nonzero(halo)
    return x0 + xs.min(), y0 + ys.min(), x0 + xs.max() + 1, y0 + ys.max() + 1


def draw_logo_and_title(c, scale, cx, top, stacked=False, outline=2, depth=None, gap=None, value_map=None, lift=0):
    """The logo moved up by `lift` canvas pixels, then the localized title under it."""
    box = _draw_logo(c, scale, cx, top - lift, stacked, outline, depth, gap, value_map)
    # Biggest glyph size whose title stays inside the frame and about as wide as the logo.
    limit = min(c.shape[1] - 10, max(box[2] - box[0], 0.8 * c.shape[1]))
    for PXS in (PX, 14, 12):
        A, B = title_masks(PXS)
        if A.shape[1] <= limit: break
    o = min(outline, 2)
    return draw_masks(c, A, B, cx, box[3] + o + 1, o, 1)


def with_title(px, lift, fn, *a):
    global PX
    PX = px
    sa.draw_logo = lambda c, scale, cx, top, **k: draw_logo_and_title(c, scale, cx, top, lift=lift, **k)
    try:
        return fn(*a)
    finally:
        sa.draw_logo = _draw_logo


def moved(fn, moves):
    """Run a make_all scene with some sprites moved out of the title's way ((name, x, y) -> (x, y), or None to drop)."""
    real = sa.blit
    def blit(c, spr, x, y, *a, **k):
        to = moves.get((spr, x, y), (x, y))
        return None if to is None else real(c, spr, to[0], to[1], *a, **k)
    def run():
        sa.blit = blit
        try:
            return fn()
        finally:
            sa.blit = real
    return run


def small():
    """Small capsule at 4x (5x leaves no room for a second line): logo, then the title."""
    W, H, s = 462, 174, 4
    c = sa.canvas(*sa.size_for(W, H, s))          # 116 x 44
    ma.field(c, 11, density=0.01, circles=[(4, 44, 14), (114, 0, 12)])
    sa.blit(c, "enemy_spiky", 5, 4)
    sa.blit(c, "enemy_big", 114, 44)
    sa.blit(c, "enemy_bullet", 104, 5)
    global PX
    PX = 12
    box = _draw_logo(c, 1, c.shape[1] // 2, 5, outline=1)
    A, B = title_masks(PX)
    draw_masks(c, A, B, c.shape[1] // 2, box[3] + 2, 1, 1)
    return sa.export(c, W, H, s, f"{ma.OUT}/store/store_capsule_small.png")


def logo():
    """Transparent logo plus title, at the largest whole pixel size that fits the 1280 x 720 frame."""
    W, H = 1280, 720
    global PX
    PX = 16
    A, B = title_masks(PX)
    lm = sa.logo_masks(2)[0]
    lh = lm.shape[0]
    widest = max(lm.shape[1], A.shape[1]) + 12
    s = min(14, W // widest)
    c = sa.canvas(*sa.size_for(W, H, s), fill=-1)
    total = lh + 6 + 3 + A.shape[0]
    box = _draw_logo(c, 2, c.shape[1] // 2, (c.shape[0] - total) // 2, outline=1)
    draw_masks(c, A, B, c.shape[1] // 2, box[3] + 2, 1, 1)
    return sa.export(c, W, H, s, f"{ma.OUT}/library/library_logo.png", transparent=True)


def make(lang):
    global LANG
    LANG = lang
    final = os.path.join(ROOT, lang)
    tmp = os.path.join(ROOT, "_tmp_" + lang)
    base = ma.OUT
    ma.OUT = tmp
    try:
        with_title(16, 6, moved(ma.store_header, {("vitamin", 74, 39): None}))
        with_title(16, 8, moved(ma.store_main, {("enemy_sunux", 120, 56): (108, 64), ("vitamin", 82, 54): (76, 66)}))
        with_title(16, 6, ma.store_vertical)
        with_title(16, 6, ma.library_capsule)
        ma.library_header()
        small()
        logo()
    finally:
        ma.OUT = base
    os.makedirs(final, exist_ok=True)
    for sub in ("store", "library"):
        for f in sorted(os.listdir(os.path.join(tmp, sub))):
            name, ext = os.path.splitext(f)
            dst = os.path.join(final, f"{name}_{lang}{ext}")
            shutil.move(os.path.join(tmp, sub, f), dst)
            print(dst, Image.open(dst).size)
    shutil.rmtree(tmp)


if __name__ == "__main__":
    for lang in (sys.argv[1:] or TITLES):
        make(lang)
