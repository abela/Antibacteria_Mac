# Localization

The game's texts live in **Gley Localization**:

- Data: `Unity/AntiBacteria_Unity/Assets/GleyPlugins/Localization/Resources/LocalizationFile.json`
- Generated enums: `GleyPlugins/Localization/Scripts/WordIDs.cs` and `SupportedLanguages.cs`
- Edit the JSON directly. **Do not save from Window → Gley → Localization** any more: it would drop Georgian (see below).

Use them in code with `GleyLocalization.Manager.GetText(WordIDs.StartGame)`. For a UI label, add
`TMProUGUILocalizationComponent` to a TextMeshPro text and pick the word ID; it updates when the language changes.

## Languages

15 languages, 200 words each (3,000 translations).

| Language | `SupportedLanguages` value | Font | Added |
|---|---|---|---|
| English (default) | 10 | m6x11 | 2013 |
| French | 14 | m6x11 | 2013 |
| German | 15 | m6x11 | 2013 |
| Russian | 30 | m6x11 Cyrillic | 2013 |
| Spanish (Spain) | 34 | m6x11 | 2013 |
| Italian | 21 | m6x11 (+ `m6x11_extras`) | 2026 |
| Portuguese (Portugal) | 28 | m6x11 (+ `m6x11_extras`) | 2026 |
| Dutch | 9 | m6x11 (+ `m6x11_extras`) | 2026 |
| Polish | 27 | m6x11 (+ `m6x11_extras`) | 2026 |
| Czech | 7 | m6x11 + `m6x11_extras` (carons) | 2026 |
| Georgian | **100** (custom) | `AB_Pixel_KA` | 2026 |
| Japanese | 22 | `AB_Pixel_JA` | 2026 |
| Korean | 23 | `AB_Pixel_KO` | 2026 |
| Chinese (Simplified) | 40 | `AB_Pixel_SC` | 2026 |
| Chinese (Traditional) | 41 | `AB_Pixel_TC` | 2026 |

Values mirror Unity `SystemLanguage`, so the device language is picked automatically on first launch.
`GetDeviceLanguage` was patched so `SystemLanguage.Chinese` picks Traditional for Hant/TW/HK/MO cultures and
Simplified otherwise, and a `ka` culture picks Georgian. The in-game picker order is set in `Loc.cs` (`Languages`).

### Source of truth: the JSON, not the Gley window

From 2026 on, **`LocalizationFile.json` is the source of truth** and is edited by script, not in the Gley window.
Georgian is not a Unity `SystemLanguage`, so it uses the custom value **100**. The Gley window does not know it:
**saving from Window → Gley → Localization would drop Georgian and regenerate `SupportedLanguages.cs` without it.**
Edit the JSON directly (each word has a `translations` list of `{language, word}`), keep `SupportedLanguages.cs`
in sync by hand, and rebuild the fonts after adding new characters.

Draft translation scripts are in `Docs/Localization/Tools/` (`tr_eu.py`, `tr_asia.py`). They are the first drafts:
about 50 strings were shortened later to fit the UI, so do not re-run them over the JSON.

### Fonts for the new languages

The primary TMP font is m6x11 (static atlas, `RASTER_HINTED`, 16 pt). Characters it lacks come from fallback
font assets in `Assets/Fonts/Fallback/`. Each is a pixel-square TTF (1 px = 64 units, em 1024 = 16 px) built by
`Docs/Localization/Tools/build_fonts.py`:

- `m6x11_extras.ttf`: Czech/Polish/Portuguese/Italian/Dutch letters missing from m6x11 (carons etc.), drawn from m6x11 glyphs.
- `AB_Pixel_JA/KO/SC/TC.ttf`: Noto Sans CJK Medium rasterized at 12 px (OFL).
- `AB_Pixel_KA.ttf`: DejaVu Sans rasterized at 14 px.

Then run **AntiBacteria → UI → Build Pixel Font Asset** (`Editor/FontSetup.cs`). It collects every character
used by any translation, builds static `RASTER` (unhinted) atlases for each fallback (16 pt, padding 2, 512², point
filter) and reports anything still missing. It should end with "all covered".

Han characters look different in Japanese, Simplified and Traditional Chinese, so `Loc.ApplyFonts()` moves the
current language's CJK font to the front of the fallback list whenever the language changes.

### Fit rules used when shortening translations

At 16 px, in UI units: perk and rarity names ≤ 92, perk descriptions ≤ 4 lines at 90, option values ≤ 100,
labels ≤ 120, buttons ≤ 180, intro captions 2 lines at 308, everything else ≤ 300. Upgrade-shop rows need the
name + value (`Equipped`/`Owned`/price) to fit in 234 with a gap (Georgian `NormalShoot` and `Equipped` were
shortened for this).

### Before release

The new translations were written without a native-speaker pass. Get each one reviewed, especially Georgian,
Czech, Polish and the three CJK languages, and re-check the screens after any change.

## Imported from the 2013 game

All 53 texts from `Mac/Bacteria/Resources/Localization/localizations.plist` were imported unchanged in all
5 languages (265 translations). The old `#define` keys became readable IDs. Spelling was fixed **in the
IDs only**, never in the texts.

