using AntiBacteria.Core;
using AntiBacteria.Rendering;
using AntiBacteria.UI.Menu;
using TMPro;
using UnityEngine;

namespace AntiBacteria.UI.Menus
{
    /// <summary>
    /// Campaign level select + difficulty. Port of ChooseLevelLayer (Docs/DesignReference/05, section 3.4) with the
    /// 2013 gaps filled: level numbers, locked / cleared states, boss column, keyboard and gamepad navigation.
    /// </summary>
    public static class LevelSelectMenu
    {
        private const float GridX = 47f;   // 10 cells x 20 px + 9 gaps x 3 px = 227 px, centred
        private const float GridY = 28f;

        public static MenuScreen Build(UISkin skin, Transform parent, MenuNavigator nav)
        {
            MenuScreen s = MenuScreen.Create(parent, "LevelSelect");
            Transform t = s.transform;
            s.ResetSelectionOnShow = true;

            LiveText.Create(skin, t, "Title", 160f, 8f, () => Loc.Get(WordIDs.ChooseLevel).ToUpperInvariant(), role: PaletteRole.Accent);

            // World numbers to the left of each row.
            for (int r = 0; r < LevelGrid.Rows; r++)
            {
                int world = r + 1;
                LiveText.Create(skin, t, "World" + world, GridX - 9f, GridY + r * (LevelGrid.CellH + LevelGrid.Gap),
                    () => world.ToString(), dim: true);
            }

            LevelGrid grid = s.Add(LevelGrid.Create(skin, t, GridX, GridY, Play));
            LiveText info = LiveText.Create(skin, t, "Info", 160f, 126f, () => Info(grid.SelectedLevel));
            grid.CursorMoved = info.Refresh;

            s.Add(DifficultyRow.Create(skin, t, 144f, () => { grid.Refresh(); info.Refresh(); }, () => grid.Submit()));
            s.Add(MenuButton.Create(skin, t, "Back", 160f, 162f, () => Loc.Get(WordIDs.Back), nav.Back));

            s.OnShown = () => grid.SetCursor(Progress.NextToPlay());
            return s;
        }

        private static void Play(int level) => SceneFlow.LoadLevel(level);

        /// <summary>"Level 2-3   Best 1250", "Level 1-10   Boss", "Level 3-4   Locked".</summary>
        private static string Info(int level)
        {
            int world = (level - 1) / LevelGrid.Columns + 1, inWorld = (level - 1) % LevelGrid.Columns + 1;
            string s = Loc.Get(WordIDs.Level) + " " + world + "-" + inWorld;
            if (!Progress.IsUnlocked(level)) return s + "   " + Loc.Get(WordIDs.Locked);
            if (inWorld == LevelGrid.Columns) s += "   " + Loc.Get(WordIDs.Boss);
            int best = Progress.Best(level, GameSettings.Difficulty);
            if (best > 0) s += "   " + Loc.Get(WordIDs.Best) + " " + best;
            return s;
        }
    }
}
