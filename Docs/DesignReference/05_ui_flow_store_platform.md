# 05 - UI screens, screen flow, HUD, store / in-app purchases, platform services

Source root: `/home/claude/legacy` (all paths below are relative to it). Line numbers are `File:line`.

## Overview of classes covered

| Class / file | Role | One-line summary |
|---|---|---|
| `AppDelegate.mm` (`BacteriaAppDelegate`) | App bootstrap | Sizes the window to 1024 points wide, computes the UI scale factor `aspectRatio`, sets the save-hash secret, preloads resources, runs the main menu. |
| `main.m` + `Resources/English.lproj/MainMenu.xib` | Bootstrap | Loads the GL view class and the Cocoa main menu (About, Quit Cmd+Q, Toggle Full Screen Cmd+F). |
| `Scenes/Abstracted/Scene` | Base scene | Trivial `CCScene` subclass with an empty `initLayers` hook. |
| `Scenes/MainMenuScene` | Scene | Wraps a `MainMenuLayer`. Used only by "Go To Menu" from gameplay. |
| `Scenes/MainGameScene` | Scene | Gameplay scene: stacks `MainGameLayer` (world), `LevelOverLayer` (HUD + overlay menus) and `HUDLayer` (input), and connects their delegates. |
| `Layers/GameMenuLayer` | Base layer | Base class for menu screens: has a `menu` field, and on exit removes it and clears unused textures. |
| `Layers/MainMenuLayer` | Screen | Main menu with 7 text items. |
| `Layers/ChooseGameModeLayer` | Screen | Pick Campaign / Capture The Flag / Rush / Peace Walker. A fifth item, "Armagedon", is built but never shown. |
| `Layers/ChooseLevelLayer` | Screen | 5x10 grid of campaign levels plus a Normal/Hard/Impossible difficulty picker. |
| `Layers/CharacterUpgradeLayer` | Screen | Soft-currency (vitamins) shop: shot types, Double Speed, Immortality. Ownership flags are stored AES-encrypted. |
| `Layers/SettingsLayer` | Screen | Language, difficulty, auto-shoot, control type, SFX and music settings. |
| `Layers/StoreLayer` | Screen + StoreKit | Real-money vitamin packs through Mac App Store StoreKit. This is the only working IAP code path. |
| `Layers/AboutLayer` | Screen | An empty, black screen. ESC goes back. Also shown after the game is finished. |
| `Layers/MainGameLayer` | Gameplay layer | Holds the world, runs `tick:` every frame by calling `GameManager manageLoop:`, and passes HUD calls on to `LevelOverLayer`. |
| `Layers/HUDLayer` | Input layer | Despite its name it draws nothing. It sends mouse and keyboard events to `GameManager`, `InputManager` and `MainCharacterController`. |
| `Layers/LevelOverLayer` | The real HUD | Score, timer, hearts, special-ability bar, vitamins, centre messages, bonus text, slow-motion tint, pause menu and game-over menu. |
| `Managers/InAppManager/InAppManager` | IAP manager (dead) | Product-list fetcher and purchase helper. Never called. |
| `Managers/InAppManager/StoreManager` | IAP manager (dead) | Second purchase helper that grants nothing. Never called. |
| `Managers/InAppManager/NSData+AES256` | Crypto | AES-256 encryption keyed by this Mac's hardware UUID. Used to encrypt almost all saved settings and ownership data. |
| `Managers/GameCenter/GameCenterManager` | Game Center (**compiled copy**) | Mac GameKit singleton: authenticate, show the Game Center dialog, report a score. Only `loadLeaderboards` is ever called. |
| `Managers/GameCenterManager/*`, `GameCenter/*` | Game Center (not compiled) | Two copies of Apple's iOS "GKTapper" sample plus an iOS `LeaderboardViewController`. Dead. |

---

## 0. Global conventions in this area (read first)

### 0.1 Logical resolution and the `aspectRatio` UI factor
- The logical screen is 1024 points wide: `DEFAULT_SCREEN_WIDTH 1024` (`AppDelegate.mm:19`). Height is `round(1024 / (displayWidth/displayHeight))` (`AppDelegate.mm:47-53`). The director uses `kCCDirectorResize_AutoScale` (`AppDelegate.mm:58`), so `winSize` stays 1024 x h whatever the window size. In this document `w` = `winSize.width` = 1024 and `h` = `winSize.height`.
- Almost every UI size and offset is multiplied by a second factor, `CoreSettings.aspectRatio` (written `ar` below), computed from the **physical main-display width in points** (`AppDelegate.mm:43`):
  `ar = (displayW > 1024) ? 1024/displayW : displayW/1024`
  Typical values: 1280-wide display gives ar 0.80 (h 640). 1440x900 gives 0.711 (h 640). 1512x982 gives 0.677 (h 665). 1920x1080 gives 0.533 (h 576). 2560x1440 gives 0.40 (h 576).
- **QUIRK:** the logical resolution is fixed, yet `ar` shrinks the UI on wider monitors. The same build has a smaller UI on a 27" iMac than on a MacBook. The rewrite should choose one reference look (ar of about 0.7-0.8 matches a 2013 MacBook) and use resolution-independent UI (Canvas Scaler) instead.
- **QUIRK:** HUD labels multiply by `ar` twice: font size is `40*ar` **and** node scale is `0.9*ar` (for example `LevelOverLayer.mm:120,127`). Their effective size therefore scales with ar².
- Positions are cocos2d points with origin at the bottom-left. A `CCMenu` sits at screen centre by default (cocos2d 2.0), so menu-item positions below are **offsets from screen centre**.

### 0.2 Fonts used
- `Helvetica` 20 for the main menu and game-mode menus (`MainMenuLayer.mm:59-60`, `ChooseGameModeLayer.mm:64-65`). Not scaled by ar.
- `Times New Roman` for screen titles and the settings, upgrade, store and level-select labels.
- `MarkerFelt-Thin` for all HUD text and the pause and game-over menus (size 20, not scaled, `LevelOverLayer.mm:343-344,469-470`).

### 0.3 Input model in menus: mouse only
- Every menu is either a cocos2d `CCMenu` (in cocos2d-mac 2.0 it reacts to mouse clicks only and has no focus or keyboard navigation) or a set of sprites hit-tested by hand in `ccMouseDown:`.
- The only keyboard input in menus is **ESC (keycode 27, `ConfigConstants.h:397`) = back**.
- There is **no gamepad support anywhere**. `InputManager` has `DDHidJoystick` delegate stubs that are empty, and `joystickListener` is never called (`InputManager.mm:61-85`).
- For the Unity rewrite every screen needs explicit focus or navigation order. That order is given per screen below.

### 0.4 Localisation
- `LocalizationManager` loads `localizations.plist`, an array of 5 dictionaries: 0 English, 1 Russian, 2 French, 3 German, 4 Spanish (`ConfigConstants.h:461-469`). Converted copy: `/home/claude/out/data/raw/localizations.json`.
- All 5 languages share the same 53 keys. Keys are `#define`d at `ConfigConstants.h:474-526`. English text is quoted below as "key" -> "English".
- **QUIRK:** `LANGUAGE_COUNT` is 4 (`ConfigConstants.h:472`) but it is really used as the *maximum index*. Wrapping covers 0..4, all 5 languages.
- **QUIRK:** a few strings are hard-coded: "Armagedon", the NSAlert button "Ok!", store prices "$ 0.99"..., the store label "Buy Extra Vitamins"/"Buying...", the level-select dead-code labels, and Russian "Витаминов" in the store.

---

## 1. Startup sequence (`main.m`, `AppDelegate.mm`, `MainMenu.xib`)

1. `main.m:16-17`: `[CCGLView load_]`, then `NSApplicationMain`. This loads `MainMenu.xib`:
   - window `{{100,100},{1024,768}}` containing a `CCGLView`;
   - app menu with "About AntiBacteria" (standard about panel) and "Quit AntiBacteria" (Cmd+Q);
   - View menu with "Toggle Full Screen" (Cmd+F), which calls `toggleFullScreen:` (`AppDelegate.mm:103-112`, `enterFullScreenMode:` on the main screen).
