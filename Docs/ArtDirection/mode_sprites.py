"""Endless-mode objectives (palette indices: '.' clear, '#' foreground, 'R' accent)."""
import sys
from PIL import Image
out = sys.argv[1]
COL = {'#': (255, 255, 255, 255), 'R': (255, 0, 0, 255)}
def png(name, rows):
    h = len(rows); w = max(len(r) for r in rows)
    im = Image.new('RGBA', (w, h), (0, 0, 0, 0)); px = im.load()
    for y, r in enumerate(rows):
        for x, c in enumerate(r):
            if c in COL: px[x, y] = COL[c]
    im.save(f'{out}/{name}.png')
S = {
 # Capture the Flag / Rush centre: a red pennant on a white pole
 'mode_flag': ['..#..........',
               '..#RRRR......',
               '..#RRRRRRR...',
               '..#RRRRRRRRR.',
               '..#RRRRRRR...',
               '..#RRRR......',
               '..#..........',
               '..#..........',
               '..#..........',
               '..#..........',
               '..#..........',
               '.###.........',
               '#####........'],
 # Peace Walker: a white flag, outlined so it reads against the accent-free arena
 'mode_peace_flag': ['..#..........',
                     '..##########.',
                     '..#########..',
                     '..##########.',
                     '..#########..',
                     '..##########.',
                     '..#..........',
                     '..#..........',
                     '..#..........',
                     '..#..........',
                     '..#..........',
                     '.###.........',
                     '#####........'],
 'mode_dot': ['##', '##'],
 'mode_dot_lit': ['RR', 'RR'],
}
for k, v in S.items(): png(k, v)
