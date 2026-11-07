"""
Builds the pixel fallback fonts for the new languages, with only the characters the game's texts use.

- m6x11_extras.ttf: Czech letters m6x11 lacks (č ř š ž ě ň ď ť ů and capitals), drawn from m6x11's own letters,
  plus the medal stars ★ ☆.
- AB_Pixel_JA/KO/SC/TC.ttf: Japanese, Korean, Simplified and Traditional Chinese, from Noto Sans CJK Medium
  (SIL Open Font License) rendered as 1-bit pixels at 12 px.
- AB_Pixel_KA.ttf: Georgian, from DejaVu Sans (Bitstream Vera licence) as 1-bit pixels at 14 px.

Every font is drawn on a 16 px em (1 pixel = 64 units), so at TextMeshPro size 16 one font pixel is one game
pixel, the same as m6x11. Run: python3 build_fonts.py <LocalizationFile.json> <m6x11 ttf> <out dir>
"""
import json
import os
import sys

import numpy as np
from fontTools.fontBuilder import FontBuilder
from fontTools.pens.ttGlyphPen import TTGlyphPen
from PIL import Image, ImageDraw, ImageFont

UNIT = 64           # font units per pixel
EM = 16 * UNIT
NOTO = "/usr/share/fonts/opentype/noto/NotoSansCJK-Medium.ttc"
NOTO_INDEX = {"JA": 0, "KO": 1, "SC": 2, "TC": 3}
DEJAVU = "/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf"
LANG = {"CS": 7, "NL": 9, "PL": 27, "PT": 28, "IT": 21, "JA": 22, "KO": 23, "SC": 40, "TC": 41, "KA": 100}


def render(font, ch, pad=8):
    """1-bit bitmap of one character, the row index of the baseline, and the advance in pixels."""
    asc, desc = font.getmetrics()
    w = int(font.getlength(ch)) + 2 * pad + 8
    h = asc + desc + 2 * pad
    im = Image.new("1", (w, h), 0)
    d = ImageDraw.Draw(im)
    d.fontmode = "1"
    d.text((pad, pad), ch, font=font, fill=1)
    return np.array(im, dtype=bool), pad + asc, pad


def glyph_from_bitmap(bits, baseline_row, origin_col, dy=0):
    """Pixel rows -> TrueType glyph made of one rectangle per horizontal run. dy shifts it up (pixels)."""
    pen = TTGlyphPen(None)
    any_ink = False
    for r in range(bits.shape[0]):
        row = bits[r]
        c = 0
        while c < row.size:
            if not row[c]:
                c += 1
                continue
            s = c
            while c < row.size and row[c]:
                c += 1
            x0, x1 = (s - origin_col) * UNIT, (c - origin_col) * UNIT
            y1 = (baseline_row - r + dy) * UNIT
            y0 = y1 - UNIT
            pen.moveTo((x0, y0)); pen.lineTo((x0, y1)); pen.lineTo((x1, y1)); pen.lineTo((x1, y0)); pen.closePath()
            any_ink = True
    return pen.glyph() if any_ink else TTGlyphPen(None).glyph()


def save_font(path, family, glyphs):
    """glyphs: {char: (glyph, advance_px, lsb_px)}"""
    order = [".notdef", "space"] + ["u%04X" % ord(c) for c in sorted(glyphs)]
    fb = FontBuilder(EM, isTTF=True)
    fb.setupGlyphOrder(order)
    cmap = {32: "space"}
    cmap.update({ord(c): "u%04X" % ord(c) for c in glyphs})
    fb.setupCharacterMap(cmap)
    empty = TTGlyphPen(None).glyph()
    gl = {".notdef": empty, "space": empty}
    metrics = {".notdef": (6 * UNIT, 0), "space": (4 * UNIT, 0)}
    for c, (g, adv, _) in glyphs.items():
        name = "u%04X" % ord(c)
        gl[name] = g
        metrics[name] = (adv * UNIT, 0)
    fb.setupGlyf(gl)
    # left side bearings from the outlines
    for name, g in gl.items():
        g.recalcBounds(fb.font["glyf"])
        metrics[name] = (metrics[name][0], getattr(g, "xMin", 0) or 0)
    fb.setupHorizontalMetrics(metrics)
    fb.setupHorizontalHeader(ascent=12 * UNIT, descent=-4 * UNIT)
    fb.setupNameTable({"familyName": family, "styleName": "Regular"})
    fb.setupOS2(sTypoAscender=12 * UNIT, sTypoDescender=-4 * UNIT, usWinAscent=12 * UNIT, usWinDescent=4 * UNIT)
    fb.setupPost()
    fb.save(path)
    return path


