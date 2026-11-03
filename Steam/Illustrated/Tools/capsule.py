"""
AntiBacteria illustrated key art (hand-drawn direction): SVG built in code, rendered with Chromium.
Lowercase monoline lettering; the hero cell and an angry bacterium are the dots of the two i's.
"""
import math
import sys

BG = "#0b0b10"
FG = "#f2f2f2"
RED = "#ff3b3b"
DARKRED = "#7c1820"
GREY = "#8a8a96"
DARK = "#2a2a33"

H, ASC, R = 96, 150, 48
STROKE = 30


def P(x, y):
    return f"{x:.1f},{y:.1f}"


def arc_pt(cx, cy, r, deg):
    a = math.radians(deg)
    return cx + r * math.cos(a), cy + r * math.sin(a)


# Each letter: (paths, width, left overhang, i-dot position or None). Origin: left stem centre on the baseline.
def letter(ch):
    if ch == "a":
        return [f"M{R*2},{-H} V0", f"M{R*2},{-R} A{R},{R} 0 1 0 0,{-R} A{R},{R} 0 1 0 {R*2},{-R}"], R * 2, 0
    if ch == "b":
        return [f"M0,{-ASC} V0", f"M0,{-R} A{R},{R} 0 1 0 {R*2},{-R} A{R},{R} 0 1 0 0,{-R}"], R * 2, 0
    if ch == "n":
        return [f"M0,0 V-52 C0,-82 18,-96 40,-96 C62,-96 80,-82 80,-52 V0"], 80, 0
    if ch == "r":
        return [f"M0,0 V-52 C0,-82 18,-96 46,-96"], 46, 0
    if ch == "t":
        return [f"M16,-136 V-26 C16,-8 26,0 46,0", "M-4,-96 H44"], 46, 4
    if ch == "i":
        return ["M0,-96 V0"], 0, 0
    if ch == "c":
        x1, y1 = arc_pt(R, -R, R, -42)
        x2, y2 = arc_pt(R, -R, R, 42)
        return [f"M{P(x1, y1)} A{R},{R} 0 1 0 {P(x2, y2)}"], x1, 0
    if ch == "e":
        x2, y2 = arc_pt(R, -R, R, 42)
        return [f"M0,{-R} H{R*2} A{R},{R} 0 1 0 {P(x2, y2)}"], R * 2, 0
    raise KeyError(ch)


def word_paths(text, gap):
    """All letter paths placed along the baseline; returns (anti_paths, bact_paths, i_dots, total width)."""
    x = 0.0
    anti, bact, dots = [], [], []
    for k, ch in enumerate(text):
        paths, w, over = letter(ch)
        x += over
        g = f'<g transform="translate({x:.1f},0)">' + "".join(f'<path d="{d}"/>' for d in paths) + "</g>"
        (anti if k < 4 else bact).append(g)
        if ch == "i": dots.append((x, -150))
        x += w + gap
    return anti, bact, dots, x - gap


def lettering(cx, baseline, scale):
    anti, bact, dots, width = word_paths("antibacteria", STROKE + 16)
    x0 = cx - width * scale / 2
    tr = f'translate({x0:.1f},{baseline}) scale({scale})'
    def layer(paths, color, sw, dy=0):
        return (f'<g transform="translate(0,{dy}) {tr}" fill="none" stroke="{color}" stroke-width="{sw}" '
                f'stroke-linecap="round" stroke-linejoin="round">' + "".join(paths) + "</g>")
    svg = layer(anti + bact, BG, STROKE + 22, 0) + layer(anti + bact, BG, STROKE + 22, 12)   # clear space around
    svg += layer(anti, DARKRED, STROKE, 10) + layer(bact, DARK, STROKE, 10)                   # extrusion
    svg += layer(anti, RED, STROKE) + layer(bact, FG, STROKE)
    dots = [(x0 + x * scale, baseline + y * scale) for x, y in dots]
    return svg, dots, scale


# ------------------------------------------------------------------ characters and doodles