2. `applicationDidFinishLaunching:` (`AppDelegate.mm:24-87`), in order:
   1. Creates the `LocalizationManager` singleton: reads the plist and the saved language (default English, `LocalizationManager.mm:66-70`). (**QUIRK:** it has already been created during static initialisation by `SettingsLayer.mm:13-24`, which fills global string arrays with message sends at file scope.)
   2. `[NSUserDefaults setSecret:@"ms3_ser_202$"]` (`AppDelegate.mm:32`). This is the HMAC-style secret for `NSUserDefaults+MPSecureUserDefaults`, which signs level-unlock and score entries.
   3. Measures the title-bar height, then computes `ar` (section 0.1) and stores it in `CoreSettings` (`:43-45`).
   4. Sets the window to 1024 x round(1024/displayAspect) plus the title bar, and the GL view to the same size (`:49-59`). Sets the director view and auto-scale resize mode, locks the window content aspect to the display's aspect (`:60`), centres the window (`:62-64`) and enables mouse-moved events (`:67`).
   5. `[GameResourceManager sharedGameResourceManager]` preloads sprite sheets (another area) (`:69`).
   6. `runWithScene:[MainMenuLayer scene]` (`:72`). The default texture format is then set to RGBA8888 (`:73`). **QUIRK:** this is set after resources were already loaded, but RGBA8888 is the default anyway.
   7. Commented out: start in fullscreen (`:74`), Game Center authentication (both managers, `:75-80`), and the Mac App Store receipt check `exit(173)` (`:82-85`).
   8. `CoreSettings.mainWindow = window` (`:86`), used as the parent for NSAlert sheets.
3. `applicationShouldTerminateAfterLastWindowClosed` returns YES (`:89-92`).
4. Frame rate: never set in code, so it is cocos2d-mac's default of 60 FPS (inferred).

Unity mapping: a boot scene that loads settings and the localisation table, then loads MainMenu. Fullscreen and window mode belong in Settings (Cmd+F has no console equivalent). Drop the receipt check and the save-hash secret.

---

## 2. Screen flow

### 2.1 ASCII flow chart

```
                         [App start]
                              |
                              v
+-------------------------- MAIN MENU (MainMenuLayer) ----------------------------+
|  Start Game | Upgrade Character | Vitamin Store | LeaderBoards | Game Options |   |
|  About | Quit                                    ESC: no-op                     |
+---+--------------+---------------+--------------+--------------+---------+------+
    |              |               |              |              |         |   Quit -> exit(0)
    v              v               v              v              v         v
 CHOOSE GAME   CHARACTER        VITAMIN      Game Center     SETTINGS    ABOUT
 MODE          UPGRADE          STORE        dialog (OS)     ESC->Main   (blank)
 ESC->Main     ESC->Main        ESC->Main    no screen                   ESC->Main
    |
    +-- Compaign ----------> CHOOSE LEVEL ---(click unlocked level)---+
    |                        ESC->Choose Game Mode                    |
    +-- Capture The Flag --+                                          |
    +-- Rush --------------+--> GAMEPLAY (MainGameScene) <------------+
    +-- Peace Walker ------+    |
       (Armagedon: hidden)      |  ESC (playing): PAUSE MENU   ESC (paused): resume
                                v
   +------------------------ GAMEPLAY STATES ------------------------------------+
   | PREPARING ("Level N"/"Get Ready For the Boss" 2 s -> "Get Ready" 1 s)        |
   |      -> PLAYING                                                              |
   | PLAYING --ESC--> PAUSED [Resume | Restart | Choose Level* | Go To Menu]      |
   | PLAYING --death--> "Game Over" 2 s -> GAME OVER MENU                         |
   |                                  [Restart | Choose Level* | Go To Menu]      |
   | PLAYING --Peace Walker time 0--> "Time Is Off\nScore" 1 s -> GAME OVER MENU  |
   | PLAYING --campaign timer 0 / boss killed--> "You Won" 2 s -> "Level N+1" or  |
   |      boss warning 2 s -> "Get Ready" 1 s; next level loads at 5 s           |
   | Level 50 boss killed --> "You finished the game" 5 s --fade 0.1 s--> ABOUT   |
   +------------------------------------------------------------------------------+
     Restart     -> same level, immediately, no Get Ready
     Choose Level*  (Campaign only) -> fade 0.1 s -> CHOOSE LEVEL
     Go To Menu  -> fade 0.1 s -> MAIN MENU
```

### 2.2 Transition table

| From | Trigger (input) | To | Code |
|---|---|---|---|
| Main menu | click "Start Game" | Choose Game Mode | `MainMenuLayer.mm:97-100` |
| Main menu | click "Upgrade Character" | Character Upgrade | `:103-106` |
| Main menu | click "Vitamin Store" | Store | `:114-117` |
| Main menu | click "LeaderBoards" | OS Game Center dialog (no scene change) | `:119-123` |
| Main menu | click "Game Options" | Settings | `:109-112` |
| Main menu | click "About" | About | `:126-129` |
| Main menu | click "Quit" | `exit(0)` | `:144-147` |
| Main menu | ESC | nothing (removes `currentLoadedLayer`, which is always nil) | `:131-142` |
| Choose Game Mode | "Compaign" | sets mode `kCompaignMode`, goes to `ChooseLevelLayer` (**QUIRK:** passes a layer, not a scene) | `ChooseGameModeLayer.mm:94-99` |
| Choose Game Mode | "Capture The Flag" / "Rush" / "Peace Walker" | sets mode, goes to `MainGameScene` | `:101-117` |
| Choose Game Mode | ESC | Main menu | `:125-134` |
| Choose Level | click an unlocked level icon | `LevelManager.levelCounter = level`, goes to `MainGameScene` | `ChooseLevelLayer.mm:251-263` |
| Choose Level | click a Normal/Hard/Impossible button | stays; difficulty saved | `:279-289,195-204` |
| Choose Level | ESC | Choose Game Mode | `:291-300` |
| Character Upgrade / Settings / Store / About | ESC | Main menu | `CharacterUpgradeLayer.mm:276-287`, `SettingsLayer.mm:404-413`, `StoreLayer.mm:215-224`, `AboutLayer.mm:34-43` |
| Gameplay (playing) | ESC | Pause menu | `InputManager.mm:140-145` -> `GameManager.mm:580-599` |
| Gameplay (paused) | ESC or "Resume" | Playing | `InputManager.mm:144`, `LevelOverLayer.mm:507-512` |
| Pause / Game over | "Restart" | same level restart | `LevelOverLayer.mm:536-539` -> `GameManager.mm:288-315` |
| Pause / Game over | "Choose Level" (campaign only) | `CCTransitionFade` 0.1 s, colour (1,1,1), to Choose Level | `GameManager.mm:610-617` |
| Pause / Game over | "Go To Menu" | fade 0.1 s to `MainMenuScene` | `GameManager.mm:601-608` |
| Gameplay | level-50 boss killed | 5 s, then fade 0.1 s to `AboutLayer` (a layer, not a scene) | `GameManager.mm:224-228,246-253` |

All transitions use `replaceScene` (no scene stack). Menu-to-menu changes are instant cuts with no transition. Only gameplay exits fade, for 0.1 s.

---

## 3. Menu screens

### 3.1 `GameMenuLayer` (base)
- `+scene` returns nil (`GameMenuLayer.mm:13-16`). `createLayerMenu` is empty. `onExit` removes `menu` and calls `removeUnusedTextures` (`:23-27`).
- **QUIRK:** every subclass except `MainMenuLayer` overrides `onExit` **without calling `[super onExit]`** (`ChooseGameModeLayer.mm:136`, `ChooseLevelLayer.mm:302`, `SettingsLayer.mm:439`, `StoreLayer.mm:226`, `CharacterUpgradeLayer.mm:478`, `AboutLayer.mm:45`; also `MainGameLayer.mm:201`, `LevelOverLayer.mm:72`). The cocos2d node is never marked as not running and its children never receive `onExit`. Not worth reproducing.

### 3.2 `MainMenuLayer` - Main menu
Purpose: the title menu. There is no logo, background or title text; it is a black screen with a column of white text items.

| Order (top to bottom) | Text key -> English | Offset from centre | Action |
|---|---|---|---|
| 1 | `_START_GAME` -> "Start Game" | (0, +20) | Choose Game Mode |
| 2 | `_UPGRADE_CHARACTER` -> "Upgrade Character" | (0, -20) | Character Upgrade |
| 3 | `_VITAMIN_STORE` -> "Vitamin Store" | (0, -60) | Store (IAP) |
| 4 | `_LEADERBOARDS` -> "LeaderBoards" | (0, -100) | Game Center dialog |
| 5 | `_GAME_OPTIONS` -> "Game Options" | (0, -140) | Settings |
| 6 | `_ABOUT` -> "About" | (0, -180) | About |
| 7 | `_QUIT` -> "Quit" | (0, -220) | `exit(0)` |

