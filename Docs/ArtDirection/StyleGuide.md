# AntiBacteria — Art Style Guide

Direction: **Downwell-inspired pixel art** — a strict 3-colour palette where colour means role and
shape tells things apart. Reference images in this folder:

- `antibacteria_style_target_downwell.png` — the approved style target (native 320×180, shown at 4×)
- `antibacteria_palette_swaps.png` — the same frame in four palettes
- `antibacteria_bullet_options.png` — bullet shape options (A/A approved)
- `style_target_mock.py`, `bullet_options.py` — scripts that draw them (exact pixel shapes live here)

## 1. Resolution and scaling

| Setting | Value |
|---|---|
| Native resolution | **320 × 180** |
| Scaling | Whole numbers only: 4× (720p, Switch handheld), 6× (1080p), 8× (1440p), 12× (4K); Steam Deck 1280×800 → 4× with thin bars |
| Unity | URP 2D + **Pixel Perfect Camera** (reference 320×180, upscale render texture on), sprites at **16 PPU**, point filtering, no compression |
| Movement | Positions may be sub-pixel internally; rendering snaps to the pixel grid. Camera smoothing must not jitter (prove it in the vertical slice). |

## 2. Palette: three colour roles

Every sprite is drawn with palette **indices**, not real colours:

| Index | Role | Used for |
|---|---|---|
| 0 | **Background** | The microscope field |
| 1 | **Foreground (white)** | The player, the player's shots, enemy outlines and bodies, the arena wall, neutral effects |
| 2 | **Accent (red)** | Anything that needs attention: enemy cores, enemy bullets, vitamins, hearts |

- Real colours come from a **palette-swap shader** (index → palette lookup), so palettes can be
  changed instantly. Ship palettes: Classic, Petri dish, Gram stain and **Colour-blind safe**
  (see `antibacteria_palette_swaps.png`). Extra palettes can be unlockable rewards and Steam achievements.
- **No other colours.** "Dim" detail uses **dot patterns** of index 1: dotted outlines, checkerboard fills and sparse dots.
- Keep the field calm: background dust stays sparse (about 0.07 % of pixels). Gameplay objects must be the busiest thing on screen.

## 3. Shape language (what tells things apart)

| Object | Shape | Colour | Size (native px) |
|---|---|---|---|
| Player | Solid round cell with a dotted halo; the **nucleus shifts toward the aim** (the sprite never rotates) | white, nucleus = background | 13 (r 6) |
| Player shot | **Pellet streak**: 2-px-wide streak, 5 px long, along the flight direction | white | 2 × 5 |
| Enemy bullet | **Hollow orb**: 5 × 5 ring with a centre dot | red | 5 × 5 |
| Vitamin | Capsule with a white highlight pixel | red + white | 5 × 3 |
| Small enemy | Outlined cell with a **red core**; silhouette varies by type (spikes, tentacles, …) | white + red | 11–17 |
| Big enemy | Dithered body, outline, big red core | white + red | about 27 |
| Segmented enemy (snakes) | Solid head with red eyes, then hollow ring segments shrinking toward the tail | white + red | 4–11 per segment |
| Boss | 64–128 px, same rules: white body, red weak points | white + red | 64–128 |
| Kill effect | Dotted ring shockwave plus a few debris pixels | white | — |

Rules:
- **Red = danger or reward, white = everything else.** Red things are told apart by shape: orb = bullet, capsule = vitamin.
- **Every enemy type gets a unique silhouette** that reads in a single colour (needed for the colour-blind palette).
- **Avoid rotating sprites.** Show direction with a moving part (nucleus, eyes) or use pre-drawn directions.
  Small things that must rotate (shots, snake segments) are re-drawn on the pixel grid by the low-res render.

## 4. HUD (inside a TV-safe margin)

| Element | Position | Look |
|---|---|---|
| Hearts | top left | 7 × 6 red hearts, empty = outline |
| Slow-motion meter | top centre | 61 × 6 white frame with a white fill |
| Vitamins | top right | capsule icon + `x1250` in the pixel font |
| Level | bottom left | `1-3` (world-level) |

- Keep HUD elements on clear background (no dust or effects behind them).
- **Pixel font:** the mock-up uses a 3 × 5 digit font. The real font must cover English, Russian (Cyrillic),
  French, German and Spanish, and allow commercial use.

## 5. Still to define
- Main menu and other screens in this style
- Animation rules: frame counts, hit flash (all-white frame), death
- Effects: slow motion (palette shift?) and the big explosion
- Boss designs
- Audio direction (the original game has no audio)
