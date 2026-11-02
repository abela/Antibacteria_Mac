"""Controller / keyboard button glyphs for the menu prompts, 11 px tall, white (the palette tints them).
Xbox: filled discs with the letter cut out. Switch: ring with the letter. PlayStation: ring with the symbol.
Keyboard: key caps. Usage: python3 glyph_sprites.py <out_dir>"""
import sys
from PIL import Image
out = sys.argv[1]
W = (255, 255, 255, 255)

FONT = {  # 3x5
    'A': ['.#.', '#.#', '###', '#.#', '#.#'], 'B': ['##.', '#.#', '##.', '#.#', '##.'],
    'X': ['#.#', '#.#', '.#.', '#.#', '#.#'], 'Y': ['#.#', '#.#', '.#.', '.#.', '.#.'],
    'E': ['###', '#..', '##.', '#..', '###'], 'N': ['##.', '#.#', '#.#', '#.#', '#.#'],
    'T': ['###', '.#.', '.#.', '.#.', '.#.'], 'R': ['##.', '#.#', '##.', '#.#', '#.#'],
    'S': ['.##', '#..', '.#.', '..#', '##.'], 'C': ['.##', '#..', '#..', '#..', '.##'],
}

def canvas(w, h): return [[False] * w for _ in range(h)]

def save(name, g):
    h, w = len(g), len(g[0])
    im = Image.new('RGBA', (w, h), (0, 0, 0, 0))
    for y in range(h):
        for x in range(w):
            if g[y][x]: im.putpixel((x, y), W)
    im.save(f'{out}/{name}.png')

def disc(fill):
    g = canvas(11, 11)
    for y in range(11):
        for x in range(11):
            d = (x - 5) ** 2 + (y - 5) ** 2
            g[y][x] = d <= 5.4 ** 2 if fill else 4.4 ** 2 <= d <= 5.4 ** 2
    return g

def stamp(g, rows, x0, y0, value=True):
    for y, r in enumerate(rows):
        for x, c in enumerate(r):
            if c == '#': g[y0 + y][x0 + x] = value

def letter_disc(ch, fill):
    g = disc(fill)
    stamp(g, FONT[ch], 4, 3, value=not fill)  # filled: cut the letter out
    return g

def ps(symbol):
    g = disc(False)
    shapes = {
        'cross': ['#...#', '.#.#.', '..#..', '.#.#.', '#...#'],
        'circle': ['.###.', '#...#', '#...#', '#...#', '.###.'],
        'square': ['#####', '#...#', '#...#', '#...#', '#####'],
        'triangle': ['..#..', '.#.#.', '.#.#.', '#...#', '#####'],
    }
    stamp(g, shapes[symbol], 3, 3)
    return g

def keycap(text):
    w = 4 * len(text) - 1 + 6
    g = canvas(w, 11)
    for x in range(1, w - 1): g[0][x] = g[10][x] = True
    for y in range(1, 10): g[y][0] = g[y][w - 1] = True
    g[9] = [g[9][0]] + [True] * (w - 2) + [g[9][w - 1]]  # thicker bottom edge reads as a key
    for i, ch in enumerate(text): stamp(g, FONT[ch], 3 + 4 * i, 3)
    return g

def keys_lr():
    left = ['..#', '.##', '###', '.##', '..#']
    right = ['#..', '##.', '###', '##.', '#..']
    g = canvas(23, 11)
    for k, rows in ((0, left), (12, right)):
        for x in range(k + 1, k + 10): g[0][x] = g[10][x] = g[9][x] = True
        for y in range(1, 10): g[y][k] = g[y][k + 10] = True
        stamp(g, rows, k + 4, 3)
    return g

def dpad_lr():
    rows = ['....###....', '....#.#....', '....#.#....', '....#.#....',
            '####...####', '####...####', '####...####',
            '....#.#....', '....#.#....', '....#.#....', '....###....']
    g = canvas(11, 11)
    stamp(g, rows, 0, 0)  # a d-pad with its left and right arms lit
    return g

save('glyph_xbox_a', letter_disc('A', True)); save('glyph_xbox_b', letter_disc('B', True))
save('glyph_switch_a', letter_disc('A', False)); save('glyph_switch_b', letter_disc('B', False))
save('glyph_ps_cross', ps('cross')); save('glyph_ps_circle', ps('circle'))
save('glyph_dpad_lr', dpad_lr())
save('glyph_key_enter', keycap('ENTER')); save('glyph_key_esc', keycap('ESC')); save('glyph_key_lr', keys_lr())