| 2013 key | Gley `WordIDs` | English text |
|---|---|---|
| `_LANGUAGE` | `Language` | 'Language |
| `_LANGUAGE_VALUE` | `LanguageValue` | 'English |
| `_START_GAME` | `StartGame` | 'Start Game |
| `_UPGRADE_CHARACTER` | `UpgradeCharacter` | 'Upgrade Character |
| `_VITAMIN_STORE` | `VitaminStore` | 'Vitamin Store |
| `_LEADERBOARDS` | `Leaderboards` | 'LeaderBoards |
| `_GAME_OPTIONS` | `GameOptions` | 'Game Options |
| `_ABOUT` | `About` | 'About |
| `_QUIT` | `Quit` | 'Quit |
| `_CHOOSE_GAME` | `ChooseGame` | 'Choose Game Mode |
| `_COMPAIGN` | `Campaign` | 'Compaign |
| `_CAPTURE_FLAG` | `CaptureFlag` | 'Capture The Flag |
| `_RUSH` | `Rush` | 'Rush |
| `_PEACE_WALKER` | `PeaceWalker` | 'Peace Walker |
| `_UPGRADE_SOME_FEATURES` | `UpgradeSomeFeatures` | 'Upgrade Some Features |
| `_YOU_HAVE` | `YouHave` | 'You have |
| `_NORMAL_SHOOT` | `NormalShoot` | 'Normal Shoot |
| `_WATER_SHOOT` | `WaterShoot` | 'Water Shoot |
| `_WINDMLL_SHOOT` | `WindmillShoot` | 'Windmill Shoot |
| `_CIRCLE_SHOOT` | `CircleShoot` | 'Circle Shoot |
| `_TORNADO_SHOOT` | `TornadoShoot` | 'Tornado Shoot |
| `_DOUBLE_SPEED` | `DoubleSpeed` | 'Double Speed |
| `_IMMORTALITY` | `Immortality` | 'Immortality |
| `_VITAMINS` | `Vitamins` | 'Vitamins |
| `_DIFFICULTY` | `Difficulty` | 'Difficulty |
| `_NORMAL` | `Normal` | 'Normal |
| `_HARD` | `Hard` | 'Hard |
| `_IMPOSSIBLE` | `Impossible` | 'Impossible |
| `_AUTO_SHOOT` | `AutoShoot` | 'Auto Shoot |
| `_CONTROL_TYPE` | `ControlType` | 'Control Type |
| `_WASD_ARROWS` | `WasdArrows` | 'WASD + ARROWS |
| `_ARROWS` | `OnlyArrows` | 'Only Arrows |
| `_ONLY_MOUSE` | `OnlyMouse` | 'Only Mouse |
| `_SOUND_SFX` | `SoundSfx` | 'Sound SFX |
| `_GAME_MUSIC` | `GameMusic` | 'Music |
| `_CHOOSE_LEVEL` | `ChooseLevel` | 'Choose Level |
| `_RESUME` | `Resume` | 'Resume |
| `_RESTART` | `Restart` | 'Restart |
| `_GOTO_MENU` | `GoToMenu` | 'Go To Menu |
| `_CHOOSE_LEVEL_IN_PAUSE` | `ChooseLevelInPause` | 'Choose Level |
| `_GET_READY` | `GetReady` | 'Get Ready |
| `_LEVEL` | `Level` | 'Level  |
| `_YOU_WON` | `YouWon` | 'You Won |
| `_GET_READY_FOR_BOSS` | `GetReadyForBoss` | 'Get Ready For the Boss |
| `_GAME_OVER` | `GameOver` | 'Game Over |
| `_YOUR_TIME_IS` | `YourTimeIs` | 'Your Time Is |
| `_SLOW_MOTION` | `SlowMotion` | 'Slow Motion |
| `_BOOOM` | `Boom` | 'Boooooom! |
| `_SCORE` | `Score` | 'Score |
| `_NO_VITAMINS` | `NoVitamins` | "You don't have enough Vitamins" |
| `_FINISHED_GAME` | `FinishedGame` | 'You finished the game |
| `_TIME_OFF` | `TimeOff` | 'Time Is Off |
| `_LIFE` | `Life` | '+1 Life |

## Translation problems to fix before release

The 2013 translations look machine-made, and several have typos. They were imported as they were, so fix them in the
Gley window (or send everything for a professional translation pass):

- **Russian:**
  - `Quit` = "Bыход" starts with a **Latin B** (should be Cyrillic В). A pixel font may render it differently.
  - Misspellings:
    - "Настроийки" (Настройки)
    - "Нормалны" (Нормальный)
    - "Двоиная скорось" (Двойная скорость)
    - "Громкосьт" (Громкость)
    - "Время вьшло" (Время вышло)
    - "Мелница" (Мельница)
    - "Круглы" (Круглый)
    - "Безсмертие" (Бессмертие)
    - "Ты Выграл" (Ты выиграл)
    - "Твоя время" (Твоё время)
    - "Неспособны" (Невозможный)
  - `UpgradeSomeFeatures` repeats the text of `UpgradeCharacter`.
- **German:** "Wählen Sie die Ebenèè" (`ChooseLevelInPause`). "Deutsche" should be "Deutsch". Literal translations: "Die Kerbe" for *Score*, "Die Ebene" for *Level*, "Verbessern Charaktern".
- **French:** "Francais" should be "Français". "Rèsume" should be "Reprendre". "Le compte" for *Score* and "Sur" for *About* are literal translations.
- **Spanish:** "fleches" should be "flechas" (`WasdArrows`, `OnlyArrows`). "Juego" for *Start Game*. "Círculo de penaltis" for *Circle Shoot*.
- **English:** "Compaign" should be "Campaign". "LeaderBoards", "Normal Shoot" and the other "… Shoot" names might read better as "… Shot".
- **Trailing spaces are intentional** in `Level` ("Level ", "Уровень ", …): the number is appended in code. In German and Spanish there are two spaces.
- **Texts for removed features:** `VitaminStore`, `NoVitamins` (store), `Leaderboards` (Game Center) and the three control-type
  names (`WasdArrows`, `OnlyArrows`, `OnlyMouse`) will change with the new store-free design and the Rewired controls.