- Font Helvetica 20, `CCMenuItemFont`, default white, not scaled by ar (`MainMenuLayer.mm:59-79`). Positions are at `:86-92`.
- **QUIRK:** the `CCMenu` child order is Start, Upgrade, Store, Settings, LeaderBoards, About, Quit (`:81`), which differs from the visual order. Use the visual order for gamepad navigation.
- Keyboard: ESC does nothing useful (`:131-142`). Mouse only.
- Unity: vertical button list, first item focused, wrap-around navigation, B/Esc does nothing (or offers a quit confirmation on PC). Remove "Vitamin Store" and "Quit" on consoles. Remap "LeaderBoards" to platform leaderboards (section 7).

### 3.3 `ChooseGameModeLayer` - Choose game mode

| Element | Text | Layout | Font | Action |
|---|---|---|---|---|
| Title label | `_CHOOSE_GAME` -> "Choose Game Mode" | (w/2, h/2+100), box 500x40, centred, colour (128,0,0) dark red | Times New Roman 40 (not ar) | - |
| Item | `_COMPAIGN` -> "Compaign" (sic) | centre + (0,+20) | Helvetica 20 | `kCompaignMode`, then Choose Level |
| Item | `_CAPTURE_FLAG` -> "Capture The Flag" | (0,-20) | Helvetica 20 | `kTakeTheFlagMode`, then gameplay |
| Item | `_RUSH` -> "Rush" | (0,-60) | Helvetica 20 | `kRushMode`, then gameplay |
| Item | `_PEACE_WALKER` -> "Peace Walker" | (0,-100) | Helvetica 20 | `kPeaceWalkerMode`, then gameplay |
| (hidden) | "Armagedon" (hard-coded) | (0,-140) | Helvetica 20 | `kArmagedonMode`, never reachable |

Code: `ChooseGameModeLayer.mm:52-90`. **QUIRK / dead content:** "Armagedon" is created and positioned but left out of `menuWithItems:` (`:79-81`), so the mode `ArmagedonGameMode` cannot be reached. ESC returns to the main menu. Mouse only.

### 3.4 `ChooseLevelLayer` - Campaign level select + difficulty
Purpose: pick one of the 50 campaign levels (unlocked ones only) and the difficulty.

Elements (`ar` applied where shown):
- **Title**: `_CHOOSE_LEVEL` -> "Choose Level". Times New Roman 60ar, box 500ar x 80ar, colour (0,255,0), at (w/2, h - 100ar) (`ChooseLevelLayer.mm:52-62`).
- **Level grid**: a `CCMenu` at screen centre with 50 `CCMenuItemImage`s, all using `vitamin.png` for normal and selected, scale 0.25ar (`:64-125`).
  - Row r = 0..4 (top to bottom), column c = 0..9; level = r*10 + c + 1.
  - Offset from centre: x = -450ar + c*100ar, y = 192ar - r*100ar (`:64-68,113-121`).
  - Unlocked (`LevelManager levelIsUnlocked:`): full opacity, normal colour. Column 9 (levels 10/20/30/40/50, boss levels) is tinted (0,128,255). Click calls `enableChooseGameMode:` (`:82-94`).
  - Locked: opacity 128, tint red (255,0,0), boss column (0,128,255). **No target is set, so clicks do nothing** (`:96-110`).
  - **QUIRK:** level icons have no level number on them; the player only sees 50 identical vitamins. The rewrite should add numbers, a lock icon and a boss marker.
  - Level 1 is unlocked by `LevelManager` init (`LevelManager.mm:48`). Each level unlocks when it is loaded (`LevelManager.mm:67-77`), in secure defaults key `hash("levelisunlocked"+N)`.
- **Difficulty picker** (`:130-204`):

  | Label (Times New Roman 30ar, box 500ar x 40ar, white) | Label position | Button (`BuyButton.png`, scale 1, not ar) | Button position |
  |---|---|---|---|
  | `_NORMAL` -> "Normal" | (w/2 - 300ar, 180ar) | button 0 | (w/2 - 300ar, 100ar) |
  | `_HARD` -> "Hard" | (w/2, 180ar) | button 1 | (w/2, 100ar) |
  | `_IMPOSSIBLE` -> "Impossible" | (w/2 + 300ar, 180ar) | button 2 | (w/2 + 300ar, 100ar) |

  The selected button is tinted red (255,0,0) and the others white. Clicking sets `GameSettingsManager.gameDifficulty` and saves immediately (`:195-204,279-289`). The initial selection is the saved difficulty (`:190-191`). This is the same setting as in Settings.
- Dead code: `chooseGameModeMenuAnimationHasDone` (`:206-249`) and `startLevel:` (`:265-277`).
- **QUIRK (inferred, verify on the Mac):** at h about 640 and ar about 0.71 the bottom grid row (y about h/2 - 148) may overlap the difficulty labels at y about 128.
- Gamepad: 10x5 grid navigation, with the difficulty row as a separate focus group below it (or a shoulder-button toggle). Locked cells should still be focusable but show "locked".

### 3.5 `CharacterUpgradeLayer` - Vitamin upgrade shop (soft currency)
Purpose: spend vitamins collected in play (and, in 2013, bought with real money) on a permanent default shot type, Double Speed or Immortality.

Layout (`CharacterUpgradeLayer.mm:136-273`):
- Title `_UPGRADE_SOME_FEATURES` -> "Upgrade Some Features". Times New Roman 40ar, box 500ar x 100ar, green, at (w/2, h - 150ar) (`:141-151`).
- Wallet `_VITAMINS` + " N" -> "Vitamins 12345". Times New Roman 40ar, cyan (0,255,255), at (w/2, h - 300ar) (`:154-168`). Refreshed after every click (`:463-467`).
- 7 rows. Row i is at y = h/2 + h/8 - i*80ar (`:181-187,271`). Each row has:
  - Name label: Times New Roman 40ar, green, centred at x = w/2 - w/4 (`:191-199`).
  - Buy/select button: `BuyButton.png`, scale ar, at (w/2 + w/6, rowY + buttonH/8), tag = i (`:201-207`).
  - Price label: Times New Roman 40ar, left-aligned in a 500ar box centred at (button.x + 300ar, button.y - 25ar). It is blank once the item is owned (`:219-232`).

| Row i | Name key -> English | Price (vitamins) `:126-132` | Ownership key (NSUserDefaults) / owned value `ConfigConstants.h:443-455` | Grants |
|---|---|---|---|---|
| 0 | `_NORMAL_SHOOT` -> "Normal Shoot" | "" (0) | key "" (empty) | `ShootType kNormalShoot` (0) |
| 1 | `_WATER_SHOOT` -> "Water Shoot" | 20000 | `watershootisboughtkey` / `watershootisboughtvalue` | `kWaterShoot` (1) |
| 2 | `_WINDMLL_SHOOT` -> "Windmill Shoot" | 20000 | `windmillshootisboughtkey` / `windmillshootisboughtvalue` | `kWindilShoot` (2) |
| 3 | `_CIRCLE_SHOOT` -> "Circle Shoot" | 20000 | `circleshootisboughtkey` / `circleshootisboughtvalue` | `kExplosionShoot` (3) |
| 4 | `_TORNADO_SHOOT` -> "Tornado Shoot" | 20000 | `tornadoshootisboughtkey` / `tornadoshootisboughtvalue` | `kTornadoShoot` (4) |
| 5 | `_DOUBLE_SPEED` -> "Double Speed" | 40000 | `doublespeedisboughtkey` / `doublespeedisboughtvalue` | toggle `CharacterPropertiesManager.isDoubleSpeed` |
| 6 | `_IMMORTALITY` -> "Immortality" | 1000000 | `immortalityisboughtkey` / `immortalityisboughtvalue` | toggle `isImmortal` (character ignores hits, `MainCharacter.mm:705-708`) |

