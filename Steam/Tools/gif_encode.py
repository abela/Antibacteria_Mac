"""Turns a GifRecorder capture (raw RGB frames + .txt header) into a store GIF.

usage: python3 gif_encode.py capture.raw out.gif [--scale 2] [--start 0] [--end N] [--sheet sheet.png]
Each game pixel becomes scale x scale pixels (nearest neighbour), so the pixel art stays sharp.
"""
import argparse
from PIL import Image

ap = argparse.ArgumentParser()
ap.add_argument('raw'); ap.add_argument('out')
ap.add_argument('--scale', type=int, default=2)
ap.add_argument('--start', type=int, default=0)
ap.add_argument('--end', type=int, default=-1)
ap.add_argument('--sheet')
a = ap.parse_args()

w, h, n, fps = map(int, open(a.raw + '.txt').read().split())
data = open(a.raw, 'rb').read()
size = w * h * 3
end = n if a.end < 0 else min(n, a.end)
frames = [Image.frombytes('RGB', (w, h), data[i * size:(i + 1) * size]) for i in range(a.start, end)]

# one shared palette from every colour used in the clip (the game only has a handful)
colours = {}
for f in frames[::5]:
    for c in f.getcolors(1 << 16) or []:
        colours[c[1]] = colours.get(c[1], 0) + c[0]
top = sorted(colours, key=colours.get, reverse=True)[:256]
pal_img = Image.new('P', (1, 1))
flat = [v for c in top for v in c] + [0] * (768 - 3 * len(top))
pal_img.putpalette(flat)

out = []
for f in frames:
    q = f.quantize(palette=pal_img, dither=Image.Dither.NONE)
    out.append(q.resize((w * a.scale, h * a.scale), Image.NEAREST))
out[0].save(a.out, save_all=True, append_images=out[1:], duration=round(1000 / fps), loop=0, optimize=False, disposal=1)

if a.sheet:
    picks = [frames[int(i * (len(frames) - 1) / 5)] for i in range(6)]
    sheet = Image.new('RGB', (w * 3, h * 2))
    for i, f in enumerate(picks):
        sheet.paste(f, ((i % 3) * w, (i // 3) * h))
    sheet.resize((w * 6, h * 4), Image.NEAREST).save(a.sheet)
print(f'{len(out)} frames {w * a.scale}x{h * a.scale} {len(top)} colours')
