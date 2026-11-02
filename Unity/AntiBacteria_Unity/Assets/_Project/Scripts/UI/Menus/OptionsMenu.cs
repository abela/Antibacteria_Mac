using AntiBacteria.Core;
using AntiBacteria.Rendering;
using AntiBacteria.UI.Menu;
using UnityEngine;

namespace AntiBacteria.UI.Menus
{
    /// <summary>The Options screen, used by the main menu and the pause menu.</summary>
    public static class OptionsMenu
    {
        private static readonly WordIDs[] PaletteWords =
            { WordIDs.PaletteClassic, WordIDs.PalettePetriDish, WordIDs.PaletteGramStain, WordIDs.PaletteColourBlind };

        /// <param name="opaque">Cover whatever is behind (the paused game).</param>
        public static MenuScreen Build(UISkin skin, Transform parent, MenuNavigator nav, bool opaque)
        {
            MenuScreen s = MenuScreen.Create(parent, "Options");
            Transform t = s.transform;
            // Oversized: the screen is wider than the 320x180 layout and the backing must hide the whole paused game.
            if (opaque) UIFactory.Image(t, "Backing", skin.pixel, -400f, -300f, 1120f, 780f, PaletteRole.Background);

            LiveText.Create(skin, t, "Title", 160f, 10f, () => Loc.Get(WordIDs.GameOptions).ToUpperInvariant(), role: PaletteRole.Accent);

            float y = 32f;
            s.Add(MenuOption.Choice(skin, t, "Language", y, () => Loc.Get(WordIDs.Language),
                () => Loc.Get(WordIDs.LanguageValue), Loc.Step));
            y += 16f;
            // Difficulty is chosen per play on the level select screen.
            s.Add(MenuOption.Toggle(skin, t, "AutoShoot", y, () => Loc.Get(WordIDs.AutoShoot),
                () => GameSettings.AutoShoot, GameSettings.SetAutoShoot));
            y += 16f;
            s.Add(MenuOption.Choice(skin, t, "Palette", y, () => Loc.Get(WordIDs.Palette),
                () => Loc.Get(PaletteWords[Mathf.Clamp(CurrentPalette(), 0, PaletteWords.Length - 1)]), StepPalette));
            y += 16f;
            if (GameSettings.HasDisplayOption)
            {
                s.Add(MenuOption.Choice(skin, t, "Display", y, () => Loc.Get(WordIDs.DisplayMode),
                    () => Loc.Get(GameSettings.Fullscreen ? WordIDs.Fullscreen : WordIDs.Windowed),
                    _ => GameSettings.SetFullscreen(!GameSettings.Fullscreen)));
                y += 16f;
            }
            s.Add(MenuOption.Bar(skin, t, "Sound", y, () => Loc.Get(WordIDs.SoundSfx),
                () => GameSettings.SfxVolume, GameSettings.SetSfxVolume));
            y += 16f;
            s.Add(MenuOption.Bar(skin, t, "Music", y, () => Loc.Get(WordIDs.GameMusic),
                () => GameSettings.MusicVolume, GameSettings.SetMusicVolume));

            MenuScreen controls = ControlsMenu.Build(skin, parent, nav, opaque);
            s.Add(MenuButton.Create(skin, t, "Controls", 160f, 132f, () => Loc.Get(WordIDs.Controls), () => nav.Push(controls)));
            s.Add(MenuButton.Create(skin, t, "Back", 160f, 152f, () => Loc.Get(WordIDs.Back), nav.Back));
            return s;
        }

        public static WordIDs DifficultyWord(Difficulty d) =>
            d == Difficulty.Hard ? WordIDs.Hard : d == Difficulty.Impossible ? WordIDs.Impossible : WordIDs.Normal;

        private static int CurrentPalette()
        {
            var pm = Object.FindFirstObjectByType<PaletteManager>();
            return pm != null ? pm.CurrentIndex : GameSettings.PaletteIndex;
        }

        private static void StepPalette(int direction)
        {
            var pm = Object.FindFirstObjectByType<PaletteManager>();
            if (pm == null) return;
            pm.SetPalette(pm.CurrentIndex + direction);
            GameSettings.SetPaletteIndex(pm.CurrentIndex);
        }
    }
}
