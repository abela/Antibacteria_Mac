"""Shot / hit / wall effect sprites (palette indices: '.' clear, '#' foreground, 'R' accent)."""
import sys
from PIL import Image
out = sys.argv[1]
COL = {'#': (255, 255, 255, 255), 'R': (255, 0, 0, 255), 'k': (0, 0, 0, 255)}
def png(name, rows):
    h = len(rows); w = max(len(r) for r in rows)
    im = Image.new('RGBA', (w, h), (0, 0, 0, 0)); px = im.load()
    for y, r in enumerate(rows):
        for x, c in enumerate(r):
            if c in COL: px[x, y] = COL[c]
    im.save(f'{out}/{name}.png')
S = {
 # player bullet, 1.5x the old 5x2 bar: a pill pointing right (+x)
 'player_shot': ['.######.',
                 '########',
                 '.######.'],
 # first frames of a bullet: a fat round puff
 'fx_shot_birth': ['.###.',
                   '#####',
                   '##R##',
                   '#####',
                   '.###.'],
 # trail behind the bullet (drawn faded)
 'fx_shot_trail': ['######'],
 # muzzle flash, pointing right
 'fx_muzzle_0': ['.##....',
                 '####...',
                 '#RR####',
                 '####...',
                 '.##....'],
 'fx_muzzle_1': ['.#..#..',
                 '..##...',
                 '.#R#..#',
                 '..##...',
                 '.#..#..'],
 # bullet hits an enemy
 'fx_hit_0': ['#.#.#',
              '.###.',
              '##R##',
              '.###.',
              '#.#.#'],
 'fx_hit_1': ['#...#',
              '.....',
              '..#..',
              '.....',
              '#...#'],
 # bullet hits a wall
 'fx_wall_0': ['..#..',
               '.###.',
               '##R##',
               '.###.',
               '..#..'],
 'fx_wall_1': ['.#.#.',
               '#...#',
               '.....',
               '#...#',
               '.#.#.'],
 'fx_wall_2': ['#...#',
               '.....',
               '.....',
               '.....',
               '#...#'],
 # particles
 'fx_px_fg': ['#'],
 'fx_px_accent': ['R'],
 'fx_px_fg2': ['##', '##'],
}
for k, v in S.items(): png(k, v)
print(len(S), 'sprites')
