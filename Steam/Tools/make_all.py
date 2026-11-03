"""Draws every Steam store / library / event / broadcast image and the icons into Steam/Art. Run: python3 make_all.py (needs numpy + Pillow)."""
import math
import os
import shutil

import numpy as np
from PIL import Image

import steamart as sa

HERE = os.path.dirname(os.path.abspath(__file__))
# Run from the repo (Steam/Tools) or a copy with the sources unpacked next to it in src/.
SRC = next(p for p in (os.path.join(HERE, "../../Unity/AntiBacteria_Unity/Assets"), os.path.join(HERE, "src/Unity/AntiBacteria_Unity/Assets"))
           if os.path.isdir(p))
sa.SPRITES = SRC + "/_Project/Art/Sprites"
sa.FONT = sa.ImageFont.truetype(SRC + "/Fonts/m6x11cyrillic.ttf", 16)
OUT = os.path.join(HERE, "..", "Art") if os.path.isdir(os.path.join(HERE, "../../Unity")) else os.path.join(HERE, "out")

SNAKE = ["snake_segment_r4", "snake_segment_r4", "snake_segment_r3", "snake_segment_r3", "snake_segment_r2", "snake_segment_r2"]
ANA = ["ana_segment_r7", "ana_segment_r7", "ana_segment_r6", "ana_segment_r6", "ana_segment_r5", "ana_segment_r5"]


def field(c, seed, density=0.004, circles=()):
    rng = np.random.default_rng(seed)
    for (x, y, r) in circles: sa.dotted_circle(c, x, y, r, step=3.5, phase=r * 0.1)
    sa.dust(c, density, rng)


def fight(c, px, py, bx, by, boss="boss_inferno", shot_end=24, orbs=()):
    """The player firing at a boss, the boss's bullets coming back."""
    aim = math.atan2(by - py, bx - px)
    sa.blit(c, boss, bx, by)
    sa.shots(c, px, py, bx - shot_end * math.cos(aim), by - shot_end * math.sin(aim), first=11, spacing=10)
    sa.player(c, px, py, aim)
    sa.orbs(c, orbs)


# ------------------------------------------------------------------ store page

