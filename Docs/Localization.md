# Localization

The game's texts live in **Gley Localization**:

- Data: `Unity/AntiBacteria_Unity/Assets/GleyPlugins/Localization/Resources/LocalizationFile.json`
- Generated enums: `GleyPlugins/Localization/Scripts/WordIDs.cs` and `SupportedLanguages.cs`
- Edit texts in **Window → Gley → Localization**. Its Save button rewrites all three files, so never edit them by hand.

Use them in code with `GleyLocalization.Manager.GetText(WordIDs.StartGame)`. For a UI label, add
`TMProUGUILocalizationComponent` to a TextMeshPro text and pick the word ID; it updates when the language changes.

## Languages

| Language | `SupportedLanguages` value (Unity `SystemLanguage`) | Index in the 2013 game |
|---|---|---|
| English (default) | 10 | 0 |
| French | 14 | 2 |
| German | 15 | 3 |
| Russian | 30 | 1 |
| Spanish | 34 | 4 |

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
