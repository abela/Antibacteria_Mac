"""
AntiBacteria Steam art, illustrated style: every store, library, event and broadcast image and the icons.
Shapes are drawn as SVG in code (capsule.py has the pieces) and rendered with Chromium (Playwright).

Run: python3 illus.py [output dir]   (needs playwright with Chromium, and Pillow for the icons)
"""
import math
import os
import sys

from PIL import Image

import capsule as cp
from capsule import BG, FG, RED, DARKRED, GREY, DARK, STROKE

OUT = sys.argv[1] if len(sys.argv) > 1 else "out"
REF_R = 58.0       # character radius the drawing functions are tuned for
DOT_RISE = 82.0    # i-dot character centre above the letter's dot position, in letter units


# ------------------------------------------------------------------ lettering

def word(text, red, gap=STROKE + 16):
    """Letter groups and i positions for one word, in letter units (baseline 0)."""
    x = 0.0
    groups, dots = [], []
    for ch in text:
        paths, w, over = cp.letter(ch)
        x += over
        groups.append(f'<g transform="translate({x:.1f},0)">' + "".join(f'<path d="{d}"/>' for d in paths) + "</g>")
        if ch == "i": dots.append(x)
        x += w + gap
    return groups, dots, x - gap


# ------------------------------------------------------------------ characters (scaled from the header's drawings)

def at(svg, x, y, k):
    return f'<g transform="translate({x:.1f},{y:.1f}) scale({k:.3f})">{svg}</g>'


def hero(x, y, r, look):
    return at(cp.hero_simple(0, 0, REF_R, look), x, y, r / REF_R)


def germ(x, y, r, look):
    return at(cp.germ_simple(0, 0, REF_R, look), x, y, r / REF_R)


def duel(hx, hy, gx, gy, r, shots=2, orbs=2, germ_r=None):
    """Hero and bacterium facing each other with a short exchange of fire."""
    out = []
    d = math.hypot(gx - hx, gy - hy)
    ang = math.degrees(math.atan2(gy - hy, gx - hx))
    ux, uy = (gx - hx) / d, (gy - hy) / d
    nx, ny = -uy, ux
    k = r / REF_R
    gr = germ_r or r * 52 / 58
    # Shots leave the hero's halo; bullets leave the bacterium's spikes. Spacing in pixels, so short gaps still read.
    for i in range(shots):
        t = max((r * 1.45 + 6 * k + i * 46 * k) / d, 0.3 + 0.12 * i)
        out.append(at(cp.shot(0, 0, ang, L=20), hx + (gx - hx) * t, hy + (gy - hy) * t, k))
    for i in range(orbs):
        t = min(1 - (gr * 1.55 + 4 * k + i * 40 * k) / d, 0.68 + 0.1 * i)
        off = (-20 if i % 2 == 0 else 18) * k
        out.append(at(cp.orb(0, 0, r=12), hx + (gx - hx) * t + nx * off, hy + (gy - hy) * t + ny * off, k))
    out.append(hero(hx, hy, r, (gx - hx, gy - hy)))
    out.append(germ(gx, gy, gr, (hx - gx, hy - gy)))
    return "".join(out)


def dot_characters(dots, scale):
    """The hero on the first i, the bacterium on the last, plus the shots between them."""
    (x1, y1, _), (x2, y2, _) = dots[0], dots[-1]
    rise = DOT_RISE * scale
    return duel(x1, y1 - rise, x2, y2 - rise, 88 * scale)


def sparkles(items):
    return "".join(cp.sparkle(x, y, s, c) for x, y, s, c in items)


def svg_doc(W, H, body, transparent=False):
    bg = "" if transparent else f'<rect width="{W}" height="{H}" fill="{BG}"/>'
    return f'<svg xmlns="http://www.w3.org/2000/svg" width="{W}" height="{H}" viewBox="0 0 {W} {H}">{bg}{body}</svg>'