def sparkle(x, y, s=10, color=FG):
    return (f'<path transform="translate({x},{y})" fill="{color}" d="M0,{-s} C{s*0.15},{-s*0.15} {s*0.15},{-s*0.15} {s},0 '
            f'C{s*0.15},{s*0.15} {s*0.15},{s*0.15} 0,{s} C{-s*0.15},{s*0.15} {-s*0.15},{s*0.15} {-s},0 '
            f'C{-s*0.15},{-s*0.15} {-s*0.15},{-s*0.15} 0,{-s} Z"/>')


def syringe(x, y, ang, L=120, w=26, fill=RED, outline=FG):
    """Needle tip at the far end (+x), plunger at the near end, rotated by ang degrees around (x, y) (barrel centre)."""
    h = w / 2
    return (f'<g transform="translate({x},{y}) rotate({ang})" stroke="{outline}" stroke-width="6" stroke-linecap="round" stroke-linejoin="round">'
            f'<line x1="{L*0.5}" y1="0" x2="{L*0.5+38}" y2="0" stroke-width="5"/>'               # needle
            f'<rect x="{L*0.5-2}" y="-6" width="12" height="12" rx="3" fill="{BG}"/>'            # hub
            f'<rect x="{-L*0.5}" y="{-h}" width="{L}" height="{w}" rx="7" fill="{BG}"/>'
            f'<rect x="{-L*0.5+L*0.32}" y="{-h+6}" width="{L*0.62}" height="{w-12}" rx="3" fill="{fill}" stroke="none"/>'
            + "".join(f'<line x1="{-L*0.5+L*(0.4+0.13*i)}" y1="{-h}" x2="{-L*0.5+L*(0.4+0.13*i)}" y2="{-h+8}" stroke-width="4"/>' for i in range(4))
            + f'<line x1="{-L*0.5-4}" y1="{-h-12}" x2="{-L*0.5-4}" y2="{h+12}" stroke-width="7"/>'   # finger flange
            f'<line x1="{-L*0.5-4}" y1="0" x2="{-L*0.5-34}" y2="0" stroke-width="7"/>'              # plunger
            f'<line x1="{-L*0.5-36}" y1="-11" x2="{-L*0.5-36}" y2="11" stroke-width="8"/>'
            '</g>')


def hero(x, y, r=46, look=(1, 0.15)):
    """The player: a round white cell, its black nucleus looking at the enemy, dotted halo, syringe on its back."""
    lx, ly = look
    n = math.hypot(lx, ly); lx, ly = lx / n, ly / n
    nx, ny = x + lx * r * 0.36, y + ly * r * 0.36
    return (syringe(x - r * 0.75, y - r * 0.55, -150, L=100, w=24)
            + f'<circle cx="{x}" cy="{y}" r="{r+16}" fill="none" stroke="{FG}" stroke-width="5" stroke-dasharray="0.1 13" stroke-linecap="round"/>'
            + f'<circle cx="{x}" cy="{y}" r="{r+5}" fill="{BG}"/>'
            + f'<circle cx="{x}" cy="{y}" r="{r}" fill="{FG}"/>'
            + f'<path d="M{x-r*0.62},{y+r*0.52} A{r},{r} 0 0 0 {x+r*0.55},{y+r*0.6}" fill="none" stroke="#c9c9d2" stroke-width="7" stroke-linecap="round"/>'
            + f'<circle cx="{nx}" cy="{ny}" r="{r*0.38}" fill="{BG}"/>'
            + f'<circle cx="{nx+r*0.12}" cy="{ny-r*0.14}" r="{r*0.11}" fill="{FG}"/>'
            # determined brow
            + f'<path d="M{nx-r*0.42},{ny-r*0.58} Q{nx},{ny-r*0.66} {nx+r*0.36},{ny-r*0.46}" fill="none" stroke="{BG}" stroke-width="7" stroke-linecap="round"/>')


