# 04 - Game modes, level progression, spawning, scoring, save data, audio, localisation

Source root: `/home/claude/legacy` (paths below are relative to it). Every line number refers to the file named
next to it. "Frame" means one call of `MainGameLayer tick:` (scheduled every rendered frame,
`Layers/MainGameLayer.mm:96,153`); cocos2d-mac runs at its default 60 fps (no `setAnimationInterval` call
exists in the tree), so **1 "unit" = 100 frames = 1.667 s at 60 fps** wherever the code adds
`GAME_NEXT_SIMULATION_STEP_INTERVAL` (= `0.01f`, `ConfigConstants.h:25`) once per frame.
Physics: `PTM_RATIO 32` (`GameCore/Physics/Abstracted/Physics.h:18`), also used as `ASPECT_RATIO 32.0f`
(`ConfigConstants.h:353`): **32 points = 1 Box2D metre**.

## Overview

| Class | Role | One-line summary |
|---|---|---|
| `GameModeManager` | singleton dispatcher | Picks the mode class from `currentGameModeType`, forwards every lifecycle call to it, ticks `BonusManager` and `VitaminManager`. |
| `GameMode` | abstract base | Shared per-frame enemy/objective generator used by Rush and Capture-the-Flag; enemy pool table; unused spawn-point markers. |
| `CompaignGameMode` | mode | **Campaign**: 50 timed levels from plist files; survive the clock (or kill the boss) to advance. |
| `TakeTheFlagGameMode` | mode | **Capture The Flag**: endless; grab flags one at a time, difficulty ramps per flag. |
| `RushGameMode` | mode | **Rush**: endless; stand in a capture zone until it fills, difficulty ramps per zone. |
| `PeceWalkerGameMode` | mode | **Peace Walker**: endless, no shooting; reach each green flag within 10 "seconds" while Tentacles chase you. |
| `ArmagedonGameMode` | mode (dead) | Hidden, broken mode: spawns one `ArmagedonBacteria`, which the factory cannot create. |
| `Flag` | objective object | Static sensor box picked up on touch (CTF). |
| `PeaceFlag` | objective object | Green-tinted `Flag` (Peace Walker). |
| `RushStation` | objective object | Large circular capture zone with a radial progress meter (Rush). |
| `Level` | data | One parsed `level_N.plist`, with difficulty adjustment of the spawn interval. |
| `LevelManager` | singleton | Loads levels by number, grows enemies-per-wave over time, picks random enemy types, stores unlocks. |
| `SpawnManager` | singleton | Geometry helpers: spawn points on the arena edge, around the player, around a point, Gridium lines, bonus positions. |
| `GameManager` | singleton | Game flow: start, pause, death detection, level win, boss win, game finished, restart, menus, bullet time, score display. |
| `ScoreManager` | singleton | Accumulates the campaign level score and saves per-level best and the campaign total; Game Center stubs. |
| `PlistAction` | utility (dead) | Write/read arrays to `~/Documents`; never called. |
| `GameResourceManager` | singleton | Loads 3 sprite sheets, owns the 3 batch nodes, toggles the "slow-motion" blend. |
| `SoundManager` | singleton (dead) | Wrapper over SimpleAudioEngine; **never called - the game has no audio**. |
| `LocalizationManager` | singleton | 5-language string table (`localizations.plist`), language choice saved AES-encrypted. |
| `GameSettingsManager` | singleton | Settings plus vitamin (currency) balance; AES-encrypted NSUserDefaults. |
| `NSUserDefaults+MPSecureUserDefaults` | 3rd-party category | Stores `{value, MD5(archive(value)+secret)}` dictionaries to detect tampering (no encryption). |

---

## 1. GameModeManager (`Managers/GameModeManager/GameModeManager.mm`)

**Purpose.** Holds the selected mode type and the live mode object. Everything outside talks only to this manager.

- Default type `kCompaignMode` (`:39`). `GameModeType` enum: `kCompaignMode=0, kTakeTheFlagMode=1, kRushMode=2,
  kArmagedonMode=3, kPeaceWalkerMode=4` (`ConfigConstants.h:260-268`).
- `Run` (`:46-73`) creates the class with `NSClassFromString` (names `CompaignGameMode`, `TakeTheFlagGameMode`,
  `RushGameMode`, `PeceWalkerGameMode`, `ArmagedonGameMode`) and calls `Run`. Called from `GameWorld Run`
  (`GameWorld/GameWorld.mm:174`) when a game scene starts.
- `update` (`:85-90`) runs every frame (from `GameWorld update`, inside `GameManager managePhysics`, only when
  not paused): mode `update`, then `BonusManager update`, then `VitaminManager update`.
- Restart (`:97-103`): mode `simulateGameModeRestart`, release all bonuses and vitamins, reset input.
- Release (`:105-112`): mode `releaseGameMode`, release bonuses, free the mode, release vitamins (which **saves the
  vitamin balance**, see section 13). Called from `GameWorld releaseGameWorld` (`GameWorld/GameWorld.mm:219`), i.e. on
  quit to menu or level select.
- `canGenerateBonus:` (`:129-132`) is called by objectives when collected (flag pickup, rush station complete)
  to allow the next objective.
- Mode selection UI: `Layers/ChooseGameModeLayer.mm:94-123`. Campaign goes to the level select screen, the other modes
  start immediately. **Armagedon's menu item is created but not added to the menu** (`:79,81`), so players can't reach it.
- **QUIRK:** there is no unlock gating. `openedGameModes` is saved (default 1, `SettingsManager/GameSettingsManager.mm:80`)
  but never read, so all four visible modes are available from the first launch.

**Unity mapping.** A `GameModeController` MonoBehaviour that owns an `IGameMode` (one class per mode),
selected from a `GameModeDefinition` ScriptableObject. Lifecycle: `Begin`, `Tick(dt)`, `OnPlayerDied`, `Restart`, `End`.

## 2. GameMode base (`Managers/GameModeManager/GameModes/GameMode.mm`)

**Enemy pool for Rush/CTF** (`Run`, `:21-43`). This is a 19-entry table, indexed 0..18. The first entries unlock first:

| idx | type (enum value) | idx | type | idx | type |
|---|---|---|---|---|---|
| 0 | BlueCorosia (0) | 7 | Sunux (8) | 14 | ZamriGnida (21) |
| 1 | Loctopus (1) | 8 | ShootingCorosia (7) | 15 | Caterpillar (10) |
| 2 | RedNoise (2) | 9 | BlueCorosia (0) | 16 | Serpentus (14) |
| 3 | Chorux (11) | 10 | Spawner (13) | 17 | LotusBacterius (15) |
| 4 | Adrenalium (9) | 11 | Zigzagus (18) | 18 | ShootingSerpentus (19) |
| 5 | BigHunterus (5) | 12 | Gnida (20) | | |
| 6 | BigBlueCorosia (6) | 13 | Zigzagus (18) | | |

**Shared per-frame generator** (`update`, `:96-121`). Used only by Rush and CTF, which call `[super update]`:
1. `enemiesGeneratorStepCounter += 0.01` per frame. When it reaches `enemiesGeneratorLimitConst + (1 - timeScale)`, the mode
   calls `createNextEnemy` and resets the counter to 0. In bullet time (`timeScale 0.35`) the threshold grows by 0.65.
2. If `canGenerateBonus`, `bonusGeneratorTick += 0.01` per frame. When it reaches `bonusGeneratorLimitConst + (1 - timeScale)`,
   the mode spawns the next objective (`generateNextRandomBonus`), sets `canGenerateBonus = NO`, and
   **resets the enemy counter to 0**, which delays the next enemy wave.

`initSpawnPoints` (`:45-74`) places 4 `WX_circle_white.png` markers at the arena corners. Rush and CTF comment out the
call (`RushGameMode.mm:47`, `TakeTheFlagGameMode.mm:47`), so it is **dead code**.

**QUIRK:** both counters are **per frame** and not scaled by `dt`, so spawn rates depend on frame rate. 1.5 units = 150
frames = 2.5 s at 60 fps, but 1.25 s at 120 fps.

## 3. Campaign - `CompaignGameMode` (`.../GameModes/CompaignGameMode.mm`)