def save(svg, path, W, H, transparent=False):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    from playwright.sync_api import sync_playwright
    with sync_playwright() as p:
        b = p.chromium.launch()
        pg = b.new_page(viewport={"width": W, "height": H}, device_scale_factor=1)
        bodybg = "transparent" if transparent else BG
        pg.set_content(f'<html><body style="margin:0;background:{bodybg}">{svg}</body></html>')
        pg.screenshot(path=path, clip={"x": 0, "y": 0, "width": W, "height": H}, omit_background=transparent)
        b.close()
    print(path, W, H)


# ------------------------------------------------------------------ layouts

def horizontal(cx, baseline, scale, plain_dots=False):
    """One-line 'antibacteria': red 'anti', white 'bacteria', measured as one word so the spacing matches the header."""
    groups, ix, width = word("antibacteria", None)
    x0 = cx - width * scale / 2
    tr = f"translate({x0:.1f},{baseline:.1f}) scale({scale})"
    anti, bact = groups[:4], groups[4:]
    def g(gs, color, extra=0, dy=0):
        return (f'<g transform="translate(0,{dy:.1f}) {tr}" fill="none" stroke="{color}" stroke-width="{STROKE + extra}" '
                f'stroke-linecap="round" stroke-linejoin="round">' + "".join(gs) + "</g>")
    k = scale / 0.66
    svg = g(groups, BG, 22) + g(groups, BG, 22, 12 * k) + g(anti, DARKRED, 0, 10 * k) + g(bact, DARK, 0, 10 * k) + g(anti, RED) + g(bact, FG)
    dots = [(x0 + x * scale, baseline - 150 * scale, i == 0) for i, x in enumerate(ix)]
    if plain_dots:
        for px, py, red in dots:
            col, ext = (RED, DARKRED) if red else (FG, DARK)
            r = 21 * scale
            svg += (f'<circle cx="{px:.1f}" cy="{py + 10*k:.1f}" r="{r + 11*scale:.1f}" fill="{BG}"/>'
                    f'<circle cx="{px:.1f}" cy="{py + 10*k:.1f}" r="{r:.1f}" fill="{ext}"/>'
                    f'<circle cx="{px:.1f}" cy="{py:.1f}" r="{r:.1f}" fill="{col}"/>')
    return svg, dots


def stacked(cx, baseline, scale, line_gap=196):
    """'anti' over 'bacteria', plain round i-dots. Returns svg and the bottom of the block."""
    svg = ""
    k = scale / 0.66
    for n, (text, red) in enumerate((("anti", True), ("bacteria", False))):
        groups, ix, width = word(text, red)
        x0 = cx - width * scale / 2
        b = baseline + n * line_gap * scale
        tr = f"translate({x0:.1f},{b:.1f}) scale({scale})"
        col, ext = (RED, DARKRED) if red else (FG, DARK)
        def g(color, extra=0, dy=0):
            return (f'<g transform="translate(0,{dy:.1f}) {tr}" fill="none" stroke="{color}" stroke-width="{STROKE + extra}" '
                    f'stroke-linecap="round" stroke-linejoin="round">' + "".join(groups) + "</g>")
        svg += g(BG, 22) + g(BG, 22, 12 * k) + g(ext, 0, 10 * k) + g(col)
        for x in ix:
            px, py, r = x0 + x * scale, b - 150 * scale, 21 * scale
            svg += (f'<circle cx="{px:.1f}" cy="{py + 10*k:.1f}" r="{r + 11*scale:.1f}" fill="{BG}"/>'
                    f'<circle cx="{px:.1f}" cy="{py + 10*k:.1f}" r="{r:.1f}" fill="{ext}"/>'
                    f'<circle cx="{px:.1f}" cy="{py:.1f}" r="{r:.1f}" fill="{col}"/>')
    return svg, baseline + line_gap * scale + 14 * k


def make_wide(W, H, scale, baseline, seed, sparkle_items, path):
    letters, dots = horizontal(W / 2, baseline, scale)
    body = cp.dust(seed, int(W * H / 8800), W, H, [(0, baseline - 120 * scale, W, baseline + 30 * scale)])
    body += sparkles(sparkle_items) + letters + dot_characters(dots, scale)
    save(svg_doc(W, H, body), path, W, H)