def germ(x, y, r=42, spikes=10, angry=True, look=(-1, 0.1), core=RED, wobble=0.0):
    """A bacterium: dark body, white outline, spikes, red core that is also its eye."""
    out = []
    for k in range(spikes):
        a = 2 * math.pi * k / spikes + wobble
        x1, y1 = x + (r + 2) * math.cos(a), y + (r + 2) * math.sin(a)
        x2, y2 = x + (r + 16) * math.cos(a), y + (r + 16) * math.sin(a)
        out.append(f'<line x1="{x1:.1f}" y1="{y1:.1f}" x2="{x2:.1f}" y2="{y2:.1f}" stroke="{FG}" stroke-width="6" stroke-linecap="round"/>')
        out.append(f'<circle cx="{x2:.1f}" cy="{y2:.1f}" r="5" fill="{FG}"/>')
    lx, ly = look
    n = math.hypot(lx, ly) or 1; lx, ly = lx / n, ly / n
    cx, cy = x + lx * r * 0.18, y + ly * r * 0.18
    out.append(f'<circle cx="{x}" cy="{y}" r="{r}" fill="{DARK}" stroke="{FG}" stroke-width="7"/>')
    # dotted texture like the game's dithered bodies
    for k in range(14):
        a = k * 2.4; d = r * (0.45 + 0.4 * ((k * 37) % 10) / 10)
        out.append(f'<circle cx="{x + d*math.cos(a):.1f}" cy="{y + d*math.sin(a):.1f}" r="2.6" fill="{GREY}"/>')
    out.append(f'<circle cx="{cx}" cy="{cy}" r="{r*0.42}" fill="{core}" stroke="{BG}" stroke-width="4"/>')
    out.append(f'<circle cx="{cx + lx*r*0.14}" cy="{cy + ly*r*0.14 - r*0.06}" r="{r*0.13}" fill="{BG}"/>')
    out.append(f'<circle cx="{cx - r*0.12}" cy="{cy - r*0.17}" r="{r*0.07}" fill="{FG}"/>')
    if angry:
        out.append(f'<path d="M{cx - r*0.55},{cy - r*0.62} L{cx + r*0.35},{cy - r*0.36}" stroke="{FG}" stroke-width="8" stroke-linecap="round"/>')
    return "".join(out)


def bacillus(x, y, ang, L=70, w=26, eyes=True):
    """Rod bacterium with wavy flagella."""
    h = w / 2
    fl = "".join(f'<path d="M{-L/2},{o} c-12,-10 -22,10 -34,0 s-22,10 -34,0" fill="none" stroke="{GREY}" stroke-width="4" stroke-linecap="round"/>' for o in (-6, 6))
    e = (f'<circle cx="{L*0.22}" cy="-3" r="5" fill="{RED}"/><circle cx="{L*0.36}" cy="-3" r="5" fill="{RED}"/>') if eyes else ""
    return (f'<g transform="translate({x},{y}) rotate({ang})">{fl}'
            f'<rect x="{-L/2}" y="{-h}" width="{L}" height="{w}" rx="{h}" fill="{FG}"/>'
            f'<path d="M{-L/2+10},{h-6} H{L/2-12}" stroke="#c9c9d2" stroke-width="5" stroke-linecap="round"/>{e}</g>')


def phage(x, y, ang, s=1.0):
    """Bacteriophage: hexagon head, collar, tail, legs. The 'bat' of this world."""
    hx = "".join(f'{P(22*math.cos(math.radians(60*k+30)), -60 + 22*math.sin(math.radians(60*k+30)))} ' for k in range(6))
    legs = "".join(f'<path d="M0,0 L{sx*20},14 L{sx*30},34" fill="none" stroke="{FG}" stroke-width="5" stroke-linecap="round" stroke-linejoin="round"/>' for sx in (-1, -0.4, 0.4, 1))
    return (f'<g transform="translate({x},{y}) rotate({ang}) scale({s})">'
            f'<line x1="0" y1="-38" x2="0" y2="0" stroke="{FG}" stroke-width="9" stroke-linecap="round"/>'
            f'<line x1="-9" y1="-30" x2="9" y2="-30" stroke="{FG}" stroke-width="5"/><line x1="-9" y1="-20" x2="9" y2="-20" stroke="{FG}" stroke-width="5"/>'
            f'{legs}<line x1="-12" y1="0" x2="12" y2="0" stroke="{FG}" stroke-width="7" stroke-linecap="round"/>'
            f'<polygon points="{hx}" fill="{BG}" stroke="{FG}" stroke-width="6" stroke-linejoin="round"/>'
            f'<circle cx="-7" cy="-62" r="4" fill="{RED}"/><circle cx="7" cy="-62" r="4" fill="{RED}"/></g>')


