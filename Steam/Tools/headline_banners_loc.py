"""
Localized pixel headline banners for the Steam "About This Game" section, matching Steam/Tools/headline_banners.py:
white pixel text with a red (#FF3B3B) shadow one font pixel down, nearest-neighbour scaled (x8 when it fits),
centred on a 1560 px wide transparent canvas. Steam groups each file with the English one by its language suffix.

Fonts: m6x11 (Latin and Cyrillic, 16 px) with m6x11_extras for Czech carons; Noto Sans CJK Bold as 1-bit
pixels at 16 px, scaled x6, for Japanese, Korean and Chinese (the same way the game's AB_Pixel fallback fonts are made).

Run: python3 headline_banners_loc.py <out dir> <m6x11cyrillic.ttf> <m6x11_extras.ttf>
"""
import os
import sys

from fontTools.ttLib import TTFont
from PIL import Image, ImageDraw, ImageFont

from store_texts import TEXT, HEADLINES

OUT, M6, EXTRAS = sys.argv[1:4]
W = 1560
# Steam only accepts a localized copy at the exact size of its English image (no descender -> 136 px tall).
SIZES = {"swarms": 136, "weapons": 160, "bosses": 160, "perks": 160, "cure": 136}
MAX_SCALE = 8
WHITE, RED = (255, 255, 255, 255), (255, 59, 59, 255)
NOTO = "/usr/share/fonts/opentype/noto/NotoSansCJK-Bold.ttc"
CJK_PX, CJK_SCALE = 16, 6
CJK = {"japanese": 0, "koreana": 1, "schinese": 2, "tchinese": 3}


def fonts_for(lang):
    if lang in CJK:
        return [(ImageFont.truetype(NOTO, CJK_PX, index=CJK[lang]), None)]
    return [(ImageFont.truetype(M6, 16), set(TTFont(M6).getBestCmap())),
            (ImageFont.truetype(EXTRAS, 16), set(TTFont(EXTRAS).getBestCmap()))]


def mask(text, fonts):
    """1-bit text mask at font size (one pixel per game pixel), cropped to the ink."""
    im = Image.new("1", (len(text) * 20 + 20, 40), 0)
    d = ImageDraw.Draw(im)
    d.fontmode = "1"
    x = 4
    for ch in text:
        f = next((f for f, cmap in fonts if cmap is None or ord(ch) in cmap), fonts[0][0])
        d.text((x, 24), ch, font=f, fill=1, anchor="ls")
        x += round(f.getlength(ch))
    return im.crop(im.getbbox())


def banner(m, scale, H):
    w, h = m.size
    small = Image.new("RGBA", (w, h + 1), (0, 0, 0, 0))
    small.paste(Image.new("RGBA", m.size, RED), (0, 1), m)
    small.paste(Image.new("RGBA", m.size, WHITE), (0, 0), m)
    big = small.resize((small.width * scale, small.height * scale), Image.NEAREST)
    out = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    out.paste(big, ((W - big.width) // 2, (H - big.height) // 2))
    return out


def main():
    for lang, t in TEXT.items():
        if lang == "english":
            continue
        fonts = fonts_for(lang)
        masks = [mask(s, fonts) for s in t["h"]]
        # One scale per language so the five banners match each other.
        scale = min(CJK_SCALE if lang in CJK else MAX_SCALE, min((W - 48) // m.width for m in masks),
                    min((SIZES[n] - 8) // (m.height + 1) for n, m in zip(HEADLINES, masks)))
        os.makedirs(f"{OUT}/{lang}", exist_ok=True)
        for name, m in zip(HEADLINES, masks):
            p = f"{OUT}/{lang}/headline_{name}_{lang}.png"
            banner(m, scale, SIZES[name]).save(p)
        print(f"{lang:11s} x{scale}  widest {max(m.width for m in masks) * scale}px")


main()
