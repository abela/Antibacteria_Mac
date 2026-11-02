"""Bonus pickup + icons (palette indices: '.' clear, '#' foreground, 'R' accent)."""
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
 # rounded square frame, the icon sits inside at (3, 3); squares read as "not a bacterium" (those are round)
 'bonus_frame': ['..#########..',
                 '.#.........#.',
                 '#...........#',
                 '#...........#',
                 '#...........#',
                 '#...........#',
                 '#...........#',
                 '#...........#',
                 '#...........#',
                 '#...........#',
                 '#...........#',
                 '.#.........#.',
                 '..#########..'],
 'bonus_water': ['...#...',
                 '..###..',
                 '..###..',
                 '.#####.',
                 '.#.###.',
                 '.#####.',
                 '..###..'],
 'bonus_circle': ['..###..',
                  '.#...#.',
                  '#.....#',
                  '#..R..#',
                  '#.....#',
                  '.#...#.',
                  '..###..'],
 'bonus_windmill': ['..#.#..',
                    '..#.#..',
                    '##...##',
                    '...R...',
                    '##...##',
                    '..#.#..',
                    '..#.#..'],
 'bonus_tornado': ['#######',
                   '.......',
                   '.#####.',
                   '.......',
                   '..###..',
                   '.......',
                   '...R...'],
 'bonus_life': ['.RR.RR.',
                'RRRRRRR',
                'RRRRRRR',
                '.RRRRR.',
                '..RRR..',
                '...R...',
                '.......'],
 'bonus_boom': ['R..#..R',
                '.R.#.R.',
                '..RRR..',
                '##RRR##',
                '..RRR..',
                '.R.#.R.',
                'R..#..R'],
 'bonus_immortality': ['#######',
                       '#.....#',
                       '#..R..#',
                       '#.RRR.#',
                       '.#.R.#.',
                       '..#.#..',
                       '...#...'],
 'bonus_speed': ['#..#...',
                 '.#..#..',
                 '..#..#.',
                 '...#..#',
                 '..#..#.',
                 '.#..#..',
                 '#..#...'],
}
for k, v in S.items(): png(k, v)
print(len(S), 'sprites')
