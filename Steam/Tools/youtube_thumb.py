"""
YouTube thumbnail for the trailer, 1280 x 720, in the pixel set's style: the stacked logo on the left, a giant
boss looming in from the right edge (cropped by the frame), the player cell squaring up to it, and a white
framed "OFFICIAL TRAILER" tag bottom-left. The bottom-right corner stays quiet for YouTube's duration badge.

Run from Steam/Tools: python3 youtube_thumb.py [out.png]   (needs numpy + Pillow)
"""
import math
import os
import sys

import numpy as np

import make_all as ma
import steamart as sa

W, H, S = 1280, 720, 4                       # canvas 320 x 180, 4 screen px per art pixel
OUT = sys.argv[1] if len(sys.argv) > 1 else os.path.join(ma.OUT, "..", "Trailer", "AntiBacteria_YouTube_Thumbnail_1280x720.png")


def big(c, name, x, y, k):
    """A sprite scaled up k times (whole pixels), centred on x, y."""
    sa.blit(c, np.kron(sa.sprite(name), np.ones((k, k), dtype=np.int8)), x, y)


def glow(c, cx, cy, r0, r1, seed):
    """Sparse red dither that thins out with distance: a 3-colour 'glow' behind the boss."""
    rng = np.random.default_rng(seed)
    hh, ww = c.shape
    ys, xs = np.mgrid[0:hh, 0:ww]
    d = np.hypot(xs - cx, ys - cy)
    p = np.clip((r1 - d) / (r1 - r0), 0, 1) ** 2 * 0.09
    m = (rng.random(c.shape) < p) & ((xs + ys) % 2 == 0) & (c == 0)
    c[m] = 2


def tag(c, text, x, y, pad=5, framed=True, line=2):
    """The 'OFFICIAL TRAILER' label. framed: dark box, white frame, white text. Otherwise a white box, dark text."""
    m = sa.text_mask(text, 2)
    h, w = m.shape
    bw, bh = w + 2 * pad + 2, h + 2 * pad
    if framed:
        c[y - 1:y + bh + 1, x - 1:x + bw + 1] = 0          # dark margin so the frame reads over the art
        c[y:y + bh, x:x + bw] = 1                          # frame `line` px thick, so it survives YouTube's small sizes
        c[y + line:y + bh - line, x + line:x + bw - line] = 0
        ink = 1
    else:
        c[y:y + bh, x:x + bw] = 1
        ink = 0
    sub = c[y + pad:y + pad + h, x + pad + 1:x + pad + 1 + w]
    sub[m] = ink


def main():
    c = sa.canvas(W // S, H // S)                 # 320 x 180
    rng = np.random.default_rng(7)
    for (x, y, r) in [(40, 160, 60), (150, 20, 26), (120, 120, 12), (300, 175, 40)]:
        sa.dotted_circle(c, x, y, r, step=3.5, phase=r * 0.1)
    sa.dust(c, 0.004, rng)

    # The boss: twice size, centred past the right edge so the frame crops it.
    bx, by = 262, 86
    glow(c, bx, by, 80, 150, 3)
    big(c, "boss_bigboss", bx, by, 2)

    # Its bullets coming at the player, the player's shots going back.
    px, py = 160, 124
    aim = math.atan2(by - py, bx - px)
    sa.orbs(c, [(186, 128), (176, 104), (198, 146), (170, 140)])
    sa.shots(c, px, py, bx - 92 * math.cos(aim), by - 92 * math.sin(aim), first=10, spacing=9)
    big(c, "player_cell", px, py, 2)
    big(c, "player_nucleus", px + 4 * math.cos(aim), py + 4 * math.sin(aim), 2)

    # A few smaller germs around the left, behind the logo's halo.
    sa.blit(c, "enemy_spiky", 128, 120)
    sa.blit(c, "enemy_chorux", 16, 120)
    sa.blit(c, "enemy_big", 168, 18)
    sa.snake(c, "snake_head", ma.SNAKE, 92, 126, ang=-0.1, wobble=0.45)

    # Logo, stacked and big, left half.
    sa.draw_logo(c, 3, 84, 16, stacked=True, outline=2, gap=5)

    # Tag, bottom-left.
    tag(c, "OFFICIAL TRAILER", 9, 140)

    im = sa.export(c, W, H, S, OUT)
    print(OUT, im.size if hasattr(im, "size") else "")


if __name__ == "__main__":
    main()
