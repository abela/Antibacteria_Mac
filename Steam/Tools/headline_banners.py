"""Pixel-font headline banners for the Steam description (Steam has no big text in BBCode).

usage: python3 headline_banners.py out_dir
Renders each line in m6x11 at its native 16 px, white with a red drop shadow (like the logo), then scales by 8
with nearest neighbour. 1560 px wide: shown at 780 px on the store page, an exact 2:1, so the pixels stay sharp.
"""
import os, sys
from PIL import Image, ImageDraw, ImageFont

FONT = os.path.join(os.path.dirname(__file__), '../../Unity/AntiBacteria_Unity/Assets/Fonts/m6x11cyrillic.ttf')
LINES = {
    'headline_swarms': 'Blast swarms of bacteria',
    'headline_weapons': 'Unlock new weapons',
    'headline_bosses': 'Take down giant bosses',
    'headline_perks': 'Pick perks. Go deeper.',
    'headline_cure': 'Be the cure',
}
W, SCALE = 195, 8                # native width (195 * 8 = 1560)
TEXT, SHADOW = (242, 242, 242), (255, 59, 59)

font = ImageFont.truetype(FONT, 16)
out = sys.argv[1]
os.makedirs(out, exist_ok=True)
for name, text in LINES.items():
    l, t, r, b = font.getbbox(text)
    h = (b - t) + 2 + 4         # glyphs + shadow + padding
    img = Image.new('RGBA', (W, h), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    d.fontmode = '1'            # no anti-aliasing: real pixels
    x = (W - (r - l)) // 2 - l
    y = 2 - t
    d.text((x, y + 1), text, font=font, fill=SHADOW + (255,))
    d.text((x, y + 2), text, font=font, fill=SHADOW + (255,))
    d.text((x, y), text, font=font, fill=TEXT + (255,))
    big = img.resize((W * SCALE, h * SCALE), Image.NEAREST)
    big.save(os.path.join(out, name + '.png'), optimize=True)
    print(name, big.size, r - l, 'px text')