- **Player-facing name:** `_COMPAIGN`. Strings: "Compaign" (sic, English) / "Кампания" / "La campagne" / "Kampagne" / "Campaña".
- **Objective:** survive each level's clock (80 s for every level). On every 10th level, kill the boss instead.
- **Win (normal level):** the 1-second clock reaches 0 while the player is alive (`GameManager.mm:184-187`).
- **Win (boss level):** `levelHasWonAfterBigBoss` (`GameManager.mm:219-239`) is called by the dying boss
  (`AI/BigBoss.mm:139`, `AI/Inferno.mm:182`, `AI/BigRedNoise.mm:257`, `AI/Anakonda.mm:220`, `AI/Frunctus.mm:296`). It wins
  only if `AgentsManager getEnemiesCount == 1`, meaning the dying boss is the last agent left. On level 50 it sets
  `gameIsFinished` and shows "You finished the game" (`Layers/LevelOverLayer.mm:240-243`). After 5 s it goes to the credits
  (`AboutLayer`, `GameManager.mm:246-253`).
- **Lose:** the player dies. The physics loop sees the character flagged `kDealloc` (`GameManager.mm:438-449`). After 0.1 s it
  calls `showGameOver`: the mode stops, "Game Over" appears, and after 2 s a menu offers Restart / Choose Level / Go To Menu
  (`LevelOverLayer.mm:329-377`). There is no time-out loss.
- **Unlock:** always available; levels unlock as you progress (section 7).

### Timers (real time, NSTimer)
- `loadNextCompaignLevel` (`:33-87`) releases bonuses, resets agents, loads the next level, and reads `enemiescreatetime`
  (already adjusted for difficulty) into `enemyCreateVar` (`:57-58`). The level is a boss level if `number % 10 == 0`
  (`BOSS_LEVEL_CREATE_CONST 10`, `ConfigConstants.h:457`; `:60-62`).
- **First level of a session** (entered from level select, `firstStart`): a one-shot timer waits `enemyCreateVar + 2.0` s
  (`:65-71`). Then `startEnemiesAfterSomeTime` (`:89-102`) starts the spawn timer and the clock.
- **Later levels** (auto-advance): the spawn timer (interval `enemyCreateVar`) and the 1.0 s clock timer start at once
  (`:73-85`). On boss levels **both timers are non-repeating** (`repeats:!weHaveBossLevel`).
- **Clock** `realTimeTick` (`:110-132`): skipped while paused. Each tick sends the current `totalLevelTime` to
  `GameManager updateTickInGame` (shown as `"%i : %02d"`, e.g. "1 : 20") and **then** decrements it. With `time=80`
  the display goes 1:20 ... 0:01, and the tick that sends 0 wins. That is 81 ticks, about 81 s.
- **Bonuses:** every 20th tick (`BONUS_CREATE_LIMIT_CONST 20`, `:19`), except on boss levels and on tick 80 (= `time`),
  one random usable bonus spawns (`BonusManager generateRandomBonus`, a random type 0..7, `BonusManager.mm:18,55-62`).
  That gives **3 bonuses per level, at ticks 20, 40 and 60**.
- **Spawning** `createRandomEnemy` (`:134-144`): skipped while paused. Spawns `LevelManager totalEnemiesPerCreation`
  enemies. Each type is chosen uniformly at random from the level's `enemies` array (`LevelManager.mm:124-130`). All
  enemies appear at a random point on the arena edge (`AgentsManager.mm:81-90` → `SpawnManager getValidOuterSpawn`).
- Boss levels: the one-shot spawn timer fires once after `enemyCreateVar` s and spawns `totalEnemiesPerCreation` bosses.
  The one-shot clock ticks once, so the HUD freezes at "1 : 20" (the boss HUD normally hides it, see the anomaly in 7.4).
- **Level won:** `GameManager gameLevelHasWon` (`GameManager.mm:204-217`) calls `CompaignGameMode levelHasWon`
  (`:146-161`), which stops the timers and calls `AgentsManager levelHasWon`. It also saves the score
  (`ScoreManager synchronizeLevelScore`). The UI shows "You Won" (`LevelOverLayer.mm:572-584`), then after 2 s
  "Level N+1" or "Get Ready For the Boss", then "Get Ready". After **5 s** the next level loads
  (`GameManager.mm:241-286` → `gameModeActionLoadNextLevel`).
- **Restart** (`simulateGameModeRestart`, `:183-218`): `LevelManager restartLevelManager` reloads the same level, and the
  timers restart with no extra 2 s delay.
- **Death** (`:162-182`): on normal levels all agents are cleared and the timers stop. On boss levels the agents stay.
- **Bullet time** (`refreshGameMode`, `:226-260`; called whenever slow motion toggles, normal levels only):
  - ON (`timeScale = 0.35`): `enemyCreateVar += 1 - 0.35` (= +0.65 s), and the clock interval becomes
    `enemyCreateVar + 0.65`. Level 1 example: spawns every 2.65 s, one clock second every 3.3 s real.
  - OFF: `enemyCreateVar = prevEnemyCreateVar`, and the clock interval becomes `timeScale` (1.0 s).
  - Both timers are recreated from zero on every toggle.
  - **QUIRK:** the slow-motion clock period is tied to the enemy interval (it looks unintended). Toggling also resets the
    spawn countdown, so rapid toggling would delay spawns.
- `update` (`:220-224`): `AgentsManager update` and `LevelManager update` every frame. Campaign does **not** use the
  `GameMode` base generator.
- **Kill score is awarded only in Campaign** (`GameCore/Physics/Abstracted/ContactListener.mm:70,84`).

## 4. Capture The Flag - `TakeTheFlagGameMode` (`.../TakeTheFlagGameMode.mm`)

- **Name:** `_CAPTURE_FLAG`: "Capture The Flag" / "Захватить Флаг" / "Capturer le Drapeau" / "Schlagen das Flag" / "Capturar la bandera".
- **Objective / endless:** collect flags for points for as long as you survive. No timer (the HUD timer is hidden,
  `LevelOverLayer.mm:411-412`). No win condition.
- **Lose:** death. `stopGameModedAfterCharacterDeath` → `gameOverInGameMode` (`:51-57,84-93`): input is disabled and
  bonuses and agents are cleared. The UI shows "Game Over", then Restart / Go To Menu.
- **Start** (`Run`, `:25-49`): `canShoot = YES`. Generator values: `bonusGeneratorLimitConst = 1.0`,
  `enemiesGeneratorLimitConst = 1.5`, `enemiesGeneratorCount = 1`, `enemiesRandomer = 0`, `enemiesGeneratorStepCounter = 0.01`.
- **Flag cycle:** about 1 unit (100 frames, 1.67 s at 60 fps) after the previous flag is gone, `generateFlag` places a
  `Flag` (section 6) with `SpawnManager generateValidBonusSpawn`: 700-710 pt from the player, clamped 30 pt inside the
  arena (`SpawnManager.mm:261-300`). Touching it scores `FLAG_CONST_POINT = 100` (`ConfigConstants.h:440`) and re-enables
  generation (`ContactListener.mm:127-131,149-153`).
- **Enemies:** each wave spawns `enemiesGeneratorCount` enemies. Each type is `enemies[arc4random() % (enemiesRandomer+1)]`
  (`:74-82`), so only BlueCorosia appears at first.
- **Ramp** (`generateNextRandomBonus`, `:107-134`). `flagTakeCount` increments when a flag **spawns**, so "the Nth flag"
  means after N-1 captures:
  - every 4th flag: `enemiesRandomer++` (cap 19), which unlocks the next enemy in the pool table;
  - every 6th flag: `enemiesGeneratorLimitConst -= 0.1` (min 0.7 units ≈ 1.17 s) and `enemiesGeneratorCount++`
    (**no cap**);
  - every 8th flag: `mainCharacter levelUp:YES` (more shots per volley, see the character doc) and one random usable bonus.
- **Difference from Rush:** the objective is a small pickup placed far from the player, scored on touch.
  Rush uses a big zone you must hold.
