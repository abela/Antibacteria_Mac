"""Vitamin orb (palette indices: '.' clear, '#' foreground, 'R' accent). Frame 1 is the glint flash."""
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
png('vitamin_orb_0', ['..RRR..',
                      '.RRRRR.',
                      'RR#RRRR',
                      'RRRRRRR',
                      'RRRRRRR',
                      '.RRRRR.',
                      '..RRR..'])
png('vitamin_orb_1', ['..RRR..',
                      '.R#RRR.',
                      'R###RRR',
                      'RR#RRRR',
                      'RRRRRRR',
                      '.RRRRR.',
                      '..RRR..'])