def pill(x, y, ang, L=46, w=20):
    h = w / 2
    return (f'<g transform="translate({x},{y}) rotate({ang})">'
            f'<rect x="{-L/2}" y="{-h}" width="{L}" height="{w}" rx="{h}" fill="{FG}"/>'
            f'<path d="M0,{-h} H{L/2-h} A{h},{h} 0 0 1 {L/2-h},{h} H0 Z" fill="{RED}"/>'
            f'<path d="M{-L/2+8},{-h+5} H{-6}" stroke="{BG}" stroke-width="3" stroke-linecap="round" opacity="0.35"/></g>')


def orb(x, y, r=11):
    """Enemy bullet: red ring with a dot."""
    return f'<circle cx="{x}" cy="{y}" r="{r}" fill="none" stroke="{RED}" stroke-width="5"/><circle cx="{x}" cy="{y}" r="3.5" fill="{RED}"/>'


def shot(x, y, ang, L=16):
    """Player shot: a white pellet with a short dotted trail."""
    return (f'<g transform="translate({x},{y}) rotate({ang})">'
            f'<circle cx="{-L-6}" cy="0" r="2.5" fill="{GREY}"/><circle cx="{-L-14}" cy="0" r="2" fill="{GREY}"/>'
            f'<line x1="{-L/2}" y1="0" x2="{L/2}" y2="0" stroke="{FG}" stroke-width="9" stroke-linecap="round"/></g>')


def ring_chain(pts, r0=14, r1=6):
    out = []
    n = len(pts)
    for k, (x, y) in enumerate(reversed(pts)):
        r = r1 + (r0 - r1) * k / max(1, n - 1)
        out.append(f'<circle cx="{x}" cy="{y}" r="{r:.1f}" fill="{BG}" stroke="{FG}" stroke-width="5"/>')
    hx, hy = pts[0]
    out.append(f'<circle cx="{hx}" cy="{hy}" r="{r0+5}" fill="{FG}"/>'
               f'<circle cx="{hx+7}" cy="{hy-5}" r="4" fill="{RED}"/><circle cx="{hx+7}" cy="{hy+6}" r="4" fill="{RED}"/>')
    return "".join(out)


def petri(x, y, w=90, h=26):
    return (f'<ellipse cx="{x}" cy="{y}" rx="{w/2}" ry="{h/2}" fill="{BG}" stroke="{FG}" stroke-width="5"/>'
            f'<ellipse cx="{x}" cy="{y-5}" rx="{w/2-8}" ry="{h/2-6}" fill="none" stroke="{GREY}" stroke-width="3"/>'
            + "".join(f'<circle cx="{x + dx}" cy="{y - 5 + dy}" r="3" fill="{RED}"/>' for dx, dy in ((-14, 0), (6, -3), (18, 2), (-2, 3))))


def drop(x, y, s=1.0, color=FG):
    return f'<path transform="translate({x},{y}) scale({s})" d="M0,-14 C6,-4 10,2 10,6 A10,10 0 0 1 -10,6 C-10,2 -6,-4 0,-14 Z" fill="{color}"/>'


def dust(seed, n, W, Hh, avoid):
    import random
    rnd = random.Random(seed)
    out = []
    for _ in range(n):
        x, y = rnd.uniform(0, W), rnd.uniform(0, Hh)
        if any(x0 < x < x1 and y0 < y < y1 for x0, y0, x1, y1 in avoid): continue
        out.append(f'<circle cx="{x:.0f}" cy="{y:.0f}" r="{rnd.choice((1.5, 2, 2.5))}" fill="{GREY}"/>')
    return "".join(out)


