using System;
using AntiBacteria.Core;
using AntiBacteria.Rendering;
using AntiBacteria.UI.Menu;
using UnityEngine;

namespace AntiBacteria.UI.Menus
{
    /// <summary>
    /// Campaign level select, one world at a time: "< WORLD 2 >", the world's name, its map of 9 levels and a boss,
    /// an info line and Back. Picking a level opens the difficulty screen (the same one the endless modes use),
    /// which starts the level. Opens on the next level to play.
    /// Port of ChooseLevelLayer (Docs/DesignReference/05, section 3.4), reorganised into named worlds.
    /// </summary>
    public static class LevelSelectMenu
    {
        /// <param name="onPick">Called with the level number when an open level is chosen.</param>
        public static MenuScreen Build(UISkin skin, Transform parent, MenuNavigator nav, Action<int> onPick)
        {
            MenuScreen s = MenuScreen.Create(parent, "LevelSelect");
            Transform t = s.transform;
            s.ResetSelectionOnShow = true;
            s.DefaultIndex = 1; // the map

            WorldMap map = null;
            WorldPicker picker = s.Add(WorldPicker.Create(skin, t, 6f,
                () => Loc.Get(WordIDs.World).ToUpperInvariant() + " " + (map != null ? map.World : 1),
                dir =>
                {
                    int w = map.World + dir;
                    if (w < 1 || w > Worlds.Count) return false;
                    map.SetWorld(w, 0);
                    return true;
                }));

            LiveText worldName = LiveText.Create(skin, t, "WorldName", 160f, 20f,
                () => map != null ? Worlds.Name(map.World).ToUpperInvariant() : "", scale: 2, role: PaletteRole.Accent, maxWidth: 300f);
            LiveText progress = LiveText.Create(skin, t, "WorldProgress", 160f, 44f, () => WorldLine(map), dim: true);

            map = s.Add(WorldMap.Create(skin, t, 160f - WorldMap.Width / 2f, 62f, skin.bossMark, onPick));
            LiveText info = LiveText.Create(skin, t, "Info", 160f, 124f, () => Info(map.SelectedLevel));
            map.Changed = () => { info.Refresh(); worldName.Refresh(); progress.Refresh(); picker.Refresh(); };

            s.Add(MenuButton.Create(skin, t, "Back", 160f, 146f, () => Loc.Get(WordIDs.Back), nav.Back));

#if UNITY_EDITOR
            // Testing aid, compiled out of builds: open every level without touching the save.
            s.Add(MenuOption.CompactToggle(skin, t, "UnlockAll", 4f, 162f, 110f, () => "Unlock all levels",
                () => Progress.UnlockAll, on => { Progress.UnlockAll = on; map.Refresh(); map.Changed?.Invoke(); }));
#endif

            s.OnShown = () => map.ShowLevel(Progress.NextToPlay());
            map.ShowLevel(1);
            return s;
        }

        /// <summary>"7/10   Gold 4/30", or "Locked" for a world whose first level isn't open yet.</summary>
        private static string WorldLine(WorldMap map)
        {
            if (map == null) return "";
            if (!Worlds.IsOpen(map.World)) return Loc.Get(WordIDs.Locked);
            return Worlds.ClearedOnAny(map.World) + "/" + Worlds.LevelsPerWorld + "   " + Loc.Get(WordIDs.MedalGold) + " " +
                   Progress.GoldMedals(map.World) + "/" + Worlds.LevelsPerWorld * Progress.DifficultyCount;
        }

        /// <summary>"Level 2-3", "Boss: Inferno", "Level 3-4   Locked".</summary>
        private static string Info(int level)
        {
            string s = Title(level, false);
            if (!Progress.IsUnlocked(level)) return s + "   " + Loc.Get(WordIDs.Locked);
            Medal m = Progress.BestMedal(level); // best on any difficulty; each difficulty's is on the next screen
            return m == Medal.None ? s : s + "   " + Medals.Name(m);
        }

        /// <summary>"Level 2-3" / "Boss: Inferno" (upper case for screen titles).</summary>
        public static string Title(int level, bool upper)
        {
            int world = Worlds.Of(level);
            string s = Worlds.IsBossLevel(level)
                ? Loc.Get(WordIDs.Boss) + ": " + Worlds.BossName(world)
                : Loc.Get(WordIDs.Level) + " " + world + "-" + Worlds.IndexInWorld(level);
            return upper ? s.ToUpperInvariant() : s;
        }
    }
}
