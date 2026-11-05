"""
Draws the PlayStation Store images for AntiBacteria into PlayStation/Store/Art, in the same pixel style as the Steam art
(Steam/Tools/steamart.py: the game's own sprites and font, scaled by whole numbers).

Run from the repo:  python3 PlayStation/Tools/make_ps.py      (needs numpy + Pillow; the PSD is written by psd.py here)

Sizes follow Sony's Content Pipeline asset specs (Asset Submission Guidelines, 2026):
  Master Image 2160x2160 (logo required), 16x9 Master 3840x2160 JPG, 4x3 Master 2880x2160, Portrait Master 1440x2160,
  Cover Art 3840x2160 (no text or logos, focus in the right third), Logo (horizontal, 1440-2880 x 296-592, transparent,
  tightly cropped), Screenshots 3840x2160 JPG (no added text), Featured PSD 3840x2160 (Logo | Primary Art | Background).
"""
import glob
import math
import os
import sys

import numpy as np
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.normpath(os.path.join(HERE, "../.."))
sys.path.insert(0, os.path.join(ROOT, "Steam/Tools"))
sys.path.insert(0, HERE)
import steamart as sa  # noqa: E402
import psd  # noqa: E402

ASSETS = os.path.join(ROOT, "Unity/AntiBacteria_Unity/Assets")
sa.SPRITES = ASSETS + "/_Project/Art/Sprites"
sa.FONT = sa.ImageFont.truetype(ASSETS + "/Fonts/m6x11cyrillic.ttf", 16)
OUT = os.path.join(ROOT, "PlayStation/Store/Art")
NAME = "AntiBacteria_StandardEdition"      # Sony filename convention: TITLE_CONTENTTYPE_ASSET_SIZE_LIFECYCLE

SNAKE = ["snake_segment_r4", "snake_segment_r4", "snake_segment_r3", "snake_segment_r3", "snake_segment_r2", "snake_segment_r2"]
ANA = ["ana_segment_r7", "ana_segment_r7", "ana_segment_r6", "ana_segment_r6", "ana_segment_r5", "ana_segment_r5"]
S = 12   # 3840 / 12 = 320, 2160 / 12 = 180: the game's own 320 x 180 pixel grid


def field(c, seed, density=0.004, circles=()):
    rng = np.random.default_rng(seed)
    for (x, y, r) in circles: sa.dotted_circle(c, x, y, r, step=3.5, phase=r * 0.1)
    sa.dust(c, density, rng)


def fight(c, px, py, bx, by, boss="boss_inferno", shot_end=24, orbs=()):
    aim = math.atan2(by - py, bx - px)
    sa.blit(c, boss, bx, by)
    sa.shots(c, px, py, bx - shot_end * math.cos(aim), by - shot_end * math.sin(aim), first=11, spacing=10)
    sa.player(c, px, py, aim)
    sa.orbs(c, orbs)


def logo_scale(max_w, stacked=False):
    """Largest whole-number logo scale whose drawn width fits max_w canvas pixels."""
    best = 1
    for k in range(1, 8):
        A, B = sa.logo_masks(k, stacked)
        if A.shape[1] + 2 * (2 + k + 1) <= max_w: best = k
    return best


def name(asset, w, h, ext="png"):
    return f"{OUT}/{NAME}_{asset}_{w}x{h}_R.{ext}"


# ------------------------------------------------------------------ scenes (canvas pixels, 320 x 180 at S = 12)