def header(W=920, Hh=430):
    word, dots, sc = lettering(W / 2, 318, 0.66)
    (ix1, iy1), (ix2, iy2) = dots
    body = []
    body.append(dust(4, 140, W, Hh, [(20, 200, 900, 345)]))
    # big faint microscope-field rings
    body.append(f'<circle cx="80" cy="420" r="120" fill="none" stroke="{DARK}" stroke-width="5" stroke-dasharray="1 14" stroke-linecap="round"/>')
    body.append(f'<circle cx="860" cy="30" r="95" fill="none" stroke="{DARK}" stroke-width="5" stroke-dasharray="1 14" stroke-linecap="round"/>')
    # the fight above the letters
    hx, hy = ix1, iy1 - 30
    gx, gy = ix2, iy2 - 28
    for k in range(4):
        t = 0.2 + 0.14 * k
        body.append(shot(hx + (gx - hx) * t, hy + (gy - hy) * t, math.degrees(math.atan2(gy - hy, gx - hx))))
    for k, (t, off) in enumerate(((0.6, -36), (0.7, 8), (0.82, -30), (0.5, 14))):
        body.append(orb(hx + (gx - hx) * t, hy + (gy - hy) * t + off))
    body.append(phage(560, 98, 16, 0.95))
    body.append(bacillus(120, 70, -18))
    body.append(bacillus(700, 52, 12, L=56, w=22))
    body.append(pill(470, 60, -25))
    body.append(drop(40, 150, 1.0, RED))
    body.append(drop(240, 40, 0.8))
    for x, y, s, c in ((180, 140, 9, FG), (395, 120, 12, FG), (645, 165, 8, RED), (895, 120, 10, FG), (35, 40, 8, FG), (520, 175, 7, FG)):
        body.append(sparkle(x, y, s, c))
    # under the letters
    body.append(ring_chain([(612, 388), (590, 396), (570, 400), (551, 398), (534, 391), (519, 381)], r0=13, r1=6))
    body.append(petri(108, 390))
    body.append(syringe(330, 392, 8, L=96, w=22))
    body.append(pill(452, 398, 15, L=40, w=18))
    body.append(pill(720, 392, -30, L=40, w=18))
    body.append(germ(820, 392, 20, spikes=8, angry=False, look=(-1, -0.3)))
    body.append(germ(230, 395, 16, spikes=7, angry=False, look=(1, -0.2)))
    body.append(sparkle(890, 400, 8, FG))
    body.append(sparkle(660, 410, 6, RED))
    chars = hero(hx, hy, 44, look=(gx - hx, gy - hy)) + germ(gx, gy, 40, look=(hx - gx, hy - gy), wobble=0.2)
    return (f'<svg xmlns="http://www.w3.org/2000/svg" width="{W}" height="{Hh}" viewBox="0 0 {W} {Hh}">'
            f'<rect width="{W}" height="{Hh}" fill="{BG}"/>' + "".join(body) + word + chars + "</svg>")


def render(svg, path, W, Hh):
    from playwright.sync_api import sync_playwright
    with sync_playwright() as p:
        b = p.chromium.launch()
        pg = b.new_page(viewport={"width": W, "height": Hh}, device_scale_factor=1)
        pg.set_content(f'<html><body style="margin:0;background:{BG}">{svg}</body></html>')
        pg.screenshot(path=path, clip={"x": 0, "y": 0, "width": W, "height": Hh})
        b.close()


if __name__ == "__main__" and (len(sys.argv) < 2 or sys.argv[1] != "simple"):
    svg = header()
    open("header.svg", "w").write(svg)
    render(svg, sys.argv[1] if len(sys.argv) > 1 else "header.png", 920, 430)


