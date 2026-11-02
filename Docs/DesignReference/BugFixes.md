# 2013 bugs: fix decisions for the Unity port

Decision (Nov 2026): **fix every 2013 bug** rather than reproduce it. This file lists each one, the fix and its
status. Sources are the QUIRK notes in `01`–`05` of this folder. When you port a system, work through its rows and
set them to Done.

Status: **Done** = fixed in the Unity code today. **By design** = the Unity architecture can't have this bug.
**When porting X** = fix it as part of that system.

## Core, timing, input

| # | 2013 bug | Fix | Status |
|---|---|---|---|
| 1 | Speeds, bonus duration, grow intervals, camera smoothing are per frame (60 fps assumed). | Everything is time-based; 1 frame unit = 1/0.6 s (`Legacy.SecondsPerFrameUnit`). Physics fixed at 60 Hz. | Done |
| 2 | Enemy `update` ran twice per frame, doubling angular steps. | One update per enemy; angular speeds tuned as `step × 120 °/s`. | Done |
| 3 | Mixed real-time and scaled clocks (fire rate, slow motion). | Gameplay uses scaled game time; only the slow-motion meter uses real time. | Done |
| 4 | Diagonal keyboard movement ~9 % faster. | Move vector clamped to length 1. | Done |
| 5 | Mouse-only mode never stops; NaN when the cursor is on the player. | Movement is keys/stick only; aim ignores a zero vector. | Done |
| 6 | Keys matched by character (Caps Lock / AZERTY break WASD); Cmd+key half-handled; no gamepad; no rebinding. | Rewired with physical keys, gamepad, rebinding later in Options. | Done (rebinding UI: when porting menus) |
| 7 | "Only Arrows" mode aims at a stale mouse point. | Aim comes from the right stick or the live mouse. | Done |
| 8 | Space while paused starts slow motion silently, then leaves it stuck on. | Slow motion ignores input while paused. | Done |
| 9 | Esc on the end screen resumes the game; player can act during "You Won". | Level over locks pause and disables player input; the result panel needs a button. | Done |
| 10 | Slow motion leaks past the end of a level. | Level over force-ends slow motion; the physics step is reset from a constant. | Done |
| 11 | Camera zoomed out ~0.58 and never clamps. | Pixel-perfect 320×180 camera clamped to the arena. | Done |

## Player, weapons, bonuses

| # | 2013 bug | Fix | Status |
|---|---|---|---|
| 12 | Enemies damage only on contact begin; overlapping after the blink deals no damage. | Damage on enter and stay, gated by the 2 s invulnerability. | Done |
| 13 | Picking up any bonus cancels post-hit invulnerability. | Pickups don't touch invulnerability. | Done |
| 14 | Dead player's body stays as an invisible solid obstacle. | Body and collider are disabled on death. | Done |
| 15 | Possible duplicate fire timer doubles fire rate. | Single cooldown in `PlayerController`. | By design |
| 16 | No collision filtering: bullets cancel each other, any bullet hurts the player, bullets knock the player back. | Physics layers; player shots hit only enemies and walls; shots are triggers. | Done |
| 17 | Life/BOOM taken during a weapon bonus makes the weapon bonus permanent. | One `ActiveBuff` slot per kind with its own expiry. | Done |
| 18 | A new timed bonus inherits the old one's remaining time. | Picking up a bonus restarts its full duration. | Done |
| 19 | Bonus type 7 (Fast Movement) reads past the colour array. | Every bonus type has a defined colour in its data asset. | Done |
| 20 | `shootCount == 4` "cap" doesn't cap. | Clamp at 4. | Done |
| 21 | Water/Tornado fire every 0.033 s after a slow-motion toggle (`/3` vs `/5`). | Fire rate comes from weapon data only. | Done |
| 22 | `bigExplosion`/`fastRun` flags never reset. | Abilities reset their state on end. | By design (only Slow Motion exists as an ability; Boom is a bonus with no lingering state) |
| 23 | Slow-motion tint shows for every special ability. | Tint belongs to slow motion only. | By design (the slow-motion tint was never ported; only slow motion changes time) |
| 24 | Bonus spawn "too close to player" check compares the wrong vector. | Real distance check. | Done |

## Enemies and scoring