- **QUIRK (bug):** the pool has 19 entries (0..18), but `enemiesRandomer` is capped at 19, so `% 20` can read
  `enemies[19]`, past the end of the `malloc` block (`GameMode.mm:23`, `TakeTheFlagGameMode.mm:116-117`). This is reachable
  from the 76th flag. Cap it at 18.
- **QUIRK:** no high score is saved for this mode, and the leaderboard ID `com.AntiBacteria.CaptureTheFlag`
  (`ConfigConstants.h:28`) is never reported.

## 5. Rush - `RushGameMode` (`.../RushGameMode.mm`)

- **Name:** `_RUSH`: "Rush" / "Схватка" / "S'élancer" / "Treiben" / "Acometida".
- **Objective / endless:** stand inside capture zones until each fills; survive as long as possible. No timer, no win.
- **Lose:** death (same flow as CTF, `:82-89,155-161`).
- **Start values:** identical to CTF (`:25-48`).
- **Zone cycle:** `generateStation` → `BonusManager generateRushStation` places a `RushStation` 500-520 pt from the
  **screen centre** (`SpawnManager getRandomPointAroundPoint`, `BonusManager.mm:80-91`). When it completes and is released,
  it scores `RUSH_CONS_POINT = 250` (`ConfigConstants.h:441`) and re-enables generation (`RushStation.mm:119-125`).
- **Ramp:** same 4/6/8 rules as CTF, counted with `rushTakeCount` (`:97-124`).
- **QUIRK (bug):** the +250 points sit in `RushStation releaseAgent`, which also runs when bonuses are **released
  without capture**. Game over calls `releaseBonusManager` (`:160`), so an unfinished zone pays +250 at death. On restart
  the HUD is reset just after, so that case is hidden.
- Same `enemies[19]` out-of-bounds bug as CTF (`:105-107`).

## 6. Objective objects (`Managers/BonusManager/`)

All three derive from `BonusObject` (see the bonus doc). `BonusManager update` deletes any object whose `flag == kDealloc`
and calls `releaseAgent` on it (`BonusManager.mm:106-117`).

### Flag (`Flag.mm`)
- Sprite `bonus.png` (whole file, not from an atlas) (`:20`). Game type `kBonusObject`, bonus type `kFlagBonus` (8).
- Box2D **static** body, `SetAsBox(1.3, 1.3)`, which is a half-extent, so the box is 2.6 m = **83.2 pt square**. Sensor,
  density 1.0, friction 0.3, `allowSleep=false` (`:23-24,36-60`).
- Collected when the main character touches it (sensor contact, `ContactListener.mm:121-131`).

### PeaceFlag (`PeaceFlag.mm`)
- A `Flag` tinted green `ccc3(0,255,0)` (`:17`), bonus type `kPeaceFlagBonus` (9). Pickup: +100 points and
  `gameModeSpecialAction` (`ContactListener.mm:132-136`).

### RushStation (`RushStation.mm`)
- Game type `kRushStation`. Sprite `bonus.png` at `scale 5.0`, opacity `DEFAULT_OPACITY 55` (`:13,30-35`).
- Static **circle** sensor, radius `5.0` m = **160 pt** (`:34,62-85`).
- Radial `CCProgressTimer` (also `bonus.png`), placed 1.2 × half the sprite height above it, z 1000
  (`:49-59`, `BonusManager.mm:88`).
- Per frame (`:87-117`): if the player is inside (set on contact begin/end, `ContactListener.mm:165-175,203-213`),
  progress rises by `0.5 * timeScale` %. Otherwise it falls by `0.5 * timeScale` % (floor 0). At 100 % the station is
  flagged `kDealloc`.
- That is **200 frames inside (3.33 s at 60 fps)**. **QUIRK:** this is frame-based; the 100 % test reads the old value,
  so completion lands one frame late. `opacityCounterStep` (`:36`) is never used.

## 7. Peace Walker - `PeceWalkerGameMode` (`.../PeceWalkerGameMode.mm`)

- **Name:** `_PEACE_WALKER`: "Peace Walker" / "Доброволец" / "Marcher paisiblement" / "Fridlich Geher" / "Pacíficamente  paseante".
- **Objective / endless:** the player **cannot shoot** (`canShoot = NO`, `:39,64`). Reach each green flag before a
  10-"second" countdown runs out (`TOTAL_GAMEMODE_TIME_CONST 10`, `:25`). Each flag resets the countdown. Enemies
  (Tentacles) accumulate.
- **Lose:** the countdown hits 0 (UI: "Time Is Off\nScore: N", then Restart / Go To Menu, `LevelOverLayer.mm:556-570`),
  or the player dies (`:74-77`).
- **Countdown** (`realTimeGameTick`, `:67-82`, called every frame): `counter += 0.01 * timeScale`. At 1.0 it does
  `totalGameModeTime--` and refreshes the HUD (`updateTickInGameMode`, "0 : 10" format). One countdown "second" = 100
  frames = 1.67 s real at 60 fps (longer in slow motion).
- **Start:** 2 Tentacles (`FIRST_ENEMIE_COUNT 2`, `:21,89-94`).
- **Flag cycle** (`update`, `:135-166`): while `canGenerateBonus`, `bonusGeneratorTick += 0.01 * timeScale` per frame. At
  `NEXT_FLAG_GENERATOR_CONST 1.0` (`:24`) the mode:
  - spawns a `PeaceFlag` 500-520 pt from the screen centre;
  - for every flag except the first, spawns **5 vitamins worth 1** within ±80 pt of the screen centre (`:125-133`);
  - spawns +1 Tentacle (`ENEMIES_CREATE_CONST 1`, every flag);
  - spawns a life bonus every 8th flag (`LIFE_BONUS_GENERATOR_CONST 8`, `BonusManager generateLifeBonus`).
- **Pickup:** +100 points (`ContactListener.mm:134`). `gameModeSpecialAction` (`:110-116`) re-enables generation and
  resets the countdown to 10.
- **Difference:** this is the only mode without shooting, the only one with a per-objective timer, and the only one that
  spawns vitamins directly. It uses `kTentacle` (17) only.

## 8. Armagedon - `ArmagedonGameMode` (`.../ArmagedonGameMode.mm`) - dead

- `Run` spawns `ARMAGEDON_BACTERIA_START_COUNT 1` `kArmagedonBacteria` (`:17,29-31`). The case is **commented out** in
  `AI/EnemyAgentsFactory.mm:94-95`, so the factory returns `nil` and nil is pushed into the agent list.
- It has no menu entry (section 1) and no player-facing localisation (the menu label is the hard-coded "Armagedon").
- **Enum QUIRK:** the comment says `kArmagedonBacteria //22`, but its real value is **27** (`ConfigConstants.h:255`).
- Drop it from the rewrite, or redesign it.

### Mode comparison

| | Campaign | Capture The Flag | Rush | Peace Walker |
|---|---|---|---|---|
| Endless | no (50 levels) | yes | yes | yes |
| Shooting | yes | yes | yes | **no** |
| Timer shown | 80 s per level | no | no | 10 s per flag |
| Spawns driven by | level plist + NSTimer (s) | per-frame generator | per-frame generator | 1 Tentacle per flag |
| Points from | kills (`killScore`) | flags +100 | zones +250 | flags +100 |
| Points saved | yes (per level + total) | no | no | no |
| Difficulty effect | spawn interval −0.5/−1.0 s; score ×1/×2/×3; boss stats | score ×(d+1) | score ×(d+1) | score ×(d+1) |

---

## 9. Level data and LevelManager

### 9.1 Files
- `Resources/Levels/Boss_1..5/level_1..50.plist`. They are loaded by **file name** `level_<levelCounter>` from the bundle
  root (`LevelManager.mm:55-65`). The `Boss_X` folders are Xcode groups and get flattened.
- Converted copy: `/home/claude/out/data/levels/levels_all.json`. I re-checked it against all 50 source plists
  (every field and enemy list): **0 mismatches**. No plist has extra keys. Some numbers are stored as strings in the
  plist (`enemiesgrowinterval`, `maximumenemies`, some `enemies` arrays). The code reads them with `intValue`/`floatValue`, so
  both forms work.

### 9.2 Field meanings (traced through the code)