Rules (`ccMouseDown:` `:289-475`, mouse only):
- **Rows 0-4 (shot types)** are a radio group. The equipped row is tinted blue (0,0,255) (`:211-212,321-323`).
  - Not owned and wallet >= price: subtract the price, save the wallet, blank the price, write the owned flag (AES), set `currentShootType = row` and save (`:313-341`).
  - Not owned and wallet < price: NSAlert sheet `_NO_VITAMINS` -> "You don't have enough Vitamins" with button "Ok!" (`:47-64,342-344`).
  - Owned: just equip (`:347-356`).
- **Rows 5-6** are on/off toggles: red (255,0,0) = active, white = inactive (`:250-266`).
  - The first purchase deducts the price and switches the toggle on (`:370-397`, `:421-447`). Later clicks toggle for free (`:403-408`, `:454-459`).
- The equipped shot type becomes the character's default shot. Double Speed and Immortality are read when the character spawns (`MainCharacter.mm:110-112`).
- **QUIRK:** row 0 has no ownership key. `objectForKey:@""` returns nil, so the row is "not owned" with price 0. Each click "buys" it and writes an encrypted "" under the empty key "" (`:313-335`). Harmless but silly.
- **QUIRK:** `encriptedValuesArray` (`:95-124`) is computed (NSKeyedArchiver + AES) and never used.
- **QUIRK:** labels are built with `stringWithFormat:` using a localised string as the format (`:158`). A `%` in a translation would crash.
- **Balance note:** Immortality costs 1,000,000 vitamins, exactly the largest real-money pack ($9.99, section 5). It was designed as a paid "god mode".
- Unity: a list of 7 selectable rows (shot types as a radio group, two toggles), a wallet header and an in-game confirm/"not enough" dialog instead of NSAlert. Fully gamepad navigable.

### 3.6 `SettingsLayer` - Game options
Layout: title `_GAME_OPTIONS` -> "Game Options", Times New Roman 50ar, green, at (w/2, h - 100ar) (`SettingsLayer.mm:64-74`). Then six rows. Each row has a name label (Times New Roman 30ar, box 500ar x 60ar, green) at x = w/4. Value labels sit at x = 3w/4; arrow sprites (`arrow.png`, scale 0.5ar, the right one flipped) at value.x +/- 150ar, y + 20ar; toggle sprites (`Play_Normal.png`, scale 0.5ar, opacity 255 = on, 128 = off) at x = 3w/4.

