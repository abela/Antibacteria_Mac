# PlayStation Store (Content Pipeline)

Concept **10021487**, product group **10093882**, product **HB2201-PPSA40891_00-0389464538186540** (NP Title ID PPSA40891_00).

## Store text
`Store/store_texts.json`: sub-header and marketing copy in the 13 store languages Sony offers that the game supports
(en, fr, de, it, es, pt, nl, pl, ru, ja, ko, zh = Simplified, ch = Traditional). Mode, shot and perk names match the
game's own localization. Japanese, Korean and Chinese also have a search/sort name (`sort`). Czech and Georgian are not
PlayStation Store metadata languages; those countries see English.

Entered in Content Pipeline → product group → Shared Metadata & Assets (one block per language).

## Art
`Store/Art/`, drawn by `python3 PlayStation/Tools/make_ps.py` (numpy + Pillow; reuses `Steam/Tools/steamart.py`).
Every image is the game's 320 × 180 pixel grid ×12, so 3840 × 2160 is pixel-perfect.

| File | Sony asset | Notes |
|---|---|---|
| `*_MasterImage_2160x2160_R.png` | Master Image (mandatory) | Stacked logo in the safe zone |
| `*_Logo_2805x462_R.png` | Logo (mandatory) | Transparent, tightly cropped, horizontal |
| `*_CoverArt_3840x2160_R.png` | Cover Art (mandatory) | No text; action in the right third |
| `*_Screenshot01..10_3840x2160_R.jpg` | Screenshot (2 mandatory, max 10) | `Steam/Screenshots` doubled with no smoothing |
| `*_FeaturedPSD_3840x2160_R.psd` | Hero Art / Featured PSD (mandatory) | Layers: Background, Primary Art, Logo (pixel layer, not a smart object) |
| `*_MasterImage16x9_3840x2160_R.jpg` | 16x9 Master Image (recommended) | |
| `*_MasterImage4x3_2880x2160_R.png` | 4x3 Master Image (recommended) | |
| `*_MasterImagePortrait_1440x2160_R.png` | Portrait Master Image (recommended) | |

`Tools/psd.py` writes the layered PSD (RLE, 8-bit RGB) without extra libraries.