| Field | Parsed (`Level.mm`) | Used | Meaning |
|---|---|---|---|
| `number` | `intValue` `:38` | boss test `number % 10 == 0` (`CompaignGameMode.mm:60,204`; `LevelOverLayer.mm:160,400`); final-level test `== 50` (`GameManager.mm:33,224`); pre-boss banner `number % 10 == 9` (`LevelOverLayer.mm:589`) | Level ID for the boss/last-level logic. **Not** used to find the file. |
| `time` | `floatValue` `:39`, cast to int `LevelManager.mm:74` | campaign clock (`CompaignGameMode.mm:116-117`); also excludes the bonus on tick == `time` (`:123-126`) | Survival time in seconds (1 Hz NSTimer). The win comes on the tick that sends 0, about `time + 1` s. Ignored on boss levels. |
| `enemiescreatetime` | `floatValue` `:40`; Hard −0.5, Impossible −1.0 (`:45-57`) | `NSTimer` interval for `createRandomEnemy` (`CompaignGameMode.mm:57,75,92`) | Seconds between waves (real time). On boss levels it is the delay before the boss appears (one shot). **No lower clamp** (e.g. level 41 on Impossible = 0.3 s). |
| `enemies` | array retained `:41` | `getNextRandomEnemyType`: uniform random index (`LevelManager.mm:124-130`) | Pool of `EnemyAgentType` values. Duplicates add weight. |
| `maximumenemies` | `intValue` `:42` | cap for `totalEnemiesPerCreation` (`LevelManager.mm:164-166`) | Maximum enemies per wave. |
| `enemiesgrowinterval` | `intValue` into a float `:43` | `LevelManager update` (`:148-174`) | Every this many **units** (0.01 per frame; 0.005 per frame in bullet time), enemies-per-wave goes up by 1 (from 1, capped at `maximumenemies`). **0 = never grows.** 1 unit = 1.67 s at 60 fps. |
| `score`, `unlocked` (Level props) | not in the plist | `unlocked` set YES but never read; `score` unused | Leftovers. The old per-level `levelisunlocked<N>` defaults code is commented out (`Level.mm:64-80`). |

`totalEnemiesPerCreation` restarts at 1 on every load or restart (`LevelManager.mm:71,136`).
**QUIRK:** the growth clock is frame-based while waves are timed in real seconds, so wave size at a given moment depends
on frame rate. The update guard `gameIsPaused == NO || levelHasWon == NO` (`:151-152`) should be `&&`; it is harmless
because `update` isn't called while paused.

### 9.3 All 50 levels

Enemy names come from `EnemyAgentType` (`ConfigConstants.h:225-258`). The fraction is the share of the pool. Difficulty
columns show the spawn interval on Hard / Impossible.

| file | world-lvl | number | time | interval s | Hard / Imp. | grow / max | enemy pool |
|---|---|---|---|---|---|---|---|
| 1 | 1-1 | 1 | 80 | 2 | 1.5 / 1 | 10 / 3 | BlueCorosia(0) |
| 2 | 1-2 | 2 | 80 | 1.8 | 1.3 / 0.8 | 10 / 3 | BlueCorosia(0) 1/3, Loctopus(1) 2/3 |
| 3 | 1-3 | 3 | 80 | 1.8 | 1.3 / 0.8 | 10 / 3 | BlueCorosia(0) 1/3, Loctopus(1) 2/3 |
| 4 | 1-4 | 4 | 80 | 1.8 | 1.3 / 0.8 | 10 / 4 | BlueCorosia(0), Loctopus(1), RedNoise(2) 1/3 each |
| 5 | 1-5 | 5 | 80 | 1.8 | 1.3 / 0.8 | 10 / 4 | BlueCorosia(0), Loctopus(1), RedNoise(2) 1/3 each |
| 6 | 1-6 | 6 | 80 | 2.2 | 1.7 / 1.2 | 10 / 3 | BlueCorosia(0), Loctopus(1), RedNoise(2), SpiralCorosia(3) 1/4 each |
| 7 | 1-7 | 7 | 80 | 2.2 | 1.7 / 1.2 | 10 / 3 | BlueCorosia(0) 1/4, Loctopus(1) 1/4, RedNoise(2) 2/4 |
| 8 | 1-8 | 8 | 80 | 2.1 | 1.6 / 1.1 | 10 / 3 | BlueCorosia(0) 1/4, Loctopus(1) 1/4, RedNoise(2) 2/4 |
| 9 | 1-9 | 9 | 80 | 2 | 1.5 / 1 | 10 / 3 | BlueCorosia(0), Loctopus(1), RedNoise(2), SpiralCorosia(3) 1/4 each |
| 10 | 1-10 BOSS | 10 | 80 | 2 | 1.5 / 1 | 1 / 1 | BigBoss(22) |
| 11 | 2-1 | 11 | 80 | 1.9 | 1.4 / 0.9 | 8 / 4 | BlueCorosia(0), Loctopus(1), RedNoise(2), BigHunterus(5) 1/4 each |
| 12 | 2-2 | 12 | 80 | 1.9 | 1.4 / 0.9 | 8 / 4 | same as 11 |
| 13 | 2-3 | 13 | 80 | 1.8 | 1.3 / 0.8 | 10 / 4 | BigBlueCorosia(6), BlueCorosia(0), Loctopus(1), RedNoise(2) 1/4 each |
| 14 | 2-4 | 14 | 80 | 1.8 | 1.3 / 0.8 | 10 / 4 | same as 13 |
| 15 | 2-5 | 15 | 80 | 1.8 | 1.3 / 0.8 | 10 / 3 | BigBlueCorosia(6) 2/5, BlueCorosia(0) 1/5, Loctopus(1) 1/5, RedNoise(2) 1/5 |
| 16 | 2-6 | 16 | 80 | 1.8 | 1.3 / 0.8 | 10 / 3 | same as 15 |
| 17 | 2-7 | 17 | 80 | 2 | 1.5 / 1 | 12 / 4 | BigHunterus(5), BlueCorosia(0), Loctopus(1), SpiralCorosia(3) 1/4 each |
| 18 | 2-8 | 18 | 80 | 2 | 1.5 / 1 | 12 / 4 | same as 17 |
| 19 | 2-9 | 19 | 80 | 2 | 1.5 / 1 | 12 / 4 | same as 17 |
| 20 | 2-10 BOSS | 20 | 80 | 2 | 1.5 / 1 | 1 / 2 | Inferno(23) |
| 21 | 3-1 | 21 | 80 | 2 | 1.5 / 1 | 30 / 2 | Chorux(11) |
| 22 | 3-2 | 22 | 80 | 2 | 1.5 / 1 | 30 / 2 | Chorux(11) |
| 23 | 3-3 | 23 | 80 | 2.0 | 1.5 / 1 | 10 / 4 | Chorux(11), BlueCorosia(0), Loctopus(1) 1/3 each |
| 24 | 3-4 | 24 | 80 | 2.0 | 1.5 / 1 | 10 / 4 | same as 23 |
| 25 | 3-5 | 25 | 80 | 2 | 1.5 / 1 | 10 / 3 | Adrenalium(9), RedNoise(2), ShootingCorosia(7) 1/3 each |
| 26 | 3-6 | 26 | 80 | 2 | 1.5 / 1 | 10 / 3 | same as 25 |
| 27 | 3-7 | 27 | 80 | 1.4 | 0.9 / 0.4 | 5 / 8 | RedNoise(2) |
| 28 | 3-8 | 28 | 80 | 1.4 | 0.9 / 0.4 | 5 / 8 | RedNoise(2) |
| 29 | 3-9 | 29 | 80 | 1.4 | 0.9 / 0.4 | 5 / 8 | RedNoise(2) 1/2, BlueCorosia(0) 1/2 |
| 30 | 3-10 BOSS | 30 | 80 | 2 | 1.5 / 1 | 0 / 1 | BigRedNoise(24) |
| 31 | 4-1 | 31 | 80 | 5 | 4.5 / 4 | 12 / 2 | Serpentus(14) |
| 32 | 4-2 | 32 | 80 | 5 | 4.5 / 4 | 12 / 2 | Serpentus(14) |
| 33 | 4-3 | 33 | 80 | 4 | 3.5 / 3 | 12 / 2 | LotusBacterius(15) |
| 34 | 4-4 | 34 | 80 | 4 | 3.5 / 3 | 12 / 2 | LotusBacterius(15) |
| 35 | 4-5 | 35 | 80 | 3 | 2.5 / 2 | 10 / 5 | Zigzagus(18) 1/2, LotusBacterius(15) 1/2 |
| 36 | 4-6 | 36 | 80 | 3 | 2.5 / 2 | 10 / 5 | same as 35 |
| 37 | 4-7 | 37 | 80 | 4 | 3.5 / 3 | 12 / 2 | ShootingSerpentus(19) |
| 38 | 4-8 | 38 | 80 | 4 | 3.5 / 3 | 12 / 2 | ShootingSerpentus(19) |
| 39 | 4-9 | 39 | 80 | 3.9 | 3.4 / 2.9 | 12 / 2 | ShootingSerpentus(19) |
| 40 | 4-10 BOSS | 40 | 80 | 2 | 1.5 / 1 | 0 / 1 | Anakonda(25) |
| 41 | 5-1 | 41 | 80 | 1.3 | 0.8 / 0.3 | 10 / 4 | Gnida(20), SpiralCorosia(3), BigHunterus(5) 1/3 each |
| 42 | 5-2 | 42 | 80 | 1.3 | 0.8 / 0.3 | 10 / 4 | same as 41 |
| 43 | 5-3 | 43 | 80 | 2 | 1.5 / 1 | 8 / 3 | Gnida(20), Adrenalium(9), LotusBacterius(15) 1/3 each |
| 44 | 5-4 | 44 | 80 | 2 | 1.5 / 1 | 8 / 3 | same as 43 |
| 45 | 5-5 | 45 | 80 | 1.5 | 1 / 0.5 | 5 / 4 | Gnida(20) 1/2, ZamriGnida(21) 1/2 |
| 46 | 5-6 | 46 | 80 | 1.5 | 1 / 0.5 | 5 / 4 | same as 45 |
| 47 | 5-7 | 47 | 80 | 1.5 | 1 / 0.5 | 5 / 4 | Gnida(20), ZamriGnida(21), BigBlueCorosia(6), RedNoise(2) 1/4 each |
| 48 | 5-8 | 48 | 80 | 1.5 | 1 / 0.5 | 5 / 4 | same as 47 |
| 49 | 5-9 | **48** | 80 | 1.4 | 0.9 / 0.4 | 5 / 4 | same as 47 |
| 50 | 5-10 BOSS | 50 | 80 | 2 | 1.5 / 1 | 0 / 1 | Frunctus(26) |

