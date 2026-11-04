# Steam art

Two complete sets, same file names and sizes:
- `Illustrated/Art/` — smooth hand-drawn style (rounded lowercase logo, the hero cell and an angry bacterium as the i-dots). Redraw with `python3 Illustrated/Tools/illus.py Illustrated/Art` (needs Pillow and Playwright with Chromium: `pip install playwright && playwright install chromium`).
- `Art/` — pixel-art style, described below.

## Pixel-art set

Every image is drawn from the game's own sprites and font (m6x11), in the Classic palette, then scaled up by a whole number so the pixels stay crisp. To redraw them all after a change: `python3 Tools/make_all.py` (needs numpy and Pillow). Layouts live in `Tools/make_all.py`, one function per image.

| File | Size | Upload in Steamworks |
|---|---|---|
| `Art/store/store_capsule_header.png` | 920×430 | Store Page → Graphical Assets → Header Capsule |
| `Art/store/store_capsule_small.png` | 462×174 | Small Capsule |
| `Art/store/store_capsule_main.png` | 1232×706 | Main Capsule |
| `Art/store/store_capsule_vertical.png` | 748×896 | Vertical Capsule |
| `Art/store/store_page_background.png` | 1438×810 | Page Background (dimmed, no text) |
| `Art/library/library_capsule.png` | 600×900 | Library Assets → Library Capsule |
| `Art/library/library_header.png` | 920×430 | Library Header |
| `Art/library/library_hero.png` | 3840×1240 | Library Hero (no text, as Steam requires) |
| `Art/library/library_logo.png` | 1280×720, transparent | Library Logo (then position it over the hero) |
| `Art/events/event_cover.png` | 800×450 | Events: cover image (generic) |
| `Art/events/event_header.png` | 1920×622 | Events: header image (generic) |
| `Art/broadcast/store_broadcast_left.png`, `_right.png` | 199×433 | Store Page → Broadcast side images |
| `Art/icons/community_icon_184.jpg` | 184×184 | App Admin → Community Icon |
| `Art/icons/client_icon.ico` | 16 + 32 | Client Icon |
| `Art/icons/shortcut_icon.ico`, `shortcut_icon_256.png` | 16–256 | Client shortcut icon (Linux uses the PNG) |

The PSD templates in `Unity/AntiBacteria_Unity/Assets/Steam/Steam Game Templates` were used for the sizes only.

## Localized store art

Japanese, Korean and Chinese capsules: `Art/Localized/<lang>/` (pixel, `Tools/make_loc.py`) and `Illustrated/Art/Localized/<lang>/` (illustrated, for the A/B test). Store texts and localized description banners: see `Localization/README.md`.
