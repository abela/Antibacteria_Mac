"""Bullet design options for AntiBacteria (Downwell-style, 3 colours)."""
import numpy as np
from PIL import Image, ImageDraw, ImageFont

BG, FG, AC = 0, 1, 2
PAL = np.array([[11, 11, 16], [242, 242, 242], [255, 59, 59]], dtype=np.uint8)
S = 10            # upscale for viewing
CELL = 24         # native px per option cell

PLAYER = {
    "A  Pellet streak": ["......", "...XX.", "..XX..", ".XX...", "XX....", "X....."],
    "B  Chevron": [".....", "..X..", ".XXX.", "XX.XX", "X...X"],
    "C  Ring shot": [".XXX.", "X...X", "X...X", "X...X", ".XXX."],
    "D  Spark (+ trail)": ["...X...", "..XXX..", "...X...", ".......", "...X...", ".......", "...X..."],
}
ENEMY = {
    "A  Hollow orb": [".XXX.", "X...X", "X.X.X", "X...X", ".XXX."],
    "B  Hot core": [".XXX.", "XXXXX", "XXWXX", "XXXXX", ".XXX."],
    "C  Spore": ["..X..", ".XXX.", "XXXXX", ".XXX.", "..X.."],
    "D  Thorn": ["X...X", ".X.X.", "..X..", ".X.X.", "X...X"],
}


def strip(options, colour):
    n = len(options)
    img = np.zeros((CELL, CELL * n), dtype=np.uint8)
    for i, (name, rows) in enumerate(options.items()):
        h, w = len(rows), len(rows[0])
        ox, oy = i * CELL + (CELL - w) // 2, (CELL - h) // 2
        for y, row in enumerate(rows):
            for x, ch in enumerate(row):
                if ch == "X":
                    img[oy + y, ox + x] = colour
                elif ch == "W":
                    img[oy + y, ox + x] = FG
    return Image.fromarray(PAL[img]).resize((CELL * n * S, CELL * S), Image.NEAREST)


try:
    font = ImageFont.load_default(size=20)
except TypeError:
    font = ImageFont.load_default()

rows = [("Player shots (white)", PLAYER, FG), ("Enemy bullets (red)", ENEMY, AC)]
width = CELL * 4 * S
sheet = Image.new("RGB", (width + 40, 2 * (CELL * S + 90) + 20), (255, 255, 255))
d = ImageDraw.Draw(sheet)
y = 20
for title, opts, col in rows:
    d.text((20, y), title, fill=(30, 30, 30), font=font)
    y += 34
    sheet.paste(strip(opts, col), (20, y))
    for i, name in enumerate(opts):
        d.text((20 + i * CELL * S + 12, y + CELL * S + 8), name, fill=(60, 60, 60), font=font)
    y += CELL * S + 56
sheet.save("/home/claude/style/antibacteria_bullet_options.png")
print("ok")