Enemy types never placed directly by a level: Gridium(4), Sunux(8), Caterpillar(10), ChoruxDefender(12), Spawner(13),
LotusBacteriusBody(16), Tentacle(17, Peace Walker only), ArmagedonBacteria(27). Sunux, Caterpillar and Spawner do
appear in Rush/CTF.

### 9.4 Data anomalies
1. **`level_49.plist` has `number = 48`** (it should be 49). The file loads by name, so it does load. Effects:
   - Boss test, final-level test and save keys are unaffected. Scores and unlocks use `levelCounter`, not `number`
     (`ScoreManager.mm:86`, `LevelManager.mm:84`).
   - **It does matter for the pre-boss banner:** `LevelOverLayer getReady` checks `number % 10 == 9` on the level just won
     (`LevelOverLayer.mm:589`). After beating level 49, 48 % 10 = 8, so the game shows "Level 50" instead of
     "Get Ready For the Boss". It also leaves the score counter, timer and ability bar visible during the Frunctus fight;
     every other boss hides them. This is cosmetic. Fix it in data (49).
2. **Boss level 20 (Inferno): grow 1 / max 2.** The one-shot spawn timer spawns `totalEnemiesPerCreation` bosses. That
   value becomes 2 after 1 unit (100 frames ≈ 1.67 s at 60 fps).
   - Advancing from level 19 on Normal, the timer fires at 2.0 s, so **2 Infernos** appear.
   - On Hard (1.5 s) or Impossible (1.0 s), **1 Inferno** appears.
   - Entering from level select, the delay is ≥ 6 s, so 2 Infernos.
   - The count also depends on frame rate. The win check handles 2 bosses (it wins when the last one dies). The
     designer's intent was probably **2 Infernos**. Make it explicit in the rewrite.
3. Boss levels 10/30/40/50 always produce exactly 1 boss (max 1 or grow 0). Their `time` and grow values are otherwise
   unused.
4. `time` is 80 everywhere. The HUD default label is hard-coded "1 : 20" (`LevelOverLayer.mm:130,658`).
5. `enemiescreatetime` minus the difficulty offset has no floor (level 41/42 Impossible = 0.3 s).

### 9.5 LevelManager behaviour (`Managers/LevelManager.mm`)
- `levelCounter` = the **next** level to load. Init sets it to 1 and unlocks level 1 (`:40-53`).
- Level select sets `levelCounter = tag` (`Layers/ChooseLevelLayer.mm:261`).
- `loadNextLevel` (`:67-78`): resets the per-wave count, parses `level_<levelCounter>`, sets `totalLevelTime = time`,
  **unlocks the level being loaded** (`saveUnlockLevel` uses the pre-increment counter, `:80-92`), then `levelCounter++`.
- `restartLevelManager` (`:132-139`): `levelCounter--`, then reload. `resetLevelManager` (`:141-146`): `levelCounter = 1`
  (called when the mode is released).
- **QUIRK:** `Level` objects are allocated per load and never released (`:72`), a leak.

### 9.6 Progression rules
- **Structure:** 5 worlds ("Boss_1..5") × 10 levels. Levels 10, 20, 30, 40, 50 are boss fights.
  The level-select screen is a 5×10 grid of `vitamin.png` icons (`ChooseLevelLayer.mm:74-123`):
  - boss icons tinted `(0,128,255)`;
  - locked icons at opacity 128, tinted red, with no tap target;
  - the difficulty picker (Normal / Hard / Impossible, `BuyButton.png`) is on the same screen and saves immediately
    (`:130-204`).
- **Unlock:** a level unlocks when it is **loaded**, so it is reachable either from level select or by auto-advancing after
  winning the previous one. **QUIRK:** if the player quits during the 5 s "You Won" window, the next level stays locked.
  The rewrite should unlock N+1 at the moment N is won.
- **Advance:** automatic, 5 s after the win. After level 50 the credits (`AboutLayer`) appear and the session resets.
- **Stars / grades:** **none.** The per-level best score and the campaign total are saved but **never displayed**
  (`showHighScore` is empty, `LevelOverLayer.mm:464`). No other completion state is saved.
- **Difficulty** (Normal=0, Hard=1, Impossible=2): campaign spawn interval (above), score multiplier ×(d+1)
  (`GameManager.mm:621-622`), and boss stats (switches in `AI/BigBoss.mm:31`, `AI/Inferno.mm:54`, `AI/BigRedNoise.mm:50`,
  `AI/Anakonda.mm:70`, `AI/Frunctus.mm:48`; see the AI doc).

**Unity mapping.**
- `LevelDefinition` ScriptableObjects (one per level, imported from `levels_all.json`), with world and boss flags explicit
  instead of `number % 10`.
- A `WaveSpawner` driven by scaled game time (`Time.deltaTime`), never frames. Convert grow-interval units to seconds by
  choosing the 60 fps value (× 1.667) or retuning.
- Unlocks and bests in the save file (section 13).

## 10. SpawnManager (`Managers/SpawnManager/SpawnManager.mm`)

Arena bounds come from `CoreSettings upperLeft/upperRight/bottomLeft/bottomRight`: the background sprite `bg.jpg` bounds
inset by `ENEMY_CREATE_BORDER_DELTA 20` pt (`GameWorld/GameWorld.mm:62-89`, `ConfigConstants.h:427`). The image is not in
the tree. The Gridium constants imply about −555..1576 × −375..1124 (`ConfigConstants.h:415-425`).
`Utils getRandomNumber:from to:` is an **inclusive integer** random (`Utils/Utils.mm:36-39`).