def store_header(name="store_capsule_header.png"):
    W, H, s = 920, 430, 5
    c = sa.canvas(*sa.size_for(W, H, s))          # 184 x 86
    field(c, 3, circles=[(22, 80, 34), (150, 6, 22), (100, 72, 9)])
    fight(c, 84, 62, 170, 64, orbs=[(132, 50), (126, 75), (117, 60), (108, 80)])
    sa.blit(c, "enemy_spiky", 44, 56)
    sa.blit(c, "enemy_chorux", 20, 41)
    sa.blit(c, "enemy_big", 58, 84)
    sa.snake(c, "snake_head", SNAKE, 22, 66, ang=0.35, wobble=0.45)
    sa.pop(c, 66, 46, 4)
    sa.blit(c, "vitamin", 74, 39)
    sa.draw_logo(c, 2, c.shape[1] // 2, 9)
    return sa.export(c, W, H, s, f"{OUT}/store/{name}")


def store_small():
    """Shown as small as 120 x 45 in lists: almost all logo, at the same chunky pixel size as the big capsules."""
    W, H, s = 462, 174, 5
    c = sa.canvas(*sa.size_for(W, H, s))          # 93 x 35
    field(c, 11, density=0.01, circles=[(4, 34, 12), (90, 0, 10)])
    sa.blit(c, "enemy_spiky", 4, 3)
    sa.blit(c, "enemy_big", 92, 36)
    sa.blit(c, "enemy_bullet", 80, 4)
    sa.draw_logo(c, 1, c.shape[1] // 2, 11, outline=1)
    return sa.export(c, W, H, s, f"{OUT}/store/store_capsule_small.png")


def store_main():
    W, H, s = 1232, 706, 6
    c = sa.canvas(*sa.size_for(W, H, s))          # 206 x 118
    field(c, 5, circles=[(30, 104, 40), (176, 10, 26), (112, 92, 10)])
    fight(c, 92, 84, 180, 84, orbs=[(140, 70), (134, 98), (124, 79), (150, 108), (118, 104)])
    sa.blit(c, "enemy_spiky", 50, 76)
    sa.blit(c, "enemy_chorux", 24, 58)
    sa.blit(c, "enemy_big", 62, 110)
    sa.blit(c, "enemy_sunux", 120, 56)
    sa.snake(c, "snake_head", SNAKE, 22, 90, ang=0.4, wobble=0.45)
    sa.pop(c, 72, 60, 4)
    sa.blit(c, "vitamin", 82, 54)
    sa.blit(c, "vitamin", 102, 108)
    sa.draw_logo(c, 2, c.shape[1] // 2, 14)
    return sa.export(c, W, H, s, f"{OUT}/store/store_capsule_main.png")


def vertical(W, H, s, path, seed):
    c = sa.canvas(*sa.size_for(W, H, s))
    w, h = c.shape[1], c.shape[0]
    field(c, seed, circles=[(10, h * 0.55, 30), (w - 8, h * 0.3, 24), (w * 0.5, h - 30, 12)])
    bx, by = w // 2 + 4, h - 10
    px, py = w // 2 - 8, int(h * 0.56)
    aim = math.atan2(by - py, bx - px)
    sa.blit(c, "boss_inferno", bx, by)
    sa.shots(c, px, py, bx - 22 * math.cos(aim), by - 22 * math.sin(aim), first=11, spacing=9)
    sa.player(c, px, py, aim)
    sa.orbs(c, [(px + 26, py + 10), (px - 16, py + 22), (px + 6, py + 30), (px + 34, py + 26)])
    sa.blit(c, "enemy_spiky", w - 22, int(h * 0.48))
    sa.blit(c, "enemy_chorux", 18, int(h * 0.42))
    sa.blit(c, "enemy_sunux", w - 26, int(h * 0.66))
    sa.snake(c, "snake_head", SNAKE, 24, int(h * 0.7), ang=1.2, wobble=0.5)
    sa.pop(c, px + 24, py - 16, 4)
    sa.blit(c, "vitamin", px - 20, py - 8)
    sa.draw_logo(c, 2, w // 2, 12, stacked=True)
    return sa.export(c, W, H, s, path)


def store_vertical():
    return vertical(748, 896, 5, f"{OUT}/store/store_capsule_vertical.png", 7)


def page_background():
    """Steam darkens this behind the page: an ambient, dimmed microscope field, no text."""
    W, H, s = 1438, 810, 5
    c = sa.canvas(*sa.size_for(W, H, s))          # 288 x 162
    rng = np.random.default_rng(21)
    for (x, y, r) in [(40, 130, 60), (250, 30, 48), (150, 90, 22), (270, 150, 30), (90, 20, 18)]:
        sa.dotted_circle(c, x, y, r, step=4.0)
    sa.dust(c, 0.003, rng)
    for name, x, y in [("enemy_spiky", 30, 40), ("enemy_big", 262, 100), ("enemy_chorux", 200, 140),
                       ("enemy_sunux", 110, 140), ("enemy_spiral", 180, 30)]:
        sa.blit(c, name, x, y)
    sa.snake(c, "snake_head", SNAKE, 60, 80, ang=-0.2, wobble=0.5)
    return sa.export(c, W, H, s, f"{OUT}/store/store_page_background.png", palette="dim")


# ------------------------------------------------------------------ library

def library_capsule():
    return vertical(600, 900, 5, f"{OUT}/library/library_capsule.png", 13)


def library_header():
    os.makedirs(f"{OUT}/library", exist_ok=True)
    shutil.copy(f"{OUT}/store/store_capsule_header.png", f"{OUT}/library/library_header.png")


def library_hero():
    """No logo or text (Steam puts the logo on top). Calm lower-left where the logo usually sits."""
    W, H, s = 3840, 1240, 10
    c = sa.canvas(*sa.size_for(W, H, s))          # 384 x 124
    field(c, 17, circles=[(60, 120, 50), (300, 10, 34), (210, 104, 14), (360, 120, 24)])
    fight(c, 228, 66, 352, 62, orbs=[(300, 46), (292, 80), (280, 58), (312, 92), (270, 88)])
    sa.blit(c, "enemy_spiky", 184, 40)
    sa.blit(c, "enemy_chorux", 160, 76)
    sa.blit(c, "enemy_big", 196, 106)
    sa.blit(c, "enemy_sunux", 262, 22)
    sa.snake(c, "snake_head", SNAKE, 150, 30, ang=0.2, wobble=0.45)
    sa.snake(c, "ana_head", ANA, 112, 54, ang=-0.25, wobble=0.35)
    sa.pop(c, 210, 60, 4)
    sa.blit(c, "vitamin", 240, 44)
    sa.blit(c, "vitamin", 250, 100)
    return sa.export(c, W, H, s, f"{OUT}/library/library_hero.png")


def library_logo():
    """Transparent: the logo alone, as wide as the 1280 x 720 frame allows."""
    W, H, s = 1280, 720, 14
    c = sa.canvas(*sa.size_for(W, H, s), fill=-1)  # 92 x 52
    sa.draw_logo(c, 1, c.shape[1] // 2, c.shape[0] // 2 - 6, outline=1)
    return sa.export(c, W, H, s, f"{OUT}/library/library_logo.png", transparent=True)


# ------------------------------------------------------------------ events and broadcast

def event_cover():
    W, H, s = 800, 450, 4
    c = sa.canvas(*sa.size_for(W, H, s))          # 200 x 113
    field(c, 31, circles=[(20, 104, 34), (170, 4, 22)])
    fight(c, 92, 80, 186, 82, orbs=[(146, 68), (138, 96), (126, 78)])
    sa.blit(c, "enemy_spiky", 46, 74)
    sa.blit(c, "enemy_chorux", 20, 56)
    sa.snake(c, "snake_head", SNAKE, 20, 96, ang=0.3, wobble=0.45)
    sa.blit(c, "vitamin", 70, 56)
    sa.draw_logo(c, 2, c.shape[1] // 2, 14)
    return sa.export(c, W, H, s, f"{OUT}/events/event_cover.png")


def event_header():
    W, H, s = 1920, 622, 6
    c = sa.canvas(*sa.size_for(W, H, s))          # 320 x 104
    field(c, 37, circles=[(30, 96, 40), (280, 8, 28), (160, 92, 10)])
    fight(c, 220, 66, 306, 64, orbs=[(268, 50), (262, 82), (252, 62)])
    sa.blit(c, "enemy_spiky", 34, 40)
    sa.blit(c, "enemy_chorux", 64, 76)
    sa.snake(c, "snake_head", SNAKE, 100, 84, ang=0.0, wobble=0.45)
    sa.blit(c, "enemy_big", 186, 92)
    sa.draw_logo(c, 2, c.shape[1] // 2, 12)
    return sa.export(c, W, H, s, f"{OUT}/events/event_header.png")


def broadcast(side):
    W, H, s = 199, 433, 3
    c = sa.canvas(*sa.size_for(W, H, s))          # 67 x 145
    w, h = c.shape[1], c.shape[0]
    field(c, 41 if side == "left" else 43, circles=[(8, 100, 22), (w - 6, 60, 16)])
    sa.blit(c, "boss_inferno", w // 2 + (10 if side == "left" else -10), h - 2)
    px, py = w // 2, 72
    sa.shots(c, px, py, w // 2, h - 30, first=11, spacing=9)
    sa.player(c, px, py, math.pi / 2)
    sa.orbs(c, [(px - 18, py + 20), (px + 16, py + 28)])
    sa.blit(c, "enemy_spiky", 14 if side == "left" else w - 14, 50)
    sa.draw_logo(c, 1, w // 2, 10, stacked=True, outline=1, gap=2)
    return sa.export(c, W, H, s, f"{OUT}/broadcast/store_broadcast_{side}.png")


# ------------------------------------------------------------------ icons

def icon_canvas(n):
    c = sa.canvas(n, n)
    sa.blit(c, "enemy_spiky", n // 2, n // 2)
    return c


def icons():
    os.makedirs(f"{OUT}/icons", exist_ok=True)
    big = sa.export(icon_canvas(32), 256, 256, 8, f"{OUT}/icons/shortcut_icon_256.png")
    sa.export(icon_canvas(23), 184, 184, 8, f"{OUT}/icons/community_icon_184.jpg")
    sa.export(icon_canvas(32), 32, 32, 1, f"{OUT}/icons/client_icon_32.png")
    sizes = [(16, 16), (24, 24), (32, 32), (48, 48), (64, 64), (128, 128), (256, 256)]
    # Each size drawn on whole pixels: 32 px art scaled by whole numbers where possible.
    frames = {}
    for n, _ in sizes:
        if n % 32 == 0: frames[n] = big.resize((n, n), Image.NEAREST)
        else:
            k = max(1, n // 17)
            frames[n] = sa.export(icon_canvas(math.ceil(n / k)), n, n, k, None)
    frames[256].save(f"{OUT}/icons/shortcut_icon.ico", sizes=sizes, append_images=[frames[n] for n, _ in sizes[:-1]])
    frames[32].save(f"{OUT}/icons/client_icon.ico", sizes=[(16, 16), (32, 32)], append_images=[frames[16]])


if __name__ == "__main__":
    sa.PALETTES["dim"] = [sa.PALETTES["classic"][0]] + [
        tuple(int(a + (b - a) * 0.38) for a, b in zip(sa.PALETTES["classic"][0], col)) for col in sa.PALETTES["classic"][1:]]
    store_header(); store_small(); store_main(); store_vertical(); page_background()
    library_capsule(); library_header(); library_hero(); library_logo()
    event_cover(); event_header(); broadcast("left"); broadcast("right")
    icons()
    for root, _, files in os.walk(OUT):
        for f in sorted(files):
            p = os.path.join(root, f)
            if f.endswith(".ico"): print(p); continue
            print(p, Image.open(p).size)