def used_chars(loc, lang_value):
    s = set()
    for w in loc["allText"]:
        for t in w["translations"]:
            if t["language"] == lang_value: s.update(t["word"])
    return s


# ---------------------------------------------------------------- Czech letters drawn from m6x11

def czech(m6x11_path):
    f = ImageFont.truetype(m6x11_path, 16)

    def bm(ch):
        bits, base, org = render(f, ch)
        return bits, base, org

    def accent_rows(with_accent, plain):
        a, _, _ = bm(with_accent)
        b, _, _ = bm(plain)
        extra = a & ~b
        ys, xs = np.nonzero(extra)
        return ys.min(), ys.max()

    out = {}
    lo_top, lo_bot = accent_rows("é", "e")
    up_top, up_bot = accent_rows("É", "E")

    def with_caron(base_ch, rows, centre=None):
        bits, base, org = bm(base_ch)
        ys, xs = np.nonzero(bits)
        cx = (xs.min() + xs.max()) // 2 if centre is None else centre
        r0 = rows[1] - 1                # two rows, ending where the acute ends
        b = bits.copy()
        b[r0, cx - 1] = b[r0, cx + 1] = True
        b[r0 + 1, cx] = True
        return b, base, org

    def with_tick(base_ch):
        """ď ť: the caron becomes a small tick right of the stem."""
        bits, base, org = bm(base_ch)
        ys, xs = np.nonzero(bits)
        b = bits.copy()
        x = xs.max() + 2
        top = ys.min()
        b[top, x] = b[top + 1, x] = True
        return b, base, org, 2

    def with_ring(base_ch, ring_src, ring_base):
        bits, base, org = bm(base_ch)
        a, _, _ = bm(ring_src)
        p, _, _ = bm(ring_base)
        ring = a & ~p
        ys, xs = np.nonzero(ring)
        bx = np.nonzero(bits)[1]
        shift = (bx.min() + bx.max()) // 2 - (xs.min() + xs.max()) // 2
        b = bits.copy()
        for y, x in zip(ys, xs): b[y, x + shift] = True
        return b, base, org

    for lo, up in zip("cenrsz", "CENRSZ"):
        car_lo = {"c": "č", "e": "ě", "n": "ň", "r": "ř", "s": "š", "z": "ž"}[lo]
        car_up = car_lo.upper()
        out[car_lo] = with_caron(lo, (lo_top, lo_bot))
        out[car_up] = with_caron(up, (up_top, up_bot))
    out["Ď"] = with_caron("D", (up_top, up_bot))
    out["Ť"] = with_caron("T", (up_top, up_bot))
    for ch, base_ch in (("ď", "d"), ("ť", "t")):
        b, base, org, extra = with_tick(base_ch)
        out[ch] = (b, base, org, extra)
    out["ů"] = with_ring("u", "å", "a")
    out["Ů"] = with_ring("U", "Å", "A")

    glyphs = {}
    for ch, v in out.items():
        bits, base, org = v[0], v[1], v[2]
        extra = v[3] if len(v) > 3 else 0
        base_ch = {"ď": "d", "ť": "t"}.get(ch)
        src = ch if base_ch is None else base_ch
        # advance: the base letter's advance from m6x11 (+ room for the tick)
        plain = {"č": "c", "ě": "e", "ň": "n", "ř": "r", "š": "s", "ž": "z", "Č": "C", "Ě": "E", "Ň": "N", "Ř": "R",
                 "Š": "S", "Ž": "Z", "Ď": "D", "Ť": "T", "ď": "d", "ť": "t", "ů": "u", "Ů": "U"}[ch]
        adv = int(round(f.getlength(plain))) + extra
        glyphs[ch] = (glyph_from_bitmap(bits, base, org), adv, 0)
    return glyphs, out


