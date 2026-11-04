"""
Localized Steam capsules for Japanese, Korean, Simplified and Traditional Chinese.
Same art as illus.py; the English 'antibacteria' logo stays (it is the brand) and the localized title sits
under it in Noto Sans CJK Black, drawn with the logo's colours and dark extrusion.

Run from Illustrated/Tools: python3 loc_capsules.py [out dir] [lang ...]   -> <out>/<steamlang>/<asset>_<steamlang>.png
"""
import os
import sys

import capsule as cp
import illus as il
from capsule import BG, FG, RED, DARKRED, DARK

OUT = sys.argv[1] if len(sys.argv) > 1 else "../Art/Localized"

# Steam language code -> (title, font family). Title split: first part red (like 'anti'), rest white.
TITLES = {
    "japanese": (("アンチ", "バクテリア"), "Noto Sans CJK JP"),
    "koreana":  (("안티", "박테리아"), "Noto Sans CJK KR"),
    "schinese": (("抗菌", "大作战"), "Noto Sans CJK SC"),
    "tchinese": (("抗菌", "大作戰"), "Noto Sans CJK TC"),
}


def title(parts, family, cx, baseline, size):
    """Localized title, centred: dark outline, extrusion, then face (red first part, white rest)."""
    a, b = parts
    k = size / 60
    def t(fill, dy=0, stroke=None, sw=0):
        st = f' stroke="{stroke}" stroke-width="{sw:.1f}" stroke-linejoin="round"' if stroke else ""
        return (f'<text x="{cx:.1f}" y="{baseline + dy:.1f}" text-anchor="middle" font-family="{family}" '
                f'font-weight="900" font-size="{size:.1f}" letter-spacing="{size*0.06:.1f}"{st}>'
                f'<tspan fill="{fill[0]}">{a}</tspan><tspan fill="{fill[1]}">{b}</tspan></text>')
    return (t((BG, BG), 0, BG, 22 * k) + t((BG, BG), 8 * k, BG, 22 * k) +
            t((DARKRED, DARK), 6 * k) + t((RED, FG)))


def wide(lang, W, H, scale, baseline, seed, sparkle_items, path, size, gap, lift):
    parts, fam = TITLES[lang]
    b = baseline - lift
    letters, dots = il.horizontal(W / 2, b, scale)
    body = cp.dust(seed, int(W * H / 8800), W, H, [(0, b - 120 * scale, W, b + gap + size * 0.4)])
    body += il.sparkles(sparkle_items) + letters + il.dot_characters(dots, scale)
    body += title(parts, fam, W / 2, b + gap + size * 0.88, size)
    il.save(il.svg_doc(W, H, body), path, W, H)


def tall(lang, W, H, path, seed, scale, logo_baseline, r, hero_at, germ_at, sparkle_items, size, lift, shots=2, orbs=2):
    parts, fam = TITLES[lang]
    lb = logo_baseline - lift
    letters, bottom = il.stacked(W / 2, lb, scale)
    body = cp.dust(seed, int(W * H / 7000), W, H, [(0, lb - 160 * scale, W, H)])
    body += il.sparkles(sparkle_items)
    body += il.duel(hero_at[0], hero_at[1] - lift, germ_at[0], germ_at[1] - lift, r, shots, orbs)
    body += letters
    body += title(parts, fam, W / 2, bottom + size * 1.05, size)
    il.save(il.svg_doc(W, H, body), path, W, H)


def logo(lang, path):
    parts, fam = TITLES[lang]
    W, H = 1280, 720
    letters, _ = il.horizontal(W / 2, 380, 0.9, plain_dots=True)
    body = letters + title(parts, fam, W / 2, 380 + 40 + 112, 120)
    il.save(il.svg_doc(W, H, body, transparent=True), path, W, H, transparent=True)


def make(lang):
    d = f"{OUT}/{lang}"
    s = lambda n: f"{d}/{n}_{lang}.png"
    wide(lang, 920, 430, 0.6, 336, 8, [(120, 120, 12, FG), (560, 70, 9, FG), (870, 300, 9, RED), (60, 395, 8, FG)],
         s("store_capsule_header"), size=52, gap=26, lift=42)
    import shutil; shutil.copy(s("store_capsule_header"), s("library_header"))
    wide(lang, 1232, 706, 0.8, 520, 9, [(150, 170, 16, FG), (640, 110, 12, FG), (1160, 470, 12, RED), (90, 640, 10, FG), (1100, 90, 9, FG)],
         s("store_capsule_main"), size=72, gap=36, lift=62)
    wide(lang, 462, 174, 0.25, 112, 10, [(20, 30, 6, FG), (446, 150, 5, RED)],
         s("store_capsule_small"), size=30, gap=10, lift=0)
    tall(lang, 748, 896, s("store_capsule_vertical"), 11, 0.72, 694, 86, (230, 420), (545, 178),
         [(90, 120, 14, FG), (650, 470, 12, RED), (660, 70, 10, FG), (80, 540, 9, FG)], size=64, lift=58)
    tall(lang, 600, 900, s("library_capsule"), 12, 0.58, 716, 74, (180, 440), (445, 190),
         [(70, 130, 12, FG), (530, 480, 10, RED), (520, 80, 9, FG), (70, 560, 8, FG)], size=56, lift=62)
    logo(lang, s("library_logo"))


if __name__ == "__main__":
    for lang in (sys.argv[2:] or TITLES):
        make(lang)