def make_tall(W, H, path, seed, scale, logo_baseline, r, hero_at, germ_at, sparkle_items, shots=2, orbs=2):
    """Big face-off above, stacked logo with plain dots below."""
    letters, _ = stacked(W / 2, logo_baseline, scale)
    body = cp.dust(seed, int(W * H / 7000), W, H, [(0, logo_baseline - 160 * scale, W, H)])
    body += sparkles(sparkle_items)
    body += duel(hero_at[0], hero_at[1], germ_at[0], germ_at[1], r, shots, orbs)
    body += letters
    save(svg_doc(W, H, body), path, W, H)


def faint_ring(x, y, r, w=6):
    return f'<circle cx="{x}" cy="{y}" r="{r}" fill="none" stroke="{DARK}" stroke-width="{w}" stroke-dasharray="0.1 {w*2.6:.0f}" stroke-linecap="round"/>'


def faint_germ(x, y, r):
    out = [f'<circle cx="{x}" cy="{y}" r="{r}" fill="none" stroke="{DARK}" stroke-width="{r*0.12:.1f}"/>',
           f'<circle cx="{x}" cy="{y}" r="{r*0.42:.1f}" fill="#3a1418"/>']
    for k in range(9):
        a = 2 * math.pi * k / 9
        out.append(f'<line x1="{x + r*1.06*math.cos(a):.1f}" y1="{y + r*1.06*math.sin(a):.1f}" x2="{x + r*1.36*math.cos(a):.1f}" '
                   f'y2="{y + r*1.36*math.sin(a):.1f}" stroke="{DARK}" stroke-width="{r*0.12:.1f}" stroke-linecap="round"/>')
    return "".join(out)


# ------------------------------------------------------------------ every image

def all_images():
    S, L, E, B, I = (f"{OUT}/{d}" for d in ("store", "library", "events", "broadcast", "icons"))

    # Header (store + library): the approved draft.
    make_wide(920, 430, 0.66, 336, 8, [(120, 120, 12, FG), (560, 70, 9, FG), (870, 300, 9, RED), (60, 395, 8, FG)],
              f"{S}/store_capsule_header.png")
    import shutil; os.makedirs(L, exist_ok=True); shutil.copy(f"{S}/store_capsule_header.png", f"{L}/library_header.png")

    make_wide(1232, 706, 0.86, 520, 9, [(150, 170, 16, FG), (640, 110, 12, FG), (1160, 470, 12, RED), (90, 640, 10, FG), (1100, 90, 9, FG)],
              f"{S}/store_capsule_main.png")

    # Small capsule: shown as small as 120 px wide, so the logo fills it.
    make_wide(462, 174, 0.315, 150, 10, [(20, 30, 6, FG), (446, 150, 5, RED)], f"{S}/store_capsule_small.png")

    make_tall(748, 896, f"{S}/store_capsule_vertical.png", 11, 0.78, 694, 90, (230, 420), (545, 178),
              [(90, 120, 14, FG), (650, 470, 12, RED), (660, 70, 10, FG), (80, 540, 9, FG)])
    make_tall(600, 900, f"{L}/library_capsule.png", 12, 0.62, 716, 78, (180, 440), (445, 190),
              [(70, 130, 12, FG), (530, 480, 10, RED), (520, 80, 9, FG), (70, 560, 8, FG)])

    # Page background: Steam shows it behind the page, darkened. Faint shapes only, no text.
    W, H = 1438, 810
    body = cp.dust(13, 120, W, H, []).replace(GREY, DARK)
    body += faint_ring(220, 640, 260) + faint_ring(1250, 160, 220) + faint_ring(760, 420, 120)
    body += faint_germ(1180, 600, 70) + faint_germ(260, 200, 46) + faint_germ(700, 120, 30)
    save(svg_doc(W, H, body), f"{S}/store_page_background.png", W, H)

    # Library hero: no text. The face-off, big, a little right of centre; the lower left stays calm for Steam's logo.
    W, H = 3840, 1240
    body = cp.dust(14, 420, W, H, []) + faint_ring(560, 1180, 420, 12) + faint_ring(3500, 120, 360, 12)
    body += sparkles([(1250, 300, 34, FG), (3420, 900, 30, RED), (2300, 160, 26, FG), (3700, 420, 22, FG)])
    body += duel(1900, 690, 2980, 560, 230, shots=3, orbs=3)
    save(svg_doc(W, H, body), f"{L}/library_hero.png", W, H)

    # Library logo: transparent, plain dots (the hero image already shows both characters).
    W, H = 1280, 720
    letters, _ = horizontal(W / 2, 420, 0.9, plain_dots=True)
    save(svg_doc(W, H, letters, transparent=True), f"{L}/library_logo.png", W, H, transparent=True)

    make_wide(800, 450, 0.57, 352, 15, [(80, 110, 11, FG), (490, 60, 8, FG), (760, 330, 8, RED), (50, 410, 7, FG)],
              f"{E}/event_cover.png")
    make_wide(1920, 622, 0.9, 470, 16, [(130, 180, 18, FG), (1790, 160, 16, FG), (1840, 520, 14, RED), (90, 560, 12, FG), (960, 70, 11, FG)],
              f"{E}/event_header.png")

    # Broadcast side panels: face-off on top of each other, stacked logo at the bottom.
    for side, flip in (("left", 1), ("right", -1)):
        cx = 99.5
        make_tall(199, 433, f"{B}/store_broadcast_{side}.png", 17 + (flip > 0), 0.19, 362, 28,
                  (cx - 34 * flip, 232), (cx + 36 * flip, 82), [(30, 30, 5, FG), (170, 300, 5, RED)], shots=1, orbs=1)

    icons(I)


