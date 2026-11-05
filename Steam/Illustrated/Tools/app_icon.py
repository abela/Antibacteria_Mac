"""AntiBacteria desktop/app icon (Unity Player Settings): hero cell with syringe facing an angry bacterium on a dark petri tile.
Run: python3 app_icon.py [output dir]   (needs playwright with Chromium, and Pillow). Sizes 1024-16; 64 and below are the simple face."""
import math, os, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))   # capsule.py lives next to this file
import capsule as cp
from capsule import BG, FG, RED, GREY, DARK
from PIL import Image
OUT = sys.argv[1] if len(sys.argv) > 1 else "out_icon"
REF_R = 58.0

def at(svg, x, y, k): return f'<g transform="translate({x:.1f},{y:.1f}) scale({k:.4f})">{svg}</g>'

def tile(n, m, rx):
    s = n - 2 * m
    return (f'<defs><radialGradient id="g" cx="0.42" cy="0.4" r="0.75">'
            f'<stop offset="0" stop-color="#20202c"/><stop offset="1" stop-color="#09090d"/></radialGradient>'
            f'<clipPath id="c"><rect x="{m}" y="{m}" width="{s}" height="{s}" rx="{rx}"/></clipPath></defs>'
            f'<rect x="{m}" y="{m}" width="{s}" height="{s}" rx="{rx}" fill="url(#g)"/>')

def border(n, m, rx, w):
    s = n - 2 * m
    return f'<rect x="{m+w/2}" y="{m+w/2}" width="{s-w}" height="{s-w}" rx="{rx-w/2}" fill="none" stroke="#34343f" stroke-width="{w}"/>'

def full(n=1024):
    k = n / 1024
    m, rx = 56 * k, 210 * k
    body = [tile(n, m, rx), '<g clip-path="url(#c)">']
    # faint agar rings and specks
    for r, o in ((430, .10), (330, .07)):
        body.append(f'<circle cx="{455*k}" cy="{585*k}" r="{r*k}" fill="none" stroke="{FG}" stroke-opacity="{o}" stroke-width="{10*k}"/>')
    for i in range(26):
        a = i * 2.39996; d = (180 + (i * 53) % 330) * k
        x, y = 455 * k + d * math.cos(a), 585 * k + d * math.sin(a)
        body.append(f'<circle cx="{x:.1f}" cy="{y:.1f}" r="{(4 + i % 3 * 2)*k}" fill="{GREY}" fill-opacity="0.35"/>')
    hx, hy, hr = 455 * k, 585 * k, 170 * k
    gx, gy, gr = 770 * k, 290 * k, 88 * k
    # exchange of fire
    ux, uy = gx - hx, gy - hy; d = math.hypot(ux, uy); ux, uy = ux / d, uy / d
    ang = math.degrees(math.atan2(uy, ux))
    for t in (0.47,):
        body.append(at(cp.shot(0, 0, ang, L=20), hx + ux * d * t, hy + uy * d * t, 2.6 * k))
    body.append(at(cp.orb(0, 0, r=12), hx + ux * d * 0.64 - uy * 34 * k, hy + uy * d * 0.64 + ux * 34 * k, 2.4 * k))
    body.append(at(cp.germ_simple(0, 0, REF_R, (hx - gx, hy - gy)), gx, gy, gr / REF_R))
    body.append(at(cp.hero_simple(0, 0, REF_R, (ux, uy)), hx, hy, hr / REF_R))
    for x, y, s, c in ((200, 210, 22, FG), (860, 640, 20, RED), (700, 850, 15, FG)):
        body.append(cp.sparkle(x * k, y * k, s * k, c))
    body.append('</g>')
    body.append(border(n, m, rx, 8 * k))
    return svg(n, "".join(body))

def small(n):
    """16-48 px: just the hero face filling a tile."""
    k = n / 1024
    m, rx = 24 * k, 220 * k
    r = 330 * k
    x = y = n / 2
    lx, ly = 1, -0.35
    q = math.hypot(lx, ly); lx, ly = lx / q, ly / q
    nx, ny = x + lx * r * 0.32, y + ly * r * 0.32
    face = (f'<circle cx="{x}" cy="{y}" r="{r}" fill="{FG}"/>'
            f'<circle cx="{nx}" cy="{ny}" r="{r*0.42}" fill="{BG}"/>'
            f'<circle cx="{nx+r*0.13}" cy="{ny-r*0.15}" r="{r*0.12}" fill="{FG}"/>'
            f'<circle cx="{x+r*0.62}" cy="{y+r*0.55}" r="{r*0.14}" fill="{RED}"/>')
    return svg(n, tile(n, m, rx) + face)

def svg(n, body): return f'<svg xmlns="http://www.w3.org/2000/svg" width="{n}" height="{n}" viewBox="0 0 {n} {n}">{body}</svg>'

def render(jobs):
    from playwright.sync_api import sync_playwright
    with sync_playwright() as p:
        b = p.chromium.launch()
        for s, n, path in jobs:
            pg = b.new_page(viewport={"width": n, "height": n})
            pg.set_content(f'<html><body style="margin:0;background:transparent">{s}</body></html>')
            pg.screenshot(path=path, clip={"x": 0, "y": 0, "width": n, "height": n}, omit_background=True)
            pg.close()
        b.close()

os.makedirs(OUT, exist_ok=True)
jobs = [(full(1024), 1024, f"{OUT}/AppIcon_1024.png")]
for n in (16, 24, 32, 48, 64): jobs.append((small(n), n, f"{OUT}/AppIcon_{n}.png"))
render(jobs)
big = Image.open(f"{OUT}/AppIcon_1024.png").convert("RGBA")
for n in (512, 256, 128):
    big.resize((n, n), Image.LANCZOS).save(f"{OUT}/AppIcon_{n}.png")
print("ok")
