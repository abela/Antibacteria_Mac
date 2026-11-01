# AntiBacteria — Design Reference

Extracted from the 2013 Objective-C / cocos2d-mac source (`Mac/Bacteria/`) as the source of truth
for the Unity / C# rewrite. It describes what the game **does** — rules, numbers, timings, quirks —
so it can be rebuilt with Unity's own systems rather than translated line by line.

Every gameplay number cites the original `File.mm:line` so it can be checked. Items marked
**QUIRK:** are oddities or bugs in the original; decide per item whether the rewrite keeps or fixes them.
The playable Mac build (`Mac/`) is the reference for feel: compare side by side while porting.

## Contents

| File | Covers |
|---|---|
| [01_player_weapons_bonuses_core.md](01_player_weapons_bonuses_core.md) | Player character, controls, upgrades, weapons, bonuses/vitamins, physics, camera, constants |
| [02_enemies_framework_and_group_A.md](02_enemies_framework_and_group_A.md) | Enemy framework (spawning, targeting, damage, death), **enemy ID table**, first group of enemies |
| [03_enemies_group_B_and_bosses.md](03_enemies_group_B_and_bosses.md) | Segmented enemies (snakes, caterpillars…) and the 5 bosses |
| [04_game_modes_levels_progression.md](04_game_modes_levels_progression.md) | Game modes, level fields and progression, spawning rules, scoring, save data, audio, localisation |
| [05_ui_flow_store_platform.md](05_ui_flow_store_platform.md) | Screen flow, every screen and the HUD, store / in-app purchases, Game Center |

## Data

| File | Contents |
|---|---|
| `data/levels/levels_all.json` | All 50 levels (5 boss worlds × 10), values normalised to numbers |
| `data/raw/*.json` | Other original plists converted as-is: sprite-sheet frame data, game world resources, localised strings |

## The game at a glance

- **Top-down arena shooter.** The player is a green cell in an arena of about 2136 × 1496 pt
  (`bg.jpg` 2176 × 1536 px, inset 20 pt). The camera always follows the player and appears zoomed out to ~0.55×.
- **28 enemy type IDs** (0–27):
  - 20 spawnable regular enemies.
  - 2 parts created by their parent enemy: Chorux's defenders (ID 12) and Lotus segments (ID 16).
  - 5 bosses.
  - `ArmagedonBacteria` (ID 27), which can never be created.
  - The full table is in doc 02.
- **5 bosses**, one ending each world of 10 levels:

  | Level | ID | Boss | HP |
  |---|---|---|---|
  | 10 | 22 | BigBoss | 255 |
  | 20 | 23 | Inferno | 300 |
  | 30 | 24 | BigRedNoise | 400 |
  | 40 | 25 | Anakonda | 24 segments × 14 |
  | 50 | 26 | Frunctus | **1** (see below) |

- **Game modes:** Campaign (the 50 levels), Rush, Take the Flag and Peace Walker are in the menu. Armagedon exists in code only.
- **Economy:** vitamins dropped by enemies buy one-off upgrades (four extra shot types, Double Speed, Immortality).
- **Special ability:** Slow Motion (0.35× time, Space key) on a recharging meter shown at the top of the HUD.
- **Pick-ups:** timed bonuses (extra shot types, Life, BOOM, which deals −20 hits to enemies within 700 pt).
- **No audio:** there are no sound files, so all music and sound effects have to be made for the rewrite.

## Findings that change rewrite decisions

These are the most important items from the five documents. Each is detailed, with sources, in its file.

**Timing (affects everything)**
- Most logic is counted **per frame at 60 fps**, not in seconds. This covers player acceleration (an impulse every frame with damping 6), bonus durations (800 frames ≈ 13.3 s), the level "grow" counters, the Rush/Flag generators, snake segment spacing, and spiral turn rates.
  - Enemies are also **updated twice per frame** (`GameManager.mm:471` and `AgentsManager.mm:173`), so their turn rates run at about 120 updates/s.
  - The rewrite should convert all of these to seconds, using the conversions in the docs, and tune by comparing against the Mac build.
- Some timers ignore Slow Motion and some don't. Pick one rule.

**Bugs to fix, not copy**
- **The final boss Frunctus has 1 HP:** `hitCount = 1.0f;//150` (`Frunctus.mm:44`). 150 was intended.
- **Vitamin economy:**
  - Vitamins are credited when they *drop*, not when collected (`VitaminManager.mm:80`).
  - The balance can be overwritten after spending.
  - A store purchase *replaces* the balance (`StoreLayer.mm:279`).
- **Factory IDs 12, 16 and 27 return nil.** The Rush/Flag enemy pick can read past the end of its 19-entry array.
- **Boss soft-locks:**
  - BigRedNoise can be killed by the big explosion without winning the level.
  - It can also stay invulnerable forever.
  - Level 20 may spawn one or two Infernos depending on frame rate.
- **Snakes:** the big explosion measures the player's distance to world (0, 0) instead of to the snake. A killed ShootingSerpentus is never removed.
- **Data:** `Boss_5/level_9.plist` has `number: 48` instead of 49.

**Design choices to make deliberately**
- **Collisions:** there is no collision filtering at all.
  - Any two bullets cancel each other, and any bullet hurts the player.
  - Enemies are sensors that only hurt on first contact.
  - Use proper layers in Unity.
- **Scoring:** points only count in Campaign and only for bullet kills. They're saved only for won normal levels and never shown anywhere. Effectively there's no score system, so design one, plus Steam leaderboards.
- **Controls:** three hard-coded schemes.
  - Keys are matched by typed character, so Caps Lock and AZERTY layouts break them.
  - There is no rebinding.
  - There is no gamepad support, and the menus are mouse-only.
  - Build the controls on Unity's Input System with gamepad support from day one. The list of actions to bind is in doc 01.
- **UI scale:** the original scales its UI by monitor width, so it looks different on every display. Pick one reference look.
- **Store / IAP:** four consumable vitamin packs ($0.99–$9.99). Remove them for a premium release and rebalance upgrade prices; Immortality costs 1,000,000 vitamins.
- **Saves:** encrypted with a key tied to the Mac's hardware ID. Don't migrate old saves; build a fresh save system with Steam Cloud.
- **Game Center:** four leaderboard IDs are defined but never submitted to, and there are no achievements. Steam achievements and leaderboards have to be designed new.
- **Unfinished features worth finishing:** off-screen enemy direction arrows, a boss health bar (there is none; boss damage only shows on the sprite), and an About/credits screen (blank, and also where the game ends after level 50).

## Coordinates and units

- Original game coordinates are cocos2d **points**, origin bottom-left, logical screen **1024 points wide**
  (height = 1024 ÷ display aspect ratio).
- Times are in seconds unless stated. Logic that runs per frame (frame-rate dependent) is called out,
  because Unity runs at a variable frame rate.