| Row y | Setting | Label key -> English | Control | Values / range | Default | Saved to | Code |
|---|---|---|---|---|---|---|---|
| h - 200ar | Language | `_LANGUAGE` -> "Language" | arrows around `_LANGUAGE_VALUE` (the language's own name) | 0..4 (English, Русский, Francais, Deutsche, Español), wraps | 0 English | `currentlocalization` (AES) | `:80-121,372-398` |
| h - 300ar | Difficulty | `_DIFFICULTY` -> "Difficulty" | arrows around `_NORMAL`/`_HARD`/`_IMPOSSIBLE` | 0..2 (`GameDifficulty`, `ConfigConstants.h:345-351`), wraps | 0 Normal (`GameSettingsManager.mm:79`) | `gamemode` (AES) | `:126-167,305-326` |
| h - 400ar | Auto shoot | `_AUTO_SHOOT` -> "Auto Shoot" | toggle at (3w/4, rowY) | bool | NO (`GameSettingsManager.mm:82`) | `isautofireEnabled` (AES) | `:171-191,297-302` |
| h - 500ar | Control type | `_CONTROL_TYPE` -> "Control Type" | arrows around `_WASD_ARROWS` -> "WASD + ARROWS", `_ARROWS` -> "Only Arrows", `_ONLY_MOUSE` -> "Only Mouse" | 0..2 (`CharacterMoveType`: 0 `kKeyBoardAndMouse`, 1 `kOnlyKeyboard`, 2 `kOnlyMouse`, `ConfigConstants.h:318-324`), wraps | 0 (`GameSettingsManager.mm:81`) | `controltype` (AES) | `:194-237,330-351` |
| h - 600ar | SFX | `_SOUND_SFX` -> "Sound SFX" | toggle at (3w/4, rowY + 20ar) | bool | YES (`:76`) | `sfxenabled` (AES) | `:241-263,354-360` |
| h - 700ar | Music | `_GAME_MUSIC` -> "Music" | toggle at (3w/4, rowY + 20ar) | bool | YES (`:77`) | `soundenabled` (AES) | `:267-288,363-369` |

- Every change is saved immediately (`saveGameSettings`, NSUserDefaults domain `com.Abelix.AntiBacteria`, `Info.plist`).
- Changing the language relabels the page live (`:415-437`). Other screens pick it up when rebuilt.
- What the settings actually do:
  - Difficulty multiplies HUD score by (difficulty+1) (`GameManager.mm:619-624`); its other effects are in another area.
  - Control type is read once per level start (`InputManager.mm:57`). Inferred from `InputManager.mm:95-136`: `kKeyBoardAndMouse` = WASD **and** arrows move, mouse aims, click fires. `kOnlyKeyboard` = WASD move, arrows aim and fire. `kOnlyMouse` = mouse moves and aims. **QUIRK:** the labels do not describe this ("WASD + ARROWS" is really keyboard+mouse; "Only Arrows" is really WASD + arrows).
  - Auto shoot: when ON, mouse clicks and arrow keys do not start or stop firing (`HUDLayer.mm:77,94`; `InputManager.mm:156,218`). Firing is then automatic (`MainCharacter.mm:102,905`).
  - **QUIRK:** "Music" (`soundIsEnabled`) is never read by gameplay, and no background music is ever started (`playBackgroundMusicAtString` has no callers). The toggle is dead. SFX is respected in `SoundManager.mm:45`.
- Mouse only (all controls are sprite hit-tests). Unity: a standard options list with left/right on gamepad for selectors and A for toggles. Add volume sliders, fullscreen, resolution and key rebinding as needed.

### 3.7 `StoreLayer` - Vitamin store (real money). See section 5.

### 3.8 `AboutLayer`
- An empty layer: keyboard and mouse enabled, ESC goes to the main menu (`AboutLayer.mm:23-43`). **QUIRK:** it shows nothing; the screen is black. It is also the "game finished" destination (`GameManager.mm:249-252`). The rewrite needs a real credits screen.

### 3.9 Scenes
- `Scene` (`Scenes/Abstracted/Scene.m`) is trivial.
- `MainMenuScene` adds `[[MainMenuLayer alloc] init]` at z `zMainMenuLayer` (0) (`MainMenuScene.mm:26-31`). It is used by "Go To Menu". Everywhere else `[MainMenuLayer scene]` is used, which is equivalent.
- `MainGameScene` (`MainGameScene.mm:29-52`):
  - `MainGameLayer` at z/tag 2 (`zMainGameSceneLayer`);
  - `LevelOverLayer` at z/tag 4 (`zLevelOverLayer`);
  - `HUDLayer` at z/tag 5 (`zUserInterfaceLayer`) (enum `ConfigConstants.h:33-46`);
  - wiring: `hudLayer.mainGameLayerDelegate = mainLayer`, `mainLayer.levelOverLayerDelegate = levelOverLayer`, `levelOverLayer.mainGameLayerdelegate = mainLayer`.
  - The camera only moves `MainGameLayer` (`Camera.mm:47-93`, `gameLayer.camera`), so `LevelOverLayer` and `HUDLayer` stay in screen space.

---

## 4. Gameplay: `MainGameLayer`, `HUDLayer`, `LevelOverLayer` (HUD)

### 4.1 `MainGameLayer` - gameplay root and per-frame loop
- It is a thin proxy. Most methods forward to `LevelOverLayer` (`MainGameLayer.mm:56-199`).
- Stubs: `showPauseMenu`, `showDistanceTimer`, `reFillSpecialAbilitieBar`, `createBossLife` (`:66-73,158-161,176-179`).
- Unused leftovers: the `sharedMainGameLayer` singleton (`:26-40`), the iOS `PAUSE_BUTTON_RECT`/`accelerometerFrequency` (`:14-17`) and `isTouchEnabled` (`:48`).

**Level start sequence (first entry into MainGameScene):**
1. `onEnterTransitionDidFinish` sets `GameManager.mainGameLayerDelegate = self` and calls `initGameManager` (`MainGameLayer.mm:136-142`).
2. `GameManager.initGameManager` (`GameManager.mm:87-106`):
   - `InputManager runListener` (reads the control type);
   - sets `characterIsDead=YES`, `gameIsPaused=YES`, `gameIsPreparing=YES`;
   - `initGameWorld`, `initPhysics` (the Box2D world singleton; PTM_RATIO 32 `Physics.h:18`);
   - `prepareGameForStart` sets `gameIsPreparing=YES`, then calls `startGame`.
3. `GameManager.startGame` (`:108-124`):
   - `gameWorld Run` (builds bounds and instantiates the game-mode class by name: `CompaignGameMode`, `TakeTheFlagGameMode`, `RushGameMode`, `PeceWalkerGameMode` or `ArmagedonGameMode`, `GameModeManager.mm:46-73`);
   - creates the main character;
   - sets `gameIsStarted=YES`, `gameIsPaused=NO`, `characterIsDead=NO`;
   - `MainGameLayer.startGame`: builds the HUD (`LevelOverLayer initLevelOverLayer`) and **schedules `tick:` every frame** (`MainGameLayer.mm:149-155`);
   - creates the `Camera`;
   - `MainGameLayer.prepareGameForStart` calls `LevelOverLayer.prepareGameForStart` (section 4.3.3).
4. After 2 s + 1 s of NSTimers, `LevelOverLayer.startLevel` sets `gameIsPreparing=NO` (`LevelOverLayer.mm:444-448`). Player movement and aiming (`InputManager.mm:264`) and click-to-shoot (`HUDLayer.mm:79`) are blocked until then. **The world already simulates while "preparing"**: physics steps and game-mode NSTimers (enemy spawns, the campaign clock) run (inferred from `GameManager.mm:424` and `CompaignGameMode.mm:90-101`).

**Per-frame update order.** `tick:(dt)` -> `GameManager.manageLoop:` -> `managePhysics:` (`MainGameLayer.mm:101-104`, `GameManager.mm:413-487`). Nothing runs if `gameIsPaused`. Otherwise:
1. `b2World::Step(dt, velocityIterations 8, positionIterations 2)` (`GameManager.mm:420-427`). Contact callbacks fire inside the step (score, damage and pickups come from `ContactListener`). **QUIRK (frame-rate):** variable timestep, no fixed step. `dt` is already scaled by the scheduler time scale (0.35 in slow motion, `GameManager.mm:32,336`).
2. Loop over every Box2D body with user data:
   - main character: if `flag == kDealloc` and the character is not yet dead, schedule `showGameOver` in 0.1 s (NSTimer), set `characterIsDead=YES` and **return immediately** (the rest of this frame is skipped) (`:438-450`). Otherwise update the camera from the character sprite (`:461`);
   - enemies (only if alive and started): `EnemyAgent update` (`:466-473`).
3. `MainCharacterController update`, which calls `MainCharacter update` (`:479`).
4. `GameWorld update`, which calls `GameModeManager update`: current GameMode `update`, then `BonusManager update`, then `VitaminManager update` (`:481`, `GameModeManager.mm:85-90`).
5. `InputManager inputEventListener`: if not preparing, aim point = mouse + camera position, rotate the character, then move by keyboard flags or toward the mouse, depending on control type (`:484`, `InputManager.mm:256-281`).
- **QUIRK:** the camera follows the character's sprite **before** the character and input update, so it lags by one frame.
- **QUIRK:** many gameplay clocks are NSTimers in real time and ignore slow-motion time scale (campaign 1 s clock, spawn timers, special-ability timer, HUD refill). Peace Walker's clock is per frame (`+0.01*timeScale` per tick, `PeceWalkerGameMode.mm:70`; one "second" every 100 frames, about 1.67 s at 60 FPS). Unity should pick one time base (scaled `Time.deltaTime`) on purpose.

**Pause / resume:**
- ESC while playing calls `GameManager.pauseGame` (`InputManager.mm:140-145`). It only acts if not already paused, not in the level-won state, not preparing and not dead (`GameManager.mm:580-599`). Then: `gameState = kGamePausedState`, `CCDirector pause` (stops cocos actions and schedulers), `gameIsPaused=YES`, and `MainGameLayer.pauseGame` **unschedules `tick:`** and asks `LevelOverLayer` to build the pause menu (`MainGameLayer.mm:80-84`).
- ESC while paused, or "Resume", calls `GameManager.resumeGame` (`GameManager.mm:561-571`): `gameIsPaused=NO`, director resume + `startAnimation`, `MainGameLayer.resumeGame` (removes the menu and re-schedules `tick:`; `[self unschedule:_cmd]` at `:95` is a meaningless no-op), and `MainCharacterController resume`.
- NSTimers are **not** paused by `CCDirector pause`. Timers that matter check `gameIsPaused` themselves (campaign clock, ability timer, bar refill). There is no pause button and no automatic pause on window focus loss.
- **QUIRK (inferred):** in Peace Walker, after "Time Is Off" the character is still alive, so ESC can open the pause menu on top of the game-over menu. The `menu` field is overwritten and one menu can no longer be removed.

**Level end sequences:**
- *Campaign, normal level won*: the campaign clock reaches 0 with the character alive (`GameManager.mm:182-187`), so `gameLevelHasWon` runs (`:204-217`): `levelHasWon=YES`, game-mode special action, HUD `levelHasWon` (section 4.3.3), `ScoreManager synchronizeLevelScore`, and an NSTimer **5 s** to `loadNextLevelInTime`. That method resets the time scale and blur, calls `gameModeActionLoadNextLevel`, resets the HUD, re-schedules `tick:` and re-arms the special ability (`:241-286`).
  - **QUIRK:** `gameIsPreparing` is not set on win (commented out at `:206`), so the player can keep moving and shooting during the 5 s interlude.
  - **QUIRK:** the HUD's own 2 s + 2 s + 1 s message chain ends at 5 s too, racing with this 5 s reset.
- *Boss level won*: a boss AI calls `levelHasWonAfterBigBoss` when only 1 enemy remains (`GameManager.mm:219-239`, callers in `AI/BigBoss.mm:139`, `Inferno.mm:182`, `BigRedNoise.mm:257`, `Anakonda.mm:220`, `Frunctus.mm:296`). If the level number == `LAST_LEVEL` 50 (`:33`), the game is finished: "You finished the game", then 5 s, `resetGameData`, and a fade to `AboutLayer`. Otherwise the flow is the same as a normal win (5 s to the next level). `synchronizeLevelScore` is **not** called for boss levels.
- *Death*: detected in step 2 above. 0.1 s later `showGameOver` disables the special ability, stops the game world and shows HUD "Game Over"; 2 s later the game-over menu appears (`GameManager.mm:489-498`, `LevelOverLayer.mm:329-377`). Physics keeps stepping; enemy AI updates stop.
- *Peace Walker time-out*: `updateTickInGameMode(0)` calls `gameOverInGameMode`: label "Time Is Off\nScore: N", then 1 s to the game-over menu (`GameManager.mm:193-202`, `LevelOverLayer.mm:556-570`).
- *Restart* (`GameManager.simulateGameRestart`, `:288-315`): time scale 1, character respawned at (240,160), game world/mode restart, HUD reset, `tick:` re-scheduled, `gameIsPreparing=NO`. **There is no "Get Ready" sequence on restart.**
- *Quit to menu / level select*: `resetGameData` releases the character, world and input, sets `gameState=kMainMenuState`, calls `MainGameLayer.quitGame` (unschedule tick, reset HUD, remove `LevelOverLayer`) and resets ScoreManager (`:147-180`). Then the 0.1 s fade.

Unity mapping: a `GameFlowController` state machine (Preparing, Playing, Paused, LevelWon, GameOver, Finished) on a single gameplay scene. Use `Time.timeScale = 0` for pause and drive all gameplay clocks from scaled time. Physics in `FixedUpdate` replaces the variable `Step`. Keep the update order: physics, character, game mode/bonuses/vitamins, input. Read input first in Unity if responsiveness matters.

### 4.2 `HUDLayer` - gameplay input (draws nothing)
- Mouse moved (only in control types `kOnlyMouse` or `kKeyBoardAndMouse`) calls `GameManager rotateWithMouse:` (stores the aim point) and `AiInput setCursorPosition:` (`HUDLayer.mm:39-52`).
- Mouse dragged does the same, only when not paused (`:54-68`).
- Mouse down: if auto-fire is OFF and not paused or preparing, `MainCharacterController startShoot` (`:70-86`). Mouse up: `endShoot` (`:88-101`).
- Key down/up: ignored while Cmd is held, so Cmd+F and Cmd+Q still work (`:110-112`). Otherwise the key is wrapped in a `KeyBind` and sent to `InputManager proceedKey:down:` (`:103-150`).
- Key map (`ConfigConstants.h:392-403`, `InputManager.mm:95-145`): W/A/S/D (119/97/115/100) move; arrows (63232-63235) move or aim depending on control type; Space (32) = special ability; ESC (27) = pause/resume.
- Gamepad mapping needed in the rewrite: left stick = move, right stick = aim (plus fire when auto-fire is on, or with a trigger), a button = special ability, Start = pause.

### 4.3 `LevelOverLayer` - the HUD and the overlay menus
All widgets are created in `initLevelOverLayer` when the level starts (`LevelOverLayer.mm:86-189`). Positions are screen space.

#### 4.3.1 HUD elements

| Element | Asset / font | Position, anchor, scale | Value source / behaviour | Code |
|---|---|---|---|---|
| Score | "`_SCORE`: N" -> "Score: 0". MarkerFelt-Thin 40ar, box 300x50, left-aligned | (35ar, h - 20ar), anchor (0,1) top-left, scale 0.9ar | Internal `score` += value from `GameManager.updateLScoreWithValue(points)`, which passes `points*(difficulty+1)` (`GameManager.mm:619-624`). Points: enemy `killScore` (`ContactListener.mm:71,85`), flag `FLAG_CONST_POINT` 100 (`ConfigConstants.h:440`), rush station `RUSH_CONS_POINT` 250 (`:441`). Hidden on boss levels. | `:113-128,379-385` |
| Timer | "1 : 20" placeholder ("0 : 10" in Peace Walker). MarkerFelt-Thin 40ar, box 200x50, centred | (w - 350ar, h - 40ar), scale 0.9ar | Text from `GameManager.updateTickInGame/InGameMode` as `"%i : %02d"` (`GameManager.mm:188-190,199-201`). Campaign: `LevelManager.totalLevelTime`, counting down once per real second (`CompaignGameMode.mm:110-117`). Peace Walker: `totalGameModeTime`. Visible only in Campaign and Peace Walker (`:411-412`); hidden on boss levels. | `:130-140,546-549` |
| Hearts (lives) | `heart.png` x3, z 1, scale 0.5ar | first at (w - 5.5*35ar = w - 192.5ar, h - 40ar), then +1 sprite width to the right each | Mirrors `MainCharacter.life` (3) by events only: `updateHeartCount:NO` removes the right-most heart on a hit (`MainCharacter.mm:709,722`); `:YES` adds one on the right (max 3) on a +1 Life bonus (`MainCharacter.mm:464-465`). Rebuilt to 3 on reset. | `:191-207,301-327` |
| Special-ability bar | `CCProgressTimer` bar of `boss_life_on.png` (image name is misleading). Horizontal, change rate (1,0), midpoint (-1,0) | (w/2, h - 40ar), scale 0.4ar (inner sprite scale ar) | Starts at 100%. While an ability is active it drops `SPECIAL_ABILITE_PROGRESS_STEP` 1% per `MainCharacter` 0.1 s tick (`ConfigConstants.h:438`, `MainCharacter.mm:338-383`); `SPECIAL_POWER_TIME_LIMIT` 10 s (`ConfigConstants.h:436`) empties it. Then it refills by `SPECIAL_ABILITIE_TIMER_STEP*20` = 0.1*20 = 2% every 0.1 s (NSTimer, skipped while paused), so a full refill takes 5 s, after which `GameManager.reFillSpecialAbilitieBar` re-enables Space. Hidden on boss levels (the ability is disabled there, `MainCharacter.mm:409-412`). | `:216-238,245-299` |
| Vitamins (wallet) | "`_VITAMINS`: N" -> "Vitamins: 0". MarkerFelt-Thin 40ar, box 400x50, left-aligned | (35ar, 65ar), anchor (0,1), bottom-left, scale 0.9ar | Initial value `GameSettingsManager.vitaminsCount` (persistent wallet). Updated on each vitamin pickup with `VitaminManager.vitaminsCount`, which is also saved to the wallet (`ContactListener.mm:181,188`, `VitaminManager.mm:84-90`). | `:169-186,209-214` |
| Centre message | MarkerFelt-Thin 40ar, box 500x50, centred | (w/2, h/1.5), scale 2.0ar | Shows `_LEVEL`+N "Level N", `_GET_READY` "Get Ready", `_GET_READY_FOR_BOSS` "Get Ready For the Boss", `_YOU_WON` "You Won", `_GAME_OVER` "Game Over", `_TIME_OFF`+"\n"+score "Time Is Off / Score: N", `_FINISHED_GAME` "You finished the game". **QUIRK:** the 2-line string in a 50-pt-high box may clip. | `:143-153` |
| Bonus text | MarkerFelt-Thin 40ar, box 200x50, centred | (w/2, h/2 + h/4) | Name of the picked-up bonus (e.g. "Water Shoot", "+1 Life", "Boooooom!", "Immortality" for invisibility, "Double Speed" for fast movement; `MainCharacter.mm:430-497`), cleared after `CCDelayTime` 2 s. **QUIRK:** this is a cocos action, so it stretches under slow motion (2/0.35 = 5.7 s real). | `:89-99,520-534` |
| Slow-motion tint | `SlowMotionBc.png`, centred, scale 1.5ar, opacity 0 | (w/2, h/2) | Fades to opacity 50/255 in 0.2 s when the ability bar starts draining, and back to 0 in 0.2 s when it ends. **QUIRK:** this happens for **every** special ability (big explosion, invisible, fast run), not only slow motion. | `:102-107,254-262` |

**Not present in the HUD:**
- No boss health bar: `createBossLife` is an empty stub (`MainGameLayer.mm:176-179`, `GameManager.mm:126-129`).
- No weapon or shot indicator: the weapon is shown only by the bonus text and the character's magenta tint (`MainCharacter.mm:434-457`).
- No crosshair or cursor sprite.
- No pause button: `PAUSE_BUTTON_RECT` is an unused iOS leftover (`HUDLayer.mm:16-17`).

**QUIRK (inferred, verify):** "boss level" visibility uses `LevelManager.currentLevel.number % 10 == 0` (`:160,400`). Non-campaign modes never load a level, so `currentLevel` is whatever the last campaign session left (or nil). After playing campaign level 10, starting Rush may show "Get Ready For the Boss" and hide the score.

#### 4.3.2 Overlay menus
- **Pause menu** (`pause`, `:466-505`): `CCMenuItemFont` MarkerFelt-Thin 20, white, centred.

  | Item | Offset | Action |
  |---|---|---|
  | `_RESUME` "Resume" | (0, +40) | `GameManager.resumeGame` |
  | `_RESTART` "Restart" | (0, 0) | `GameManager.simulateGameRestart` |
  | `_CHOOSE_LEVEL` "Choose Level" (Campaign only) | (0, -40) | `goToLevelSelect` |
  | `_GOTO_MENU` "Go To Menu" | (0, -80); (0, -40) if no Choose Level | `goToMenu` |

  There is no dimming or backdrop; the frozen game stays visible.
- **Game-over menu** (`showGameOverInTime:`, `:340-377`): Restart (0,0), Choose Level (0,-40, Campaign only), Go To Menu (0,-80 or -40). The "Game Over"/"Time Is Off" text stays above it.
- Both are mouse-only `CCMenu`s. Unity: a focusable vertical menu with Resume focused by default, and B/Esc = Resume in the pause menu.

#### 4.3.3 HUD message timelines (all NSTimers, real time)
- **Level start** (`prepareGameForStart`, `:387-442`): stop the refill timer and set the bar to 100%.
  - Boss level (number % 10 == 0 and > 0): hide score, timer, bar and tint; text "Get Ready For the Boss".
  - Otherwise: show score, show the timer in Campaign/Peace Walker only, text "Level N" in Campaign (N = `levelCounter - 1`, with a special case when the counter is 1, `:420-423`) or empty.
  - Then 2 s: "Get Ready" (`:450-462`). Then 1 s: clear the text and set `gameIsPreparing=NO` (`:444-448`).
- **Level won** (`levelHasWon`, `:572-584`): the score text shows "Score: 0" (**QUIRK:** the internal `score` is not reset until `reset`, so new points would show the old total plus new); text "You Won".
  - Then 2 s, `getReady:` (`:586-630`): if the *current* level % 10 == 9 (next is a boss), hide the widgets and show "Get Ready For the Boss". Otherwise show "Level <levelCounter>" (the next level), restore the widgets and force-refill the bar.
  - Then 2 s: "Get Ready". Then 1 s: start.
  - Meanwhile, at 5 s, `GameManager.loadNextLevelInTime` calls `reset`, which clears the text and hearts and sets the timer back to "1 : 20" (`:632-669`).
- **Game over**: "Game Over", then 2 s to the menu. **Time-out**: "Time Is Off\nScore: N", then 1 s to the menu. **Game finished**: "You finished the game" (`gameHasWon`, `:240-243`), then 5 s, then About.
- Unused: `sharedLevelOverLayer`, `initWithGameLayer:`, `showHighScore:` (`:38-54,77-84,464`).

Unity mapping: one HUD Canvas prefab (TextMeshPro for score, timer, vitamins and messages; an Image row for hearts; a filled Image for the ability bar) driven by C# events from GameFlow/Character/Score systems instead of direct calls. Run the message sequences in a coroutine on scaled or unscaled time, chosen on purpose.

---

## 5. Store and in-app purchases

### 5.1 Products (real money). These IDs exist in three places: `StoreLayer.mm:14-17`, `InAppManager.mm:12-15`, `StoreManager.m:11-14`.

| Product ID | Displayed title | Displayed price (hard-coded, `StoreLayer.mm:129-132`) | Apple tier (inferred, 2013 USD) | Grants (`StoreLayer.mm:267-277`) |
|---|---|---|---|---|
| `com.antiBacteria.vitamins.1` | "10 000 Vitamins" | "$ 0.99" | Tier 1 | 10,000 vitamins |
| `com.antiBacteria.vitamins.2` | "30 000 Vitamins" | "$ 1.99" | Tier 2 | 30,000 vitamins |
| `com.antiBacteria.vitamins.3` | "100 000 Vitamins" | "$ 3.99" | Tier 4 | 100,000 vitamins |
| `com.antiBacteria.vitamins.4` | "1 000 000 Vitamins" | "$ 9.99" | Tier 10 | 1,000,000 vitamins |

- All four are consumables. The bundle ID is `com.Abelix.AntiBacteria` (`Info.plist`).
- Prices are hard-coded text, not taken from StoreKit (so not localised). Titles get "Витаминов" hard-coded in Russian, otherwise `_VITAMINS` (`StoreLayer.mm:107-121`).

### 5.2 `StoreLayer` screen and purchase flow (the only code path that runs)
- Layout (`StoreLayer.mm:71-187`):
  - wallet label "`_VITAMINS` N " (Times New Roman 40ar, green) at (w/2, h/2 + 350ar);
  - 4 rows at y = h/2 + h/8 - i*80ar, each with:
    - a title (Times New Roman 40ar, left-aligned 500ar box centred at x = w/2 - w/8, green);
    - a `BuyButton.png` (scale ar) at (w/2 + w/6, rowY + buttonH/4);
    - a price (right-aligned 500ar box centred at (w/2 + w/8, rowY), green).
- Click (mouse hit-test, only if the button opacity is 255) calls `buyProductWithIndentifier:`. That sends an `SKProductsRequest` for that one ID; the response calls `addPayment:` for each product returned (`:189-252`).
- The transaction observer is the `StoreLayer` itself, added in `init` (`:41`):
  - `Purchased`: **`vitaminsCount = vitaminsToAdd`** then save, then finish (`:263-285`).
  - `Failed`: finish.
  - `Restored`: finish, then falls through to `default` (`:286-297`).
- **QUIRK / BUG:** a purchase **replaces** the wallet instead of adding to it (`StoreLayer.mm:279`). A player with 50,000 vitamins who buys the 10,000 pack ends up with 10,000.
- **QUIRK:** the wallet label is never refreshed after a purchase. There is no busy or feedback UI: `showMenu:` is never called, and `loadingLabel` is never created, so it is nil. Repeated clicks queue repeated requests.
- **QUIRK / crash risk:** the observer is never removed (removal is commented out in `dealloc`, `:319`). Each visit adds another observer, and StoreKit keeps pointers to freed `StoreLayer`s.
- **Receipt validation: none.** No local or server check of the transaction or the App Store receipt. The launch-time receipt check is commented out (`AppDelegate.mm:82-85`). `Resources/receipt` is a bundled DER PKCS#7 file (a sandbox/test App Store receipt, inferred) and nothing references it.
- There is no "restore purchases" button; consumables cannot be restored anyway.

### 5.3 `InAppManager` (dead)
- `startGettingProducts` requests all 4 IDs, builds `StoreProductData` (id, description, localised price via `NSNumberFormatter` with `product.priceLocale`) and posts the notification `"downloadedProducts"` (`InAppManager.mm:49-90`).
- Its observer only finishes Purchased and Restored transactions. Failed transactions are **not** finished; they call `storeLayer showMenu:YES` (`:107-146`).
- `buyProductWithIdentifier:` builds an `SKMutablePayment` from a bare product ID (`:153-159`).
- Its only call site is commented out (`StoreLayer.mm:185`). Nothing else uses it.

### 5.4 `StoreManager` (dead)
- A singleton that registers as transaction observer in `init` (`StoreManager.m:36`), requests a product and adds a payment (`:41-58`). Its Purchased branch is empty for all 4 IDs, so **it grants nothing** (`:67-82`). It is never referenced.

### 5.5 `NSData+AES256` - what is encrypted and with what key
- Algorithm: `CCCrypt(kCCAlgorithmAES128, kCCOptionPKCS7Padding, key, kCCKeySizeAES256, IV=NULL)` (`NSData+AES256.m:50-55,88-93`). The 32-byte key makes this AES-256. CBC mode with an all-zero IV (no ECB flag), PKCS7 padding.
- **Key source:** the Mac's hardware `IOPlatformUUID` string (`getSystemUUID`, `:14-26`). It is copied with `getCString:maxLength:33` into a zeroed 33-byte buffer (`:35-39`).
  - **QUIRK (inferred, verify on the Mac before relying on it):** the UUID string is 36 characters and does not fit, so `getCString:` returns NO. The resulting key is undefined: likely 32 zero bytes or a 32-character prefix.
  - Either way the data is tied to one machine. Copying the preferences to another Mac makes every decrypt fail, which reads as 0/NO (wallet 0, sound and SFX **off**, because `isfirsttime` is stored unencrypted and stays 1).
  - Also leaks the IORegistry object and CFString on each call (`:20-25`).
- Stored in NSUserDefaults (`~/Library/Preferences/com.Abelix.AntiBacteria.plist`):

  | Owner | Key | Content before encryption |
  |---|---|---|
  | `GameSettingsManager.mm:15-22,153-186` | `soundenabled`, `sfxenabled`, `gamemode` (= difficulty), `openedgamemodes` (default 1, otherwise unused), `controltype`, `isautofireEnabled`, `vitamins` | `NSKeyedArchiver` of an `NSNumber` |
  | `GameSettingsManager.mm:15,65,99` | `isfirsttime` | **plain** NSNumber 1 (not encrypted) |
  | `LocalizationManager.mm:12,76-83` | `currentlocalization` | archived NSNumber |
  | `CharacterPropertiesManager.mm:13-15,63-75` | `shoottype`, `immortality`, `doublespeed` | archived NSNumber |
  | `CharacterUpgradeLayer.mm:333,391,441` | the six `*isboughtkey` keys (plus "" for row 0) | raw UTF-8 of the constant `*isboughtvalue` string |
  | (not AES) `LevelManager.mm:80-121`, `ScoreManager.mm:46-133` | hashed `levelisunlocked<N>` keys; `level_%d_score`; `compaign_high_score` | MPSecureUserDefaults: value plus hash with secret `ms3_ser_202$` |

- **QUIRK:** `ScoreManager.saveScore:forLevel:` writes `level_score_%d` (`ScoreManager.mm:49,54`), while load and sync use `level_%d_score` (`:93,131`). The first method is unused.

### 5.6 What to remove or replace for a premium Steam/console release
- **Remove:**
  - `StoreLayer`, `InAppManager`, `StoreManager`, StoreKit;
  - the "Vitamin Store" main-menu item;
  - `Resources/receipt` and the commented receipt check;
  - all `NSData+AES256` usage and the MPSecureUserDefaults secret (Unity save files: JSON/binary in `persistentDataPath`, Steam Cloud, or console save APIs; an optional checksum if tamper-resistance is wanted);
  - the NSAlert dialog.
- **Keep, but rebalance:** vitamins as an earned-only soft currency and the Character Upgrade shop. Prices were set next to paid packs (Immortality 1,000,000 = the $9.99 pack). Re-tune them against the vitamins earned per level, or turn Immortality and Double Speed into assist-mode toggles.
- **Do not port** old Mac saves; the key is machine-bound (see above).

---

## 6. Settings persistence summary
- Settings are persisted by `GameSettingsManager` (loads on first access; defaults on first run at `GameSettingsManager.mm:74-119`) and `LocalizationManager`. See the table in 3.6 for ranges and defaults, and 5.5 for storage format.
- Every change is saved immediately with `synchronize`.
- Unity: one `Settings` ScriptableObject or POCO, saved as JSON, with the same fields. Drop `openedgamemodes`. Add volume levels instead of on/off, plus display and binding options.

---

## 7. Game Center (and mapping to Steam)

### 7.1 Which copy is compiled
The Xcode project is not included; this is inferred from API use.
- **Compiled:** `Managers/GameCenter/GameCenterManager.{h,m}` (written for this game, July 2013). It is the only copy with `+sharedManager`, `loadLeaderboards`, `authenticateLocalPlayer` and `window`, which are exactly the methods `MainMenuLayer.mm:122` calls (and `AppDelegate.mm:79-80` in commented code). It uses Mac GameKit (`GKDialogController`).
- **Not compiled:**
  - `GameCenter/GameCenterManager.{h,m}` + `GameCenter/AppSpecificValues.h`, and `Managers/GameCenterManager/GameCenterManager.{h,mm}` + `AppSpecificValues.h`, are Apple's 2010 iOS GKTapper sample. They call `[UIDevice currentDevice]` (`Managers/GameCenterManager/GameCenterManager.mm:132`), which does not exist on macOS. They declare a class with the same name, `GameCenterManager`, which would clash at link time.
  - `Managers/GameCenterManager/LeaderBoard/LeaderboardViewController.{h,m,xib}` is a `UIViewController` from the iOS game "Turtle" (`LeaderboardViewController.h:12`). It imports `AppSettings.h`, which is not in the repo (`.m:12`).
- **How the two sample copies differ:**
  - `GameCenterManager.h`: identical.
  - `.m` vs `.mm`: identical apart from blank lines replaced by `//` (diff lines 95, 115-116, 129, 134).
  - `AppSpecificValues.h`: the root copy has Apple's sample IDs. Leaderboards `com.appledts.EasyTapList`, `com.appledts.HardTapList`, `com.appledts.AwesomeTapList`; achievements `com.appletest.one_tap`, `com.appledts.twenty_taps`, `com.appledts.one_hundred_taps` (`GameCenter/AppSpecificValues.h:57-65`). The Managers copy has only `kLeadBoardID @"com.fallingdown.highscore"` (from another game) (`Managers/GameCenterManager/AppSpecificValues.h:57`). None of these belong to AntiBacteria.

### 7.2 Compiled manager behaviour (`Managers/GameCenter/GameCenterManager.m`)
- `authenticateLocalPlayer` (`:29-37`) is **never called** (commented out at `AppDelegate.mm:80`).
- `loadLeaderboards` (`:39-52`) is called by the main-menu "LeaderBoards" item. It loads leaderboards, then presents a `GKGameCenterViewController` through `GKDialogController` with `parentWindow = _window`. **QUIRK:** `window` is never set (the assignment is commented out at `AppDelegate.mm:79`), so the dialog has no parent window and the player is not authenticated.
- `reportScore:forLeaderboardID:` (`:60-69`) is **never called** anywhere.
- `ScoreManager` also has GameKit code (`authenticateLocalPlayer`, `showGameCenterOnWindow:`, `ScoreManager.mm:59-114`). Its only call sites are commented out. Its campaign sync has a placeholder comment "upload compaignscore for gamecenter" (`ScoreManager.mm:99`).

### 7.3 IDs that actually belong to the game

| ID | Defined | Reported when | Notes |
|---|---|---|---|
| Leaderboard `com.AntiBacteria.CompaignMode` | `ConfigConstants.h:27` | never | The natural value is `ScoreManager`'s `compaign_high_score` (sum of best per-level raw scores, updated on normal campaign wins, `ScoreManager.mm:80-103`) |
| Leaderboard `com.AntiBacteria.CaptureTheFlag` | `ConfigConstants.h:28` | never | No per-mode high score is tracked |
| Leaderboard `com.AntiBacteria.PeaceWalker` | `ConfigConstants.h:29` | never | " |
| Leaderboard `com.AntiBacteria.Rush` | `ConfigConstants.h:30` | never | " |
| Achievements | none | - | No achievement IDs exist for this game |

Steam mapping suggestion (new design, not in the original):
- 4 Steam leaderboards with the same names: campaign total score; Capture The Flag, Peace Walker and Rush final scores, submitted at the game-over or time-out screen.
- Decide whether to submit the HUD score (multiplied by difficulty+1) or the raw ScoreManager score. The two currently differ.
- Achievements must be designed from scratch. Natural candidates from existing events: beat each boss level (10/20/30/40/50), finish the game, finish on Hard/Impossible, buy each upgrade, collect N vitamins.
- Wrap all of this behind a platform-services interface (Steamworks.NET / console SDKs).

---

## 8. Quirks summary (for keep/fix decisions)
1. **QUIRK:** buying vitamins **sets** the wallet instead of adding (`StoreLayer.mm:279`). Moot if the IAP is removed.
2. **QUIRK:** the UI scale `ar` depends on physical display width, and HUD text scales by ar² (section 0.1).
3. **QUIRK:** all menus are mouse-only. ESC is the only keyboard input outside gameplay; there is no gamepad code.
4. **QUIRK:** "Music" setting does nothing (no music is ever played). Control-type labels do not describe the real controls.
5. **QUIRK:** About is a blank screen, and also the end-of-game screen. Level-select icons have no numbers.
6. **QUIRK:** "Armagedon" mode is built but hidden (`ChooseGameModeLayer.mm:79-81`).
7. **QUIRK:** there is no Get Ready on Restart, and the player can act during the 5 s "You Won" interlude. Two 5 s timers race at level change.
8. **QUIRK:** NSTimers (real time) and cocos actions (scaled time) are mixed. Slow motion stretches some UI timings and not others. Peace Walker's clock is frame-based.
9. **QUIRK:** the slow-motion tint shows for every special ability. There is no boss health bar.
10. **QUIRK:** save data is AES-encrypted with a machine-bound key (possibly an all-zero key because the UUID is truncated). Game Center is wired up but nothing is ever authenticated or reported.
11. **QUIRK:** the StoreKit observer leaks with a dangling pointer; subclasses skip `[super onExit]`; `replaceScene` is given layers instead of scenes in two places (`ChooseGameModeLayer.mm:98`, `GameManager.mm:251`).
12. **QUIRK:** formatted strings use localised text as the format string (`LevelOverLayer.mm:384`, `CharacterUpgradeLayer.mm:158`, `StoreLayer.mm:86`).

## 9. Unity mapping hints (area-level)
- **Screens:** one UI Toolkit or uGUI Canvas per screen, either in a single Menu scene with a `ScreenStack` (push/pop gives the Back behaviour) or as separate scenes. Set the EventSystem's first-selected object on every screen, use explicit navigation for grids, and use Input System UI actions (Submit / Cancel = A/B or Enter/Esc).
- **Gameplay:** a `GameFlowController` state machine plus a HUD prefab listening to events (`ScoreChanged`, `LivesChanged`, `AbilityBarChanged`, `VitaminsChanged`, `ShowMessage`). Pause uses `Time.timeScale = 0` and a pause panel with Resume focused.
- **Platform:** an `IPlatformServices` interface (achievements, leaderboards, cloud save, overlay) with Steam and console implementations. No IAP module; settings and saves are plain serialised data.
