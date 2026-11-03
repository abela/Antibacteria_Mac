"""Export the intro as packed palette-index frames for the game: 320x180, 2 bits a pixel
(0 background, 1 dim, 2 foreground, 3 accent), raw-deflated. Captions and the skip hint are left out: the game draws
them in its own font, translated. Ends on the white flash, where the game loads Level 1."""
import sys, struct, zlib, importlib.util
import numpy as np
from PIL import Image
sys.argv = ["x"]
spec = importlib.util.spec_from_file_location("a3", "/home/claude/intro/animatic3.py")
A3 = importlib.util.module_from_spec(spec); spec.loader.exec_module(A3)
W, H, FPS = A3.W, A3.H, A3.FPS
pal = {A3.BG: 0, A3.DIM: 1, A3.FG: 2, A3.RED: 3}
frames, cues, start = [], [], 0
for i, sh in enumerate(A3.SHOTS):
    dur, fn, cap = sh[:3]
    n = int(dur * FPS)
    if fn is A3.s_dive:
        n = int(2.4 * FPS)  # up to and including the white flash
    for f in range(n):
        img = Image.new("RGB", (W, H), A3.BG)
        fn(img, f / FPS, dur)
        a = np.asarray(img).reshape(-1, 3)
        keys = a[:, 0].astype(np.int64) << 16 | a[:, 1].astype(np.int64) << 8 | a[:, 2]
        idx = np.full(len(keys), 255, np.uint8)
        for c, k in pal.items():
            idx[keys == (c[0] << 16 | c[1] << 8 | c[2])] = k
        assert (idx != 255).all(), f"off-palette pixel in shot {i} frame {f}"
        q = idx.reshape(-1, 4)
        frames.append((q[:, 0] | q[:, 1] << 2 | q[:, 2] << 4 | q[:, 3] << 6).astype(np.uint8).tobytes())
    cues.append((start, n, cap))
    start += n
raw = b"".join(frames)
co = zlib.compressobj(9, zlib.DEFLATED, -15)
body = co.compress(raw) + co.flush()
header = b"ABIN" + struct.pack("<HHHBH", 1, W, H, FPS, len(frames))
open("/home/claude/intro/export/intro_frames.bytes", "wb").write(header + body)
print("frames", len(frames), "seconds", round(len(frames) / FPS, 2), "raw", len(raw), "deflated", len(body))
for c in cues: print(c[0], c[1], c[0] / FPS, repr(c[2][:20]))
