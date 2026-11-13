using AntiBacteria.Core;
using AntiBacteria.Rendering;
using AntiBacteria.UI.Menu;
using UnityEngine;

namespace AntiBacteria.UI.Menus
{
    /// <summary>The Options screen, used by the main menu and the pause menu.</summary>
    public static class OptionsMenu
    {
        /// <param name="opaque">Cover whatever is behind (the paused game).</param>
        public static MenuScreen Build(UISkin skin, Transform parent, MenuNavigator nav, bool opaque, System.Action watchIntro = null)
        {
            MenuScreen s = MenuScreen.Create(parent, "Options");
            Transform t = s.transform;
            // Oversized: the screen is wider than the 320x180 layout and the backing must hide the whole paused game.
            if (opaque) UIFactory.Image(t, "Backing", skin.pixel, -400f, -300f, 1120f, 780f, PaletteRole.Background);

            LiveText.Create(skin, t, "Title", 160f, 10f, () => Loc.Get(WordIDs.GameOptions).ToUpperInvariant(), role: PaletteRole.Accent);

            // Seven rows fit above the buttons at 14 px apart (16 px without the Display row).
            float step = GameSettings.HasDisplayOption ? 14f : 16f;
            float y = 32f;
            s.Add(MenuOption.Choice(skin, t, "Language", y, () => Loc.Get(WordIDs.Language),
                () => Loc.Get(WordIDs.LanguageValue), Loc.Step));
            y += step;
            // Difficulty is chosen per play on the level select screen.
            s.Add(MenuOption.Toggle(skin, t, "AutoShoot", y, () => Loc.Get(WordIDs.AutoShoot),
                () => GameSettings.AutoShoot, GameSettings.SetAutoShoot));
            y += step;
            s.Add(MenuOption.Toggle(skin, t, "ScreenShake", y, () => Loc.Get(WordIDs.ScreenShake),
                () => GameSettings.ScreenShake, GameSettings.SetScreenShake));
            y += step;
            // The palettes as a row of colour swatches (no names): the frame marks the one in use.
            s.Add(PaletteSwatches.Create(skin, t, "Palette", y, () => Loc.Get(WordIDs.Palette),
                () => Palettes() != null ? Palettes().Count : 0,
                i => Palettes() != null ? Palettes().GetPalette(i) : null,
                CurrentPalette, SetPalette));
            y += step;
            if (GameSettings.HasDisplayOption)
            {
                s.Add(MenuOption.Choice(skin, t, "Display", y, () => Loc.Get(WordIDs.DisplayMode),
                    () => Loc.Get(GameSettings.Fullscreen ? WordIDs.Fullscreen : WordIDs.Windowed),
                    _ => GameSettings.SetFullscreen(!GameSettings.Fullscreen)));
                y += step;
            }
            s.Add(MenuOption.Bar(skin, t, "Sound", y, () => Loc.Get(WordIDs.SoundSfx),
                () => GameSettings.SfxVolume, GameSettings.SetSfxVolume));
            y += step;
            s.Add(MenuOption.Bar(skin, t, "Music", y, () => Loc.Get(WordIDs.GameMusic),
                () => GameSettings.MusicVolume, GameSettings.SetMusicVolume));

            MenuScreen controls = ControlsMenu.Build(skin, parent, nav, opaque);
            if (watchIntro == null)
            {
                s.Add(MenuButton.Create(skin, t, "Controls", 160f, 134f, () => Loc.Get(WordIDs.Controls), () => nav.Push(controls)));
                s.Add(MenuButton.Create(skin, t, "Back", 160f, 150f, () => Loc.Get(WordIDs.Back), nav.Back));
            }
            else
            {
                s.Add(MenuButton.Create(skin, t, "Controls", 160f, 130f, () => Loc.Get(WordIDs.Controls), () => nav.Push(controls)));
                s.Add(MenuButton.Create(skin, t, "WatchIntro", 160f, 143f, () => Loc.Get(WordIDs.WatchIntro), watchIntro));
                s.Add(MenuButton.Create(skin, t, "Back", 160f, 156f, () => Loc.Get(WordIDs.Back), nav.Back));
            }
            return s;
        }

        public static WordIDs DifficultyWord(Difficulty d) =>
            d == Difficulty.Hard ? WordIDs.Hard : d == Difficulty.Impossible ? WordIDs.Impossible : WordIDs.Normal;

        private static PaletteManager Palettes() => Object.FindFirstObjectByType<PaletteManager>();

        private static int CurrentPalette()
        {
            var pm = Palettes();
            return pm != null ? pm.CurrentIndex : GameSettings.PaletteIndex;
        }

        private static void SetPalette(int index)
        {
            var pm = Palettes();
            if (pm == null) return;
            pm.SetPalette(index);
            GameSettings.SetPaletteIndex(pm.CurrentIndex);
        }
    }
}