def icon_svg(n, halo=True):
    r = n * (0.27 if halo else 0.40)
    body = hero(n * 0.56, n * 0.56, r, (1, 0.25)) if halo else hero_face(n / 2, n / 2, r)
    return svg_doc(n, n, body)


def hero_face(x, y, r):
    """Hero without halo or syringe, for tiny icons."""
    k = r / REF_R
    nx, ny = 0.34 * REF_R * 0.97, 0.34 * REF_R * 0.24
    inner = (f'<circle cx="0" cy="0" r="{REF_R}" fill="{FG}"/>'
             f'<circle cx="{nx}" cy="{ny}" r="{REF_R*0.42}" fill="{BG}"/>'
             f'<circle cx="{nx + REF_R*0.13}" cy="{ny - REF_R*0.15}" r="{REF_R*0.12}" fill="{FG}"/>'
             f'<path d="M{nx - REF_R*0.45},{ny - REF_R*0.66} Q{nx},{ny - REF_R*0.74} {nx + REF_R*0.4},{ny - REF_R*0.52}" fill="none" stroke="{BG}" stroke-width="11" stroke-linecap="round"/>')
    return at(inner, x, y, k)


def icons(I):
    save(icon_svg(256), f"{I}/shortcut_icon_256.png", 256, 256)
    save(icon_svg(184), f"{I}/tmp_community.png", 184, 184)
    Image.open(f"{I}/tmp_community.png").convert("RGB").save(f"{I}/community_icon_184.jpg", quality=95, subsampling=0)
    os.remove(f"{I}/tmp_community.png")
    small = {}
    for n in (16, 24, 32, 48):
        save(icon_svg(n, halo=False), f"{I}/tmp_{n}.png", n, n)
        small[n] = Image.open(f"{I}/tmp_{n}.png").convert("RGBA")
        os.remove(f"{I}/tmp_{n}.png")
    small[32].save(f"{I}/client_icon_32.png")
    big = Image.open(f"{I}/shortcut_icon_256.png").convert("RGBA")
    big.save(f"{I}/shortcut_icon.ico", sizes=[(16, 16), (24, 24), (32, 32), (48, 48), (64, 64), (128, 128), (256, 256)],
             append_images=[small[16], small[24], small[32], small[48], big.resize((64, 64), Image.LANCZOS), big.resize((128, 128), Image.LANCZOS)])
    small[32].save(f"{I}/client_icon.ico", sizes=[(16, 16), (32, 32)], append_images=[small[16]])


if __name__ == "__main__":
    all_images()