# ---------------------------------------------------------------- medal stars (★ earned, ☆ not), 9x9 on the cap height

STAR9 = ["....#....",
         "....#....",
         "...###...",
         "#########",
         ".#######.",
         "..#####..",
         "..##.##..",
         ".##...##.",
         ".#.....#."]


def stars():
    full = np.array([[c == "#" for c in r] for r in STAR9], dtype=bool)
    p = np.pad(full, 1)
    inner = p[1:-1, 1:-1] & p[:-2, 1:-1] & p[2:, 1:-1] & p[1:-1, :-2] & p[1:-1, 2:]
    hollow = full & ~inner
    glyphs = {}
    for ch, bits in (("\u2605", full), ("\u2606", hollow)):
        # rows 0..8 sit one pixel above the baseline, so the star is centred on m6x11's 11 px capitals
        glyphs[ch] = (glyph_from_bitmap(bits, 10, 0), 10, 0)
    return glyphs


# ---------------------------------------------------------------- CJK and Georgian from outline fonts

def pixel_font(font, chars, adv_fn=None, dy=0):
    glyphs = {}
    for ch in sorted(chars):
        if ch == " " or ord(ch) < 0x80: continue
        bits, base, org = render(font, ch)
        adv = adv_fn(ch) if adv_fn else int(round(font.getlength(ch)))
        glyphs[ch] = (glyph_from_bitmap(bits, base, org, dy), adv, 0)
    return glyphs


def main(loc_path, m6x11_path, out_dir):
    os.makedirs(out_dir, exist_ok=True)
    loc = json.load(open(loc_path, encoding="utf-8"))
    from fontTools.ttLib import TTFont
    m6cmap = TTFont(m6x11_path).getBestCmap()

    # Czech + anything else Latin the new European languages use that m6x11 lacks
    eu = set()
    for k in ("CS", "NL", "PL", "PT", "IT"): eu |= used_chars(loc, LANG[k])
    missing = sorted(c for c in eu if ord(c) not in m6cmap)
    cz, preview = czech(m6x11_path)
    still = [c for c in missing if c not in cz]
    cz.update(stars())
    save_font(os.path.join(out_dir, "m6x11_extras.ttf"), "m6x11 extras", cz)
    print("m6x11_extras:", "".join(sorted(cz)), "| still missing:", "".join(still) or "none")

    report = {}
    for key in ("JA", "KO", "SC", "TC"):
        chars = {c for c in used_chars(loc, LANG[key]) if ord(c) not in m6cmap}
        font = ImageFont.truetype(NOTO, 12, index=NOTO_INDEX[key])
        # Noto at 12 px puts the ideograph bottom 1 px below the baseline row; dy keeps it on the baseline like m6x11
        glyphs = pixel_font(font, chars, adv_fn=lambda ch: 12 if ord(ch) >= 0x2E80 else int(round(font.getlength(ch))))
        save_font(os.path.join(out_dir, f"AB_Pixel_{key}.ttf"), f"AntiBacteria Pixel {key}", glyphs)
        report[key] = len(glyphs)
    ka = {c for c in used_chars(loc, LANG["KA"]) if ord(c) not in m6cmap}
    kfont = ImageFont.truetype(DEJAVU, 14)
    save_font(os.path.join(out_dir, "AB_Pixel_KA.ttf"), "AntiBacteria Pixel KA", pixel_font(kfont, ka))
    report["KA"] = len(ka)
    print("glyphs:", report)


if __name__ == "__main__":
    main(*sys.argv[1:4])
