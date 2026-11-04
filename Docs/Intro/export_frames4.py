"""Export intro v4 as packed palette-index frames for the game: 320x180, 2 bits a pixel
(0 background, 1 dim, 2 foreground, 3 accent), raw-deflated, into export4/intro_frames.bytes. Captions and the skip
hint are left out: the game draws them in its own font, translated. Ends on the white flash, where the game loads
Level 1. Prints each caption's frame range for IntroSettings."""
import os, sys, struct, zlib, importlib.util
import numpy as np
from PIL import Image
HERE = os.path.dirname(os.path.abspath(__file__))
sys.argv = ["x"]
spec = importlib.util.spec_from_file_location("a4", os.path.join(HERE, "animatic4.py"))
A4 = importlib.util.module_from_spec(spec); spec.loader.exec_module(A4)
W, H, FPS = A4.W, A4.H, A4.FPS
pal = {A4.BG: 0, A4.DIM: 1, A4.FG: 2, A4.RED: 3}
frames, start = [], 0
for i, sh in enumerate(A4.SHOTS):
    dur, fn, cap = sh[:3]
    cap_end = sh[3] if len(sh) > 3 else dur
    n = int(dur * FPS)
    for f in range(n):
        img = Image.new("RGB", (W, H), A4.BG)
        fn(img, f / FPS, dur)
        a = np.asarray(img).reshape(-1, 3)
        keys = a[:, 0].astype(np.int64) << 16 | a[:, 1].astype(np.int64) << 8 | a[:, 2]
        idx = np.full(len(keys), 255, np.uint8)
        for c, k in pal.items():
            idx[keys == (c[0] << 16 | c[1] << 8 | c[2])] = k
        assert (idx != 255).all(), f"off-palette pixel in shot {i} frame {f}"
        q = idx.reshape(-1, 4)
        frames.append((q[:, 0] | q[:, 1] << 2 | q[:, 2] << 4 | q[:, 3] << 6).astype(np.uint8).tobytes())
    print(f"caption {i + 1}: frames {start}-{start + min(n, int(cap_end * FPS))}  {cap!r}")
    start += n
raw = b"".join(frames)
co = zlib.compressobj(9, zlib.DEFLATED, -15)
body = co.compress(raw) + co.flush()
header = b"ABIN" + struct.pack("<HHHBH", 1, W, H, FPS, len(frames))
os.makedirs(os.path.join(HERE, "export4"), exist_ok=True)
open(os.path.join(HERE, "export4", "intro_frames.bytes"), "wb").write(header + body)
print("frames", len(frames), "seconds", round(len(frames) / FPS, 2), "raw", len(raw), "deflated", len(body))