def scene_wide(c, seed, logo=True):
    """The 16:9 battle: player in the middle-right firing at Inferno on the right, the swarm on the left."""
    w, h = c.shape[1], c.shape[0]
    field(c, seed, circles=[(30, h - 20, 44), (w - 30, 14, 30), (w * 0.62, h - 26, 12)])
    fight(c, int(w * 0.66), int(h * 0.62), w - 34, int(h * 0.62),
          orbs=[(w * 0.8, h * 0.5), (w * 0.78, h * 0.76), (w * 0.73, h * 0.56), (w * 0.84, h * 0.84)])
    sa.blit(c, "enemy_spiky", int(w * 0.5), int(h * 0.56))
    sa.blit(c, "enemy_chorux", int(w * 0.42), int(h * 0.74))
    sa.blit(c, "enemy_big", int(w * 0.56), int(h * 0.86))
    sa.blit(c, "enemy_sunux", int(w * 0.12), int(h * 0.4))
    sa.blit(c, "enemy_zigzag", int(w * 0.34), int(h * 0.58))
    sa.snake(c, "snake_head", SNAKE, int(w * 0.24), int(h * 0.8), ang=0.3, wobble=0.45)
    sa.pop(c, int(w * 0.6), int(h * 0.48), 4)
    sa.blit(c, "vitamin", int(w * 0.62), int(h * 0.4))
    sa.blit(c, "vitamin", int(w * 0.7), int(h * 0.9))
    if logo:
        sa.draw_logo(c, logo_scale(int(w * 0.85)), w // 2, int(h * 0.08), outline=2)


def master_square():
    W = H = 2160
    c = sa.canvas(*sa.size_for(W, H, S))           # 180 x 180
    w, h = c.shape[1], c.shape[0]
    field(c, 51, circles=[(16, h - 24, 40), (w - 10, 30, 28), (w * 0.5, h - 18, 10)])
    bx, by = w // 2 + 30, h - 34
    px, py = w // 2 - 22, int(h * 0.56)
    fight(c, px, py, bx, by, orbs=[(px + 28, py + 6), (px - 14, py + 24), (px + 10, py + 34), (px + 40, py + 30)])
    sa.blit(c, "enemy_spiky", 26, int(h * 0.48))
    sa.blit(c, "enemy_chorux", w - 24, int(h * 0.5))
    sa.blit(c, "enemy_sunux", w - 30, int(h * 0.76))
    sa.snake(c, "snake_head", SNAKE, 22, int(h * 0.76), ang=0.9, wobble=0.5)
    sa.pop(c, px + 22, py - 18, 4)
    sa.blit(c, "vitamin", px - 22, py - 10)
    # The one-line logo would fill the whole width; stacked it can be large and still sit inside the safe zone.
    sa.draw_logo(c, logo_scale(int(w * 0.72), stacked=True), w // 2, int(h * 0.1), stacked=True)
    return sa.export(c, W, H, S, name("MasterImage", W, H))


def master_16x9():
    W, H = 3840, 2160
    c = sa.canvas(*sa.size_for(W, H, S))
    scene_wide(c, 53)
    return sa.export(c, W, H, S, name("MasterImage16x9", W, H, "jpg"))


def master_4x3():
    W, H = 2880, 2160
    c = sa.canvas(*sa.size_for(W, H, S))           # 240 x 180
    w, h = c.shape[1], c.shape[0]
    field(c, 55, circles=[(20, h - 20, 40), (w - 16, 20, 26), (w * 0.55, h - 22, 10)])
    fight(c, int(w * 0.5), int(h * 0.64), w - 32, int(h * 0.66),
          orbs=[(w * 0.68, h * 0.54), (w * 0.64, h * 0.8), (w * 0.6, h * 0.58), (w * 0.72, h * 0.88)])
    sa.blit(c, "enemy_spiky", int(w * 0.3), int(h * 0.58))
    sa.blit(c, "enemy_chorux", int(w * 0.16), int(h * 0.5))
    sa.blit(c, "enemy_big", int(w * 0.36), int(h * 0.86))
    sa.blit(c, "enemy_sunux", int(w * 0.8), int(h * 0.36))
    sa.snake(c, "snake_head", SNAKE, 16, int(h * 0.8), ang=0.25, wobble=0.45)
    sa.pop(c, int(w * 0.42), int(h * 0.48), 4)
    sa.blit(c, "vitamin", int(w * 0.46), int(h * 0.42))
    sa.draw_logo(c, logo_scale(int(w * 0.84)), w // 2, int(h * 0.14))
    return sa.export(c, W, H, S, name("MasterImage4x3", W, H))


def master_portrait():
    W, H = 1440, 2160
    c = sa.canvas(*sa.size_for(W, H, S))           # 120 x 180
    w, h = c.shape[1], c.shape[0]
    field(c, 57, circles=[(8, h * 0.56, 30), (w - 6, h * 0.34, 22), (w * 0.5, h - 26, 12)])
    bx, by = w // 2 + 4, h - 12
    px, py = w // 2 - 8, int(h * 0.58)
    aim = math.atan2(by - py, bx - px)
    sa.blit(c, "boss_inferno", bx, by)
    sa.shots(c, px, py, bx - 22 * math.cos(aim), by - 22 * math.sin(aim), first=11, spacing=9)
    sa.player(c, px, py, aim)
    sa.orbs(c, [(px + 26, py + 10), (px - 16, py + 22), (px + 6, py + 30), (px + 34, py + 26)])
    sa.blit(c, "enemy_spiky", w - 20, int(h * 0.48))
    sa.blit(c, "enemy_chorux", 16, int(h * 0.44))
    sa.blit(c, "enemy_sunux", w - 22, int(h * 0.7))
    sa.snake(c, "snake_head", SNAKE, 20, int(h * 0.72), ang=1.2, wobble=0.5)
    sa.pop(c, px + 24, py - 16, 4)
    sa.blit(c, "vitamin", px - 20, py - 8)
    sa.draw_logo(c, logo_scale(int(w * 0.9), stacked=True), w // 2, 18, stacked=True)
    return sa.export(c, W, H, S, name("MasterImagePortrait", W, H))


def cover_art():
    """No text or logos. The system darkens the left side for text, so the action sits in the right third."""
    W, H = 3840, 2160
    c = sa.canvas(*sa.size_for(W, H, S))           # 320 x 180
    w, h = c.shape[1], c.shape[0]
    field(c, 61, density=0.003, circles=[(40, 150, 56), (120, 30, 26), (w - 20, 20, 30), (w * 0.72, h - 20, 14)])
    fight(c, int(w * 0.7), int(h * 0.56), w - 30, int(h * 0.44),
          orbs=[(w * 0.8, h * 0.4), (w * 0.78, h * 0.68), (w * 0.84, h * 0.6), (w * 0.75, h * 0.78)])
    sa.blit(c, "enemy_spiky", int(w * 0.6), int(h * 0.36))
    sa.blit(c, "enemy_chorux", int(w * 0.58), int(h * 0.74))
    sa.blit(c, "enemy_sunux", int(w * 0.88), int(h * 0.82))
    sa.blit(c, "enemy_zigzag", int(w * 0.5), int(h * 0.56))
    sa.snake(c, "snake_head", SNAKE, int(w * 0.44), int(h * 0.86), ang=0.15, wobble=0.45)
    sa.blit(c, "enemy_big", int(w * 0.2), int(h * 0.3))
    sa.blit(c, "enemy_spiral", int(w * 0.12), int(h * 0.72))
    sa.pop(c, int(w * 0.66), int(h * 0.44), 4)
    sa.blit(c, "vitamin", int(w * 0.66), int(h * 0.3))
    return sa.export(c, W, H, S, name("CoverArt", W, H))


def logo():
    """Transparent, cropped to the logo's own edges, as large as Sony's 2880 x 592 box allows."""
    c = sa.canvas(120, 30, fill=-1)
    x0, y0, x1, y1 = sa.draw_logo(c, 1, 60, 8, outline=1)
    c = c[y0:y1, x0:x1]
    h, w = c.shape
    s = min(2880 // w, 592 // h)
    assert w * s >= 1440 and h * s >= 296, (w, h, s)
    return sa.export(c, w * s, h * s, s, name("Logo", w * s, h * s), transparent=True)


def screenshots():
    """The Steam screenshots (1920 x 1080 gameplay, no captions) doubled with no smoothing to 3840 x 2160."""
    picks = ["01_swarm", "02_boss_inferno", "03_roguelike_build", "04_roguelike_level_up", "10_bullet_hell",
             "11_circle_shot_vs_swarm", "14_windmill_shot_roguelike", "18_boss_world3", "19_boss_world4_tornado_shot",
             "06_gram_stain_palette"]
    out = []
    for i, p in enumerate(picks, 1):
        im = Image.open(os.path.join(ROOT, "Steam/Screenshots", p + ".png")).convert("RGB")
        assert im.size == (1920, 1080), (p, im.size)
        im = im.resize((3840, 2160), Image.NEAREST)
        path = name(f"Screenshot{i:02d}", 3840, 2160, "jpg")
        im.save(path, quality=95, subsampling=0)
        out.append(path)
    return out


def featured_psd():
    """Featured PSD (hero art): three layers, each able to stand alone. The logo layer is a plain pixel layer."""
    W, H = 3840, 2160
    w, h = sa.size_for(W, H, S)
    bg = sa.canvas(w, h)
    field(bg, 71, circles=[(30, h - 20, 44), (w - 30, 14, 30), (w * 0.62, h - 26, 12), (w * 0.2, 30, 20)])
    art = sa.canvas(w, h, fill=-1)
    fight(art, int(w * 0.62), int(h * 0.6), w - 40, int(h * 0.58),
          orbs=[(w * 0.76, h * 0.48), (w * 0.74, h * 0.74), (w * 0.69, h * 0.54), (w * 0.8, h * 0.8)])
    sa.blit(art, "enemy_spiky", int(w * 0.46), int(h * 0.5))
    sa.blit(art, "enemy_chorux", int(w * 0.38), int(h * 0.72))
    sa.blit(art, "enemy_big", int(w * 0.52), int(h * 0.86))
    sa.blit(art, "enemy_sunux", int(w * 0.9), int(h * 0.3))
    sa.snake(art, "snake_head", SNAKE, int(w * 0.22), int(h * 0.78), ang=0.3, wobble=0.45)
    sa.pop(art, int(w * 0.56), int(h * 0.44), 4)
    sa.blit(art, "vitamin", int(w * 0.58), int(h * 0.36))
    lg = sa.canvas(w, h, fill=-1)
    sa.draw_logo(lg, logo_scale(int(w * 0.62)), w // 2, int(h * 0.1), outline=2)
    layers = [("Background", sa.export(bg, W, H, S, None)),
              ("Primary Art", sa.export(art, W, H, S, None, transparent=True)),
              ("Logo", sa.export(lg, W, H, S, None, transparent=True))]
    path = f"{OUT}/{NAME}_FeaturedPSD_{W}x{H}_R.psd"
    psd.write(path, W, H, layers)
    return path


if __name__ == "__main__":
    os.makedirs(OUT, exist_ok=True)
    for f in glob.glob(f"{OUT}/*"): os.remove(f)
    master_square(); master_16x9(); master_4x3(); master_portrait(); cover_art(); logo()
    screenshots(); featured_psd()
    for f in sorted(glob.glob(f"{OUT}/*")):
        im = Image.open(f)
        print(os.path.basename(f), im.size, im.mode, f"{os.path.getsize(f) / 1e6:.1f} MB")