| Method | Behaviour | Used by |
|---|---|---|
| `getValidOuterSpawn` `:73-106` | picks one of 4 arena edges uniformly, then a random integer position along it | **all standard enemy spawns** (`AgentsManager createRandomAgentAtPosition`; the position argument is ignored), Inferno, Anakonda |
| `getValidSpawnPosFromCharacter` `:192-226` | random angle; x and y radii each drawn independently from 500-600 (`ENEMY_CIRCLE_SPAWN_RADIUS(_2)`, `ConfigConstants.h:406,409`) around the player; if outside the arena, reflected inward by 2×(500..600) | Frunctus |
| `getCircleCoordSpawnWithAlpha:` `:228-259` | same with a given angle | AgentsManager crowd helpers |
| `getRandomPointAroundPoint:` `:327-352` | random angle, radii 500-520 (`ENEMY_ARROUND_RANDOM_POINT_RADIUS(2)`), clamped 30 pt (`BOUNDER_ESCAPE_DELTA`) inside the arena | Rush stations, peace flags, many AI wander targets |
| `generateValidBonusSpawn` `:261-300` | radii 700-710 (`BONUS_SPAWN_RADIUS(_2)`) around the player, clamped 30 pt inside | flags, usable bonuses, life bonus |
| `getGridiumPoints:gridStartSpawn:` `:108-190` | N evenly spaced start/end pairs across one side, with rotation 180/−90/0/90 | Gridium crowds |
| `createRandomAgentAtRandomPos` `:42-70`, `getFrunctusValidEvadePosition` `:302-325` | **dead** (no callers) | - |

- **QUIRK:** `generateValidBonusSpawn` compares the player position with the *offset vector* (not the offset length)
  against 350 pt and recurses on failure (`:276-279`). Because the 700 pt offset is usually clamped, bonuses and flags tend
  to sit on arena edges or corners.
- **QUIRK:** independent x/y radii make ellipse-ish rings, not circles.
- **QUIRK:** `getFrunctusValidEvadePosition` computes a value and then returns `ccp(arc4random()%300, arc4random()%700)` (`:324`).

**Unity mapping.** A static `SpawnUtility` with an arena `Rect`, edge sampling and annulus sampling using
`Random.insideUnitCircle`, with a proper minimum-distance check.

## 11. GameManager (`Managers/GameManager.mm`) - game flow

States/flags: `gameIsStarted`, `gameIsPaused`, `gameIsPreparing`, `characterIsDead`, `levelHasWon`, `gameIsFinished`,
`bulletinTimeIsOn`.

- **Start** (`initGameManager` `:87-106` → `startGame` `:108-124`): runs the input listener, creates the world (`GameWorld Run`
  → mode `Run`), the character and the camera. `gameIsPaused = NO`. The HUD shows "Level N" for 2 s, then "Get Ready" for
  1 s, then clears `gameIsPreparing` (`LevelOverLayer.mm:387-462`). The simulation already runs during these banners.
- **Main loop** (`managePhysics` `:418-487`, every frame when not paused):
  - Box2D `Step(dt, 8 velocity, 2 position iterations)`. `dt` is the cocos2d frame delta, already multiplied by the
    scheduler `timeScale`, so the physics step is variable.
  - Iterates bodies: detects player death, updates the camera, calls `update` on enemies (while alive and started), then
    character controller, `GameWorld update` (→ mode) and input polling.
- **Pause** (`:580-599`): only when not paused, not won, not preparing and alive. It pauses the CCDirector and shows
  Resume / Restart / (Choose Level, campaign only) / Go To Menu (`LevelOverLayer.mm:466-505`).
- **Death** (`:438-449`): one-time `characterIsDead`. After 0.1 s, `showGameOver` (`:489-498`) disables the special
  ability, calls the mode's death handler and shows the Game Over UI.
- **Level won** (normal) `gameLevelHasWon` (`:204-217`) and boss `levelHasWonAfterBigBoss` (`:219-239`): see section 3.
  `loadNextLevelInTime` (`:241-286`) resets time scale and blur, loads the next level, resets the HUD, re-enables the
  special ability and calls `updateCharacterSettingsAfterLevel`. **Player HP/hearts are not reset** by this path; check the
  character doc.
- **Restart** `simulateGameRestart` (`:288-315`): resets time scale, moves the character to `(240,160)`, restarts the mode,
  resets the HUD and the score accumulator.
- **Menus:** `goToMenu` (`:601-608`) and `goToLevelSelect` (`:610-617`) call `resetGameData` (`:147-180`, tears everything
  down), then a 0.1 s fade to `MainMenuScene` / `ChooseLevelLayer`.