| # | 2013 bug | Fix | Status |
|---|---|---|---|
| 25 | Vitamins credited when they drop, not when collected. | Credited on pickup. | Done |
| 26 | Hitting a dying enemy can cut its death short or pay the score twice. | `TakeHit` ignores enemies that aren't Alive; `Die` runs once. | Done |
| 27 | Score paid only for bullet kills in campaign; BOOM / big-explosion kills pay 0; Chorux (200) never paid; BigRedNoise, Gridium, Tentacle, CaterPillar pay 0. | Every kill pays its `killScore` in every mode; set real values for the 0-pay enemies. | Done |
| 28 | Factory IDs 12, 16, 27 return nil (Armagedon spawns nothing); enum comment says 22 for 27. | Catalog warns and falls back; Armagedon mode dropped from the port, so nothing asks for those IDs. | Done |
| 29 | Rush / Take-the-Flag pool reads index 19 of a 19-entry array; enemies per wave grow without limit. | Bounds-checked pool; explicit wave cap (ModeSettings: 6 per wave). | Done |
| 30 | Gnida / ZamriGnida dodge uses an un-normalised perpendicular (50 / 36 m/s sidesteps). | Normalise, tune the dodge speed by feel. | Done |
| 31 | ZigZagus wobble locked to the world diagonal, phase re-randomised every update. | Wobble relative to heading, phase set once. | Done |
| 32 | Spawner interval halves after any slow-motion toggle. | Interval from data only. | Done |
| 33 | Loctopus drops 2 vitamins, not 3. | 3. | Done |
| 34 | Chain enemies follow with per-frame lerp; body segments absorb bullets; score per segment. | Time-based follow; score once per chain. | Partly done (time-based follow; segments still pay individually) |
| 35 | Big explosion on chain enemies measures distance to the world origin and deletes the whole chain. | Distance per segment from the blast. | Done |
| 36 | ShootingSerpentus never despawns and resumes shooting after a slow-motion toggle. | Normal despawn; shooting from its own timer. | Done |
| 37 | Sunux orphans its live bullets. | Bullets owned by a pool, cleared on death. | Done |
| 38 | Agent removal: one dead agent per frame; possible use-after-free of children. | Unity object lifecycle. | By design |

## Bosses and levels

| # | 2013 bug | Fix | Status |
|---|---|---|---|
| 39 | Final boss Frunctus has 1 HP (comment says 150). | 150 HP. | Done |
| 40 | Anakonda and Frunctus fire every frame after bullet time on Normal (`prevShootRate` = 0). | Fire rate from data only. | Done |
| 41 | BigRedNoise: big-explosion kill doesn't win the level (soft-lock); can become permanently invulnerable. | Boss death always ends the level; invulnerability tied to live children. | Done |
| 42 | Level 20 spawns 1 or 2 Infernos depending on frame rate. | Explicit boss count in level data. | Done |
| 43 | Boss win requires exactly 1 agent alive. | Win = boss defeated. | Done |
| 44 | No boss HP bar (the asset is the ability meter). | Real boss HP bar. | Done |
| 45 | Level 49 has `number 48` (wrong "boss next" banner). | Fixed in the importer. | Done |
| 46 | Grow-counter guard uses `||` instead of `&&`. | Runner stops when paused or won. | Done |

## Economy, saves, UI

| # | 2013 bug | Fix | Status |
|---|---|---|---|
| 47 | Cached vitamin balance refunds spending; store purchase sets the wallet instead of adding. | Single wallet in `GameSession`/save; purchases add. | Done |
| 48 | Upgrade "Normal" row keyed `""` works by accident. | Explicit keys. | Done |
| 49 | Saves AES-locked to the machine; unlocks happen on level load, not win; scores never shown. | New versioned save (Steam Cloud ready); unlock on win; show best scores. | Done |
| 50 | Rush pays +250 for an unfinished zone at death. | Pay only completed zones. | Done |
| 51 | UI scale depends on display width; HUD text scales by ar². | Fixed 320×180 canvas, integer scaling. | Done |
| 52 | Menus mouse-only. | Menus work with keyboard, gamepad (Rewired UI actions) and mouse. | Done |
| 53 | "Music" setting does nothing; control labels wrong; About is blank; level-select icons have no numbers. | Options saved and applied (volumes wait for audio); Control Type removed; About shows credits; numbered level select still to do. | Partly done (options saved, About credits, numbered level select; volumes wait for audio) |
| 54 | No Get Ready on Restart; two 5 s timers race at level change. | One level-flow state machine (`CampaignLevelRunner`). | Done |
| 55 | Localised strings used as format strings. | Localised words are concatenated in code, never used as format strings. | Done |
| 56 | StoreKit observer leak, missing `[super onExit]`, layers passed to `replaceScene`. | Not applicable in Unity. | By design |
| 57 | Many 2013 translations were wrong or machine-made ("Compaign", "Bыход" with a Latin B, "Die Kerbe" for Score). | Menu strings corrected in all 5 languages; gameplay strings (weapons, bonuses) still to review. | Done (menus, weapon and bonus names corrected in all 5 languages) |