def header_simple(W=920, Hh=430):
    """Fewer details: the logo, the hero and the bacterium as the i-dots, a short exchange of fire, a few sparkles."""
    word, dots, sc = lettering(W / 2, 336, 0.66)
    (ix1, iy1), (ix2, iy2) = dots
    hx, hy = ix1, iy1 - 54
    gx, gy = ix2, iy2 - 52
    body = [dust(8, 45, W, Hh, [(0, 230, W, 380)])]
    ang = math.degrees(math.atan2(gy - hy, gx - hx))
    for t in (0.3, 0.42):
        body.append(shot(hx + (gx - hx) * t, hy + (gy - hy) * t, ang, L=20))
    for t, off in ((0.68, -22), (0.78, 16)):
        body.append(orb(hx + (gx - hx) * t, hy + (gy - hy) * t + off, r=12))
    for x, y, s, c in ((120, 120, 12, FG), (560, 70, 9, FG), (870, 300, 9, RED), (60, 395, 8, FG)):
        body.append(sparkle(x, y, s, c))
    chars = hero_simple(hx, hy, 58, look=(gx - hx, gy - hy)) + germ_simple(gx, gy, 52, look=(hx - gx, hy - gy))
    return (f'<svg xmlns="http://www.w3.org/2000/svg" width="{W}" height="{Hh}" viewBox="0 0 {W} {Hh}">'
            f'<rect width="{W}" height="{Hh}" fill="{BG}"/>' + "".join(body) + word + chars + "</svg>")


def hero_simple(x, y, r, look):
    lx, ly = look
    n = math.hypot(lx, ly); lx, ly = lx / n, ly / n
    nx, ny = x + lx * r * 0.34, y + ly * r * 0.34
    return (syringe(x - r * 0.78, y - r * 0.6, -150, L=r * 2.1, w=r * 0.5)
            + f'<circle cx="{x}" cy="{y}" r="{r+16}" fill="none" stroke="{FG}" stroke-width="6" stroke-dasharray="0.1 15" stroke-linecap="round"/>'
            + f'<circle cx="{x}" cy="{y}" r="{r+6}" fill="{BG}"/>'
            + f'<circle cx="{x}" cy="{y}" r="{r}" fill="{FG}"/>'
            + f'<circle cx="{nx}" cy="{ny}" r="{r*0.38}" fill="{BG}"/>'
            + f'<circle cx="{nx+r*0.12}" cy="{ny-r*0.14}" r="{r*0.11}" fill="{FG}"/>'
            + f'<path d="M{nx-r*0.42},{ny-r*0.6} Q{nx},{ny-r*0.68} {nx+r*0.36},{ny-r*0.48}" fill="none" stroke="{BG}" stroke-width="8" stroke-linecap="round"/>')


def germ_simple(x, y, r, look, spikes=9):
    lx, ly = look
    n = math.hypot(lx, ly); lx, ly = lx / n, ly / n
    out = []
    for k in range(spikes):
        a = 2 * math.pi * k / spikes + 0.2
        x1, y1 = x + (r + 2) * math.cos(a), y + (r + 2) * math.sin(a)
        x2, y2 = x + (r + 18) * math.cos(a), y + (r + 18) * math.sin(a)
        out.append(f'<line x1="{x1:.1f}" y1="{y1:.1f}" x2="{x2:.1f}" y2="{y2:.1f}" stroke="{FG}" stroke-width="7" stroke-linecap="round"/>')
        out.append(f'<circle cx="{x2:.1f}" cy="{y2:.1f}" r="6" fill="{FG}"/>')
    cx, cy = x + lx * r * 0.16, y + ly * r * 0.16
    out.append(f'<circle cx="{x}" cy="{y}" r="{r}" fill="{DARK}" stroke="{FG}" stroke-width="8"/>')
    out.append(f'<circle cx="{cx}" cy="{cy}" r="{r*0.44}" fill="{RED}"/>')
    out.append(f'<circle cx="{cx + lx*r*0.14}" cy="{cy + ly*r*0.14}" r="{r*0.15}" fill="{BG}"/>')
    out.append(f'<path d="M{cx - r*0.62},{cy - r*0.66} L{cx + r*0.3},{cy - r*0.38}" stroke="{FG}" stroke-width="9" stroke-linecap="round"/>')
    return "".join(out)


if __name__ == "__main__" and len(sys.argv) > 1 and sys.argv[1] == "simple":
    svg = header_simple()
    open("header_simple.svg", "w").write(svg)
    render(svg, "header_simple.png", 920, 430)