- **Bullet time** `goBulletinTine` (`:327-358`, called from the character's slow-motion ability, `Characters/MainCharacter.mm:372,595,891`):
  - toggles the scheduler `timeScale` between 1.0 and `BULLETIN_TIME_DELTA 0.35` (`:32`);
  - character sprite-sheet blur 2.0 / 1.0;
  - while alive, it refreshes the character, the mode (`refreshGameMode`) and the slow-motion blend
    (`GameResourceManager updateWithSlowMotion`).
- **Score** `updateLScoreWithValue:` (`:619-624`): the HUD adds `score × (difficulty + 1)`, but
  `ScoreManager saveScoreForCurrentLevel:` gets the **unmultiplied** score. **QUIRK:** the saved and displayed scores differ.
- Clock display `updateTickInGame:` (`:182-191`, campaign, wins at 0) and `updateTickInGameMode:` (`:193-202`, Peace
  Walker, "Time Is Off" at 0).
- **Dead code:** `upgradeScore`/`currentScore` (`:631-637`, no callers), `createBossLife`, `characterHit`,
  `decideCharacterAction`, `moveCharacterWithVelocity`, `rotateCharacterWithAngle`, `showPauseMenu`, `reEnableAccelerometer`.

**Unity mapping.** A `GameSession` state machine (Preparing → Playing → Paused / Won / Dead / Finished) driven by
events; menus as UI Toolkit screens; slow motion through `Time.timeScale = 0.35` with `fixedDeltaTime` scaled to match.

## 12. Score and currency

### 12.1 Points
| Source | Points (before multiplier) | Modes | Ref |
|---|---|---|---|
| Enemy kill by player bullet | the enemy's `killScore` (e.g. BlueCorosia 50, Loctopus 80, RedNoise 80, SpiralCorosia 90, BigBlueCorosia 100, BigHunterus 120, ShootingCorosia 130, Adrenalium 150, Sunux 150, Spawner 160, Zigzagus 160, Chorux 200, Gnida 200, ZamriGnida 250, Caterpillar 300, Serpentus 400, LotusBacterius 400, ShootingSerpentus 500, ArmagedonBacteria 50) | **Campaign only** | `ContactListener.mm:70,84`; values in `AI/<Enemy>.mm` `killScore =` lines (e.g. `AI/BlueCorosia.mm:35`, `AI/ShootingSerpentus.mm:32`) |
| Flag / peace flag | 100 (`FLAG_CONST_POINT`) | CTF, Peace Walker | `ConfigConstants.h:440` |
| Rush zone | 250 (`RUSH_CONS_POINT`) | Rush | `ConfigConstants.h:441`, `RushStation.mm:124` |

Displayed points = base × (difficulty+1) (`GameManager.mm:621-622`). The HUD resets to 0 at each level win
(`LevelOverLayer.mm:572-576`).

### 12.2 ScoreManager (`Managers/ScoreManager/ScoreManager.mm`)
- `saveScoreForCurrentLevel:` (`:75-78`) adds to `currentLevelScore` in every mode.
- `synchronizeLevelScore` (`:80-103`) runs only from `gameLevelHasWon`, i.e. **when a normal campaign level is won**:
  - level = `levelCounter - 1`;
  - if `currentLevelScore` > the saved `level_<N>_score`, it saves the new best and adds the improvement to
    `compaign_high_score`;
  - then resets the accumulator.
- **QUIRK:** boss wins never call it, so boss-kill points are never saved and **carry over** into the next level's
  accumulator (nothing resets it on that path).
- **QUIRK:** Rush/CTF/Peace Walker scores are never saved.
- `saveScore:forLevel:` (`:44-57`) uses a different key, `level_score_%d`, and is **dead**. `LEVEL_SCORE_KEY` is unused.
- Game Center: `authenticateLocalPlayer` and `showGameCenterOnWindow:` exist; the calls in `AppDelegate.mm:75-80` are
  commented out and scores are never reported. Leaderboard IDs: `com.AntiBacteria.CompaignMode`,
  `.CaptureTheFlag`, `.PeaceWalker`, `.Rush` (`ConfigConstants.h:27-30`).

### 12.3 Vitamins (the currency)
- **Earned:** enemies drop vitamins on death (`vitaminsCount` per enemy class, e.g. BlueCorosia 2, Frunctus 10; each
  worth 1, Anakonda drops one worth 10; see the AI doc), and Peace Walker gives 5 per flag.
- **QUIRK (big):** the balance is credited when a vitamin is **created**, not when it is collected
  (`AI/VitaminManager.mm:75-85`). Pickup only refreshes the HUD (`ContactListener.mm:177-189`). Uncollected vitamins
  still count.
- **Saved:** `VitaminManager synchronizeVitaminsCount` (`AI/VitaminManager.mm:87-91`) runs in `releaseVitaminManager`
  (`:114-126`), on every mode restart, release, or quit to menu.
- **QUIRK (bug, inferred from code):** `VitaminManager` is a singleton that reads the balance from settings only once,
  in `init` (`:40`). Vitamins spent in the upgrade screen (or bought in the store) after the first game are overwritten by
  the stale in-memory value the next time a game ends, so spending is refunded and purchases are lost.
- **Spent:** `Layers/CharacterUpgradeLayer.mm:126-132,289-475`, one-time unlocks:

  | Item | Cost (vitamins) | Bought-flag key |
  |---|---|---|
  | Water shoot | 20 000 | `watershootisboughtkey` |
  | Windmill shoot | 20 000 | `windmillshootisboughtkey` |
  | Circle shoot (=`kExplosionShoot`) | 20 000 | `circleshootisboughtkey` |
  | Tornado shoot | 20 000 | `tornadoshootisboughtkey` |
  | Double speed (toggle once owned) | 40 000 | `doublespeedisboughtkey` |
  | Immortality (toggle once owned) | 1 000 000 | `immortalityisboughtkey` |

  If the player can't afford an item, an alert shows `_NO_VITAMINS`.
- **Bought** with real money (`Layers/StoreLayer.mm:100-132,254-299`): `com.antiBacteria.vitamins.1..4` = 10 000 / 30 000
  / 100 000 / 1 000 000 vitamins for $0.99 / 1.99 / 3.99 / 9.99. **QUIRK (bug):** a purchase **sets** the balance to the
  pack size instead of adding it (`StoreLayer.mm:279`).

## 13. Save data (NSUserDefaults)

App domain = the bundle identifier (from `Resources/Info.plist`). Three storage styles are used:

**A. MPSecureUserDefaults (signed, not encrypted)** (`Utils/NSUserDefaults+MPSecureUserDefaults.m`)
- The secret is set at launch: `[NSUserDefaults setSecret:@"ms3_ser_202$"]` (`AppDelegate.mm:32`).
  `setDeviceIdentifier` is never called, so values are not bound to a machine.
- Write (`:183-197`): stores the dictionary `{ "MPSecureUserDefaultsValue": value, "MPSecureUserDefaultsHash": hex(MD5(NSKeyedArchiver(value) + secretUTF8)) }`
  under the key (`:22-23,279-318`).
- Read (`:244-277`): returns nil (and sets `valid=NO`) if the hash doesn't match. A missing key gives `valid=YES`, nil.

**B. AES blobs** (`Managers/InAppManager/NSData+AES256.m`)
- `CCCrypt(kCCAlgorithmAES128, kCCOptionPKCS7Padding)` with a 32-byte key buffer, so AES-256. No ECB flag and a NULL IV,
  so **CBC with a zero IV** (`:28-64,66-103`).
- **The key is the Mac's hardware `IOPlatformUUID`** (`:14-26`), copied with `getCString:maxLength:33`. The UUID is 36
  chars, so the copy may fail; whether the effective key is its first 32 bytes or all zeros is **unverified (inferred)**.
- Either way, **saves can't be moved between machines**. Most payloads are `NSKeyedArchiver(NSNumber)`; the bought flags
  are raw UTF-8 strings.

**C. Plain.**

### Complete key list

| Key (literal) | Style | Content / default | Writer / reader |
|---|---|---|---|
| `isfirsttime` | plain NSNumber | 0/absent → first-run defaults written, then 1 | `GameSettingsManager.mm:15,65,99` |
| `soundenabled` | AES(archived BOOL) | YES. Music toggle; **no effect** (no audio) | `GameSettingsManager.mm:17` |
| `sfxenabled` | AES(archived BOOL) | YES. Gates `SoundManager playSoundEffectAtString` (never called) | `:18` |
| `gamemode` | AES(archived int) | **difficulty**, 0=Normal (misleading key name) | `:16` |
| `openedgamemodes` | AES(archived int) | 1; never read | `:19` |
| `controltype` | AES(archived int) | 0 = `kKeyBoardAndMouse` (1 keyboard only, 2 mouse only) | `:20` |
| `isautofireEnabled` | AES(archived BOOL) | NO | `:21` |
| `vitamins` | AES(archived int) | 0. Currency balance | `:22` |
| `currentlocalization` | AES(archived int) | 0 = English (absent → English, written) | `LocalizationManager.mm:12,64-83` |
| `shoottype` | AES(archived int) | selected `ShootType` 0..4 (absent → decrypt nil → 0 Normal) | `Characters/CharacterPropertiesManager.mm:13,43,67` |
| `immortality` | AES(archived BOOL) | immortality toggle | `CharacterPropertiesManager.mm:14` |
| `doublespeed` | AES(archived BOOL) | double-speed toggle | `CharacterPropertiesManager.mm:15` |
| `watershootisboughtkey`, `windmillshootisboughtkey`, `circleshootisboughtkey`, `tornadoshootisboughtkey`, `doublespeedisboughtkey`, `immortalityisboughtkey` | AES(raw UTF-8 string) | owned when it decrypts to `watershootisboughtvalue` etc. | `ConfigConstants.h:443-455`, `CharacterUpgradeLayer.mm:78-93,333-334` |
| `""` (empty key) | AES | **QUIRK:** selecting "Normal shoot" writes an encrypted "" under the empty key (index 0 goes through the buy path at cost 0) | `CharacterUpgradeLayer.mm:302-335` |
| HMAC-SHA256 hex of `"levelisunlocked<N>"` (HMAC key = the same string) | Secure | value = HMAC-SHA256 hex of `"YES"` (key "YES"); level N unlocked if a valid value exists | `LevelManager.mm:14,80-122`, `Utils/Utils.mm:49-66` |
| `level_<N>_score` | Secure NSNumber | best raw (unmultiplied) campaign score for level N | `ScoreManager.mm:92-93,131` |
| `compaign_high_score` | Secure NSNumber | sum of per-level bests | `ScoreManager.mm:14,88,96` |
| `level_score_<N>` | Secure | dead (never written) | `ScoreManager.mm:44-57` |
| `levelisunlocked<N>` (plain) | - | dead, commented out | `Level.mm:64-80` |

- Nothing else is persisted: no per-mode high scores, no settings for volume or resolution, no hearts or upgrades
  between runs beyond the above.
- `PlistAction` (`Managers/FileManager/PlistAction.mm`) could write arrays to `~/Documents`, but it is **never called**.
- **QUIRK:** `GameSettingsManager` is a singleton loaded once at first use (`:45-57,59-151`). Every save rewrites all
  fields (`:153-186`).

**Unity mapping / recommendation.**
- One versioned JSON save (`SaveData { version, settings{difficulty, music, sfx, controlType, autoFire, language}, wallet{vitamins}, upgrades{owned[], selectedShoot, doubleSpeedOn, immortalOn}, campaign{unlockedMax, bestScore[50], total}, modes{bestCTF, bestRush, bestPeaceWalker} }`).
- Write it through a platform abstraction: Steam Cloud / `Application.persistentDataPath` on PC, the console save APIs
  elsewhere. Optionally add an HMAC for casual tamper resistance.
- Do not try to import the legacy Mac saves; they are machine-keyed.

## 14. GameResourceManager (`Managers/GameResourceManager/GameResourceManager.mm`)

- At first use, loads the sprite-frame atlases `characters_resources.plist`, `bonuses_spritesheet.plist` and
  `enemy_direction.plist` (`:34-39`; converted frame lists in `/home/claude/out/data/raw/*.json`).
- Lazily creates 3 `CCSpriteBatchNodeBlur` batch nodes:
  - `characters_resources.png` (player and enemies);
  - `bonuses_spritesheet.png` ("vitamins sheet": vitamins, bonuses, spawn markers);
  - `enemy_direction.png` (off-screen enemy arrows) (`:75-107`).
  - `GameWorld` adds them to the game layer at z 1 (`GameWorld/GameWorld.mm:48-50`).
- `updateWithSlowMotion:` (`:52-73`): YES sets blend `(GL_DST_ALPHA, GL_DST_ALPHA)` on the character and vitamin sheets and
  enables blending (a glow/ghost look during bullet time); NO disables it. Every mode calls it with NO on
  start, restart and death.
- **QUIRK:** `originalBlendFunc` is read from a nil node and never used.

**Unity mapping.** Sprite Atlases; a URP post-process or material swap (additive or "ghost") during slow motion,
plus the motion-blur effect handled by the rendering doc.

## 15. Audio - SoundManager (`Managers/SoundManager/SoundManager.mm`)

- **The shipped game plays no sound at all.** There are zero call sites for `playBackgroundMusicAtString`,
  `playSoundEffectAtString`, `playEffect` or `playBackgroundMusic` outside SoundManager. SoundManager itself is never
  referenced: it is only imported by `ContactListener.mm:13`, `MainCharacter.mm:10` and `GameSettingsManager.mm:11`.
- **No audio files** exist in the tree (`find` for mp3/wav/caf/aif/m4a/ogg returns nothing). Image files are also absent, so
  that alone isn't conclusive, but the code confirms it.
- API (dead):
  - `playSoundEffectAtString:` plays an effect only if `sfxIsEnabled` (`:43-49`);
  - `playBackgroundMusicAtString:WithLoop:` ignores `loop` and always loops (`:57-60`);
  - `setBackgroundMusicVolume:` (`:62-68`);
  - `setEffectsVolume:` sets the volume on an `underWaterThemePlayer` that is never created (`:51-55`); this is left over
    from an older project ("PivotPoint" header);
  - `playUnderwaterThemeMusicAtString:` is empty.
- The Settings screen still has working Music and SFX toggles that are saved (`Layers/SettingsLayer.mm:354-368`).
- **Rewrite:** audio has to be designed from scratch (music per world/mode, SFX for shot, hit, death, pickup, flag,
  zone fill, boss). Keep the two saved toggles; add volume sliders. Unity: an AudioMixer with Music and SFX groups.

## 16. Localisation - LocalizationManager (`Managers/LocalizationManager/LocalizationManager.mm`)

- **Languages (5):** index 0 English, 1 Russian, 2 French, 3 German ("Deutsche"), 4 Spanish (`Localizations` enum,
  `ConfigConstants.h:461-469`). Converted table: `/home/claude/out/data/raw/localizations.json` (5 dicts × 53 keys).
- **Lookup:** `localizations.plist` is an **array of dictionaries** indexed by the language enum. `getValueWithKey:`
  returns `localizedData[currentLocalization][key]` (`:55-62`), or nil if the key is missing.
- Keys are `#define`d string constants `_LANGUAGE` … `_LIFE` (`ConfigConstants.h:474-526`).
- **Default:** English on first launch (it does not follow the OS language). Saved AES-encrypted under
  `currentlocalization` (`:64-83`).
- **Switching:** Settings left/right arrows cycle through 0..`LANGUAGE_COUNT` (= 4, so 5 languages; the name is misleading,
  `ConfigConstants.h:472`, `SettingsLayer.mm:374-396`).
- **QUIRK:** several strings are hard-coded English, not localised: "Armagedon" (`ChooseGameModeLayer.mm:79`), the
  "Normal"/"Hard"/"Impossible" labels in `ChooseLevelLayer.mm:210-248` (a dead method), "Ok!" in the upgrade alert, and the
  store prices. `StoreLayer.mm:108-121` special-cases Russian plurals.
- Translation quality is rough (e.g. "Compaign", German "Schlagen das Flag", "Die Kerbe" for Score). Retranslate.
- Fonts referenced by the UI: Times New Roman, Helvetica, MarkerFelt-Thin (system fonts), `konqa32.fnt` / `konqa32-hd.fnt`.

Mode-related strings (EN): `_CHOOSE_GAME` "Choose Game Mode", `_GET_READY` "Get Ready", `_GET_READY_FOR_BOSS`
"Get Ready For the Boss", `_LEVEL` "Level ", `_YOU_WON` "You Won", `_GAME_OVER` "Game Over", `_TIME_OFF`
"Time Is Off", `_FINISHED_GAME` "You finished the game", `_SCORE` "Score", `_VITAMINS` "Vitamins", `_LIFE` "+1 Life",
`_YOUR_TIME_IS` "Your Time Is" (unused).

**Unity mapping.** The Unity Localization package with a String Table imported from `localizations.json`. Key names can
stay. Default to the system language, falling back to English.

## 17. GameSettingsManager (`SettingsManager/GameSettingsManager.mm`)

- Singleton holding `soundIsEnabled`, `sfxIsEnabled`, `gameDifficulty`, `openedGameModes`, `controlType`,
  `isAutoFireEnabled` and `vitaminsCount`.
- First run (`isfirsttime` absent or 0) writes these defaults: SFX YES, music YES, Normal, modes 1, keyboard+mouse,
  autofire OFF, 0 vitamins (`:74-84`). Otherwise it decrypts every key (`:122-150`). Key list and encryption: section 13.
- Readers: difficulty (`Level.mm:45`, bosses, score multiplier), vitamins (HUD, store, upgrades, `VitaminManager`),
  control type and autofire (input/character docs).

## 18. PlistAction (`Managers/FileManager/PlistAction.mm`)

Dead utility (`dataFilePath:`, `writeStringInPlist:atFileName:`, `readStringFromPlist:atFileNamePath:`,
`addArrayToPlist:atPath:`). It is imported by ScoreManager and GameSettingsManager but never called.
`readStringFromPlist` returns an uninitialised pointer if the file is missing (`:42-54`). Don't port it.

---

## Key quirks and decisions for the rewrite (summary)

1. **Frame-based timing everywhere except the campaign NSTimers.** Grow intervals, the Rush/CTF generators, the Peace
   Walker clock and rush-zone fill all add fixed amounts per frame (tuned for 60 fps). Convert to seconds
   (1 unit = 1.667 s) and drive them with scaled delta time.
2. **Vitamin economy bugs:** vitamins are credited on spawn (not pickup); the cached balance in `VitaminManager` refunds
   spending; store purchases overwrite the balance. Decide the intended rules (probably "credit on pickup, add on purchase").
3. **Level 49 `number=48`:** only breaks the "Get Ready For the Boss" banner before level 50. Fix it in data.
   **Level 20** spawns 1 or 2 Infernos depending on timing; make it an explicit boss count.
4. **Rush/CTF enemy-pool index can reach 19** (out of bounds); enemies per wave grow without limit; Rush pays +250 for an
   unfinished zone at death.
5. **Saves are machine-locked** (AES keyed by hardware UUID, plus MD5-signed values with secret `ms3_ser_202$`). Unlocks
   happen on level *load*, not on win. Scores are saved unmultiplied, only for won campaign levels, never shown, and never
   for bosses or other modes. Build a fresh versioned save system.
6. **No audio exists**; **Armagedon** mode is dead; mode unlocks (`openedgamemodes`) are unused. Every visible mode is
   open from the start.
