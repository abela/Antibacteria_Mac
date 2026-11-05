"""Minimal layered PSD writer (8-bit RGB, RLE): enough for Sony's Featured PSD. Needs numpy + Pillow only.

write(path, W, H, layers): layers is a bottom-to-top list of (name, PIL image W x H, RGB or RGBA).
The file also carries the flattened composite, so any viewer shows the full picture.
"""
import struct

import numpy as np
from PIL import Image


def _packbits(row):
    """PackBits one row of bytes (uint8 numpy array)."""
    n = len(row)
    edges = np.flatnonzero(np.diff(row)) + 1
    starts = np.concatenate(([0], edges))
    lens = np.diff(np.concatenate((starts, [n])))
    out = bytearray()
    lit = bytearray()

    def flush():
        i = 0
        while i < len(lit):
            chunk = lit[i:i + 128]
            out.append(len(chunk) - 1)
            out.extend(chunk)
            i += 128
        lit.clear()

    for s, L in zip(starts.tolist(), lens.tolist()):
        v = int(row[s])
        if L == 1:
            lit.append(v)
            continue
        flush()
        while L > 0:
            k = min(L, 128)
            if k == 1:
                lit.append(v)
            else:
                out.append((257 - k) & 0xFF)
                out.append(v)
            L -= k
    flush()
    return bytes(out)


def _rle_channel(ch):
    """(row byte counts, data) for one channel (H x W uint8). Identical rows are encoded once."""
    cache = {}
    counts, data = [], bytearray()
    for r in ch:
        key = r.tobytes()
        enc = cache.get(key)
        if enc is None:
            enc = cache[key] = _packbits(r)
        counts.append(len(enc))
        data.extend(enc)
    return counts, bytes(data)


def _pascal(name, pad=4):
    b = name.encode("ascii", "replace")[:255]
    s = bytes([len(b)]) + b
    while len(s) % pad: s += b"\0"
    return s


def write(path, W, H, layers):
    recs, chdata = bytearray(), bytearray()
    for name, im in layers:
        a = np.array(im.convert("RGBA"))
        assert a.shape[:2] == (H, W), (name, a.shape)
        chans = [(0, a[:, :, 0]), (1, a[:, :, 1]), (2, a[:, :, 2]), (-1, a[:, :, 3])]
        recs += struct.pack(">iiiiH", 0, 0, H, W, len(chans))
        blocks = []
        for cid, ch in chans:
            counts, data = _rle_channel(np.ascontiguousarray(ch))
            blk = struct.pack(">H", 1) + struct.pack(f">{H}H", *counts) + data
            blocks.append(blk)
            recs += struct.pack(">hI", cid, len(blk))
        extra = struct.pack(">I", 0) + struct.pack(">I", 0) + _pascal(name)
        uname = name.encode("utf-16-be")
        luni = struct.pack(">I", len(name)) + uname
        if len(luni) % 2: luni += b"\0"
        extra += b"8BIMluni" + struct.pack(">I", len(luni)) + luni
        recs += b"8BIMnorm" + struct.pack(">BBBB", 255, 0, 0, 0) + struct.pack(">I", len(extra)) + extra
        for blk in blocks: chdata += blk

    layer_info = struct.pack(">h", len(layers)) + bytes(recs) + bytes(chdata)
    if len(layer_info) % 2: layer_info += b"\0"
    layer_info = struct.pack(">I", len(layer_info)) + layer_info
    lmi = layer_info + struct.pack(">I", 0)              # no global layer mask
    lmi = struct.pack(">I", len(lmi)) + lmi

    comp = Image.new("RGBA", (W, H), (0, 0, 0, 255))
    for _, im in layers: comp = Image.alpha_composite(comp, im.convert("RGBA"))
    c = np.array(comp.convert("RGB"))
    counts_all, data_all = [], bytearray()
    for i in range(3):
        counts, data = _rle_channel(np.ascontiguousarray(c[:, :, i]))
        counts_all += counts
        data_all += data

    with open(path, "wb") as f:
        f.write(b"8BPS" + struct.pack(">H", 1) + b"\0" * 6 + struct.pack(">HIIHH", 3, H, W, 8, 3))
        f.write(struct.pack(">I", 0))                    # color mode data
        f.write(struct.pack(">I", 0))                    # image resources
        f.write(lmi)
        f.write(struct.pack(">H", 1) + struct.pack(f">{len(counts_all)}H", *counts_all) + bytes(data_all))
