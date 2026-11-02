using AntiBacteria.Core;
using AntiBacteria.Rendering;
using AntiBacteria.UI.Menu;
using TMPro;
using UnityEngine;

namespace AntiBacteria.UI.Menus
{
    /// <summary>
    /// Main menu scene: title + main list, game mode, options, about.
    /// Port of MainMenuLayer / ChooseGameModeLayer / SettingsLayer / AboutLayer (Docs/DesignReference/05, section 3),
    /// minus the real-money Vitamin Store and the never-working Leaderboards.
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        [SerializeField] private UISkin skin;
        [SerializeField] private MenuNavigator navigator;
        [Tooltip("The 320x180 area everything is laid out in.")]
        [SerializeField] private RectTransform root;

        private MenuScreen main, modes, levelSelect, options, about, upgrade, difficulty;
        // What the difficulty screen will start: an endless mode, or (Campaign) the level picked on the map.
        private GameMode pendingMode = GameMode.CaptureFlag;
        private int pendingLevel = 1;

        public MenuScreen Main => main;
        public MenuScreen Modes => modes;
        public MenuScreen LevelSelect => levelSelect;
        public MenuScreen Options => options;
        public MenuScreen About => about;
        public MenuScreen Upgrade => upgrade;

        private void Start()
        {
            MenuBackdrop backdrop = MenuBackdrop.Create(skin, root);
            main = BuildMain();
            modes = BuildModes();
            difficulty = BuildDifficulty();
            PromptBar.Create(skin, root, navigator); // [A] Select  [B] Back, on every screen
            levelSelect = LevelSelectMenu.Build(skin, root, navigator, level =>
            {
                pendingMode = GameMode.Campaign;
                pendingLevel = level;
                navigator.Push(difficulty);
            });
            options = OptionsMenu.Build(skin, root, navigator, false);
            about = BuildAbout();
            upgrade = UpgradeMenu.Build(skin, root, navigator);
            upgrade.OnShown = () => backdrop.ShowBacteria(false); // the rows use the full width
            main.OnShown = modes.OnShown = about.OnShown = () => backdrop.ShowBacteria(true);
            difficulty.OnShown = () =>
            {
                backdrop.ShowBacteria(true);
                difficulty.DefaultIndex = (int)GameSettings.Difficulty; // start on the last one played
            };
            options.OnShown = () => backdrop.ShowBacteria(false);
            System.Action levelShown = levelSelect.OnShown;
            levelSelect.OnShown = () => { backdrop.ShowBacteria(false); levelShown?.Invoke(); };
            navigator.Show(main);
        }

        private MenuScreen BuildMain()
        {
            MenuScreen s = MenuScreen.Create(root, "Main");
            s.CanGoBack = false;
            BuildLogo(s.transform, 14f);

            float y = 66f;
            s.Add(MenuButton.Create(skin, s.transform, "StartGame", 160f, y, () => Loc.Get(WordIDs.StartGame), () => navigator.Push(modes)));
            y += 16f;
            s.Add(MenuButton.Create(skin, s.transform, "Upgrade", 160f, y, () => Loc.Get(WordIDs.UpgradeCharacter), () => navigator.Push(upgrade)));
            y += 16f;
            s.Add(MenuButton.Create(skin, s.transform, "Options", 160f, y, () => Loc.Get(WordIDs.GameOptions), () => navigator.Push(options)));
            y += 16f;
            s.Add(MenuButton.Create(skin, s.transform, "About", 160f, y, () => Loc.Get(WordIDs.About), () => navigator.Push(about)));
            y += 16f;
            if (GameSettings.HasDisplayOption) // no Quit on consoles
                s.Add(MenuButton.Create(skin, s.transform, "Quit", 160f, y, () => Loc.Get(WordIDs.Quit), SceneFlow.Quit));

            LiveText version = LiveText.Create(skin, s.transform, "Version", 4f, 162f, () => "v" + Application.version,
                HorizontalAlignmentOptions.Left, 1, PaletteRole.Foreground, true);
            // Bottom-left corner of the whole screen, at any aspect ratio (not of the 320x180 menu frame):
            // 3 px from both edges. The text's baseline sits 4 px above the bottom of its row, hence the -1.
            PinToScreenCorner.Pin((RectTransform)version.transform, Vector2.zero, new Vector2(3f, -1f));
            return s;
        }

        /// <summary>"ANTI" in accent + "BACTERIA" in foreground, 2x size, centred.</summary>
        private void BuildLogo(Transform parent, float y)
        {
            TextMeshProUGUI anti = UIFactory.Text(skin, parent, "LogoAnti", 0f, y, HorizontalAlignmentOptions.Left, 2, PaletteRole.Accent);
            TextMeshProUGUI rest = UIFactory.Text(skin, parent, "LogoBacteria", 0f, y, HorizontalAlignmentOptions.Left, 2);
            anti.text = "ANTI";
            rest.text = "BACTERIA";
            // Measured when the screen opens (TextMeshPro reports 0 width while hidden).
            RefreshHook.Add(anti.gameObject, () =>
            {
                // Where BACTERIA starts inside the whole word, from the font's own layout: measuring "ANTI" alone
                // rounded its width up and put BACTERIA one game pixel too far right.
                float total = rest.GetPreferredValues("ANTIBACTERIA").x;
                float split = Mathf.Round(total - rest.GetPreferredValues("BACTERIA").x);
                float x0 = Mathf.Round(160f - total / 2f);
                anti.rectTransform.anchoredPosition = new Vector2(x0, -y);
                rest.rectTransform.anchoredPosition = new Vector2(x0 + split, -y);
            });
        }

        private MenuScreen BuildModes()
        {
            MenuScreen s = MenuScreen.Create(root, "Modes");
            Transform t = s.transform;
            LiveText.Create(skin, t, "Title", 160f, 22f, () => Loc.Get(WordIDs.ChooseGame).ToUpperInvariant(), role: PaletteRole.Accent);
            UIFactory.Image(t, "Rule", skin.pixel, 110f, 40f, 101f, 1f, PaletteRole.Foreground, true);

            float y = 56f;
            s.Add(MenuButton.Create(skin, t, "Campaign", 160f, y, () => Loc.Get(WordIDs.Campaign), () => navigator.Push(levelSelect)));
            GameMode[] endless = { GameMode.Roguelike, GameMode.CaptureFlag, GameMode.Rush, GameMode.PeaceWalker };
            foreach (GameMode m in endless)
            {
                y += 16f;
                GameMode mode = m;
                WordIDs id = (WordIDs)System.Enum.Parse(typeof(WordIDs), m.ToString());
                // Endless modes: pick the difficulty on the next screen (the campaign picks it in level select).
                s.Add(MenuButton.Create(skin, t, m.ToString(), 160f, y, () => Loc.Get(id), () =>
                {
                    pendingMode = mode;
                    navigator.Push(difficulty);
                }));
            }
            s.Add(MenuButton.Create(skin, t, "Back", 160f, 140f, () => Loc.Get(WordIDs.Back), navigator.Back));
            return s;
        }

        /// <summary>After an endless mode: Normal / Hard / Impossible one under the other, each with its best score; picking one starts the run.</summary>
        private MenuScreen BuildDifficulty()
        {
            MenuScreen s = MenuScreen.Create(root, "Difficulty");
            Transform t = s.transform;
            s.ResetSelectionOnShow = true;
            bool campaign() => pendingMode == GameMode.Campaign;
            LiveText.Create(skin, t, "Title", 160f, 22f, () => campaign()
                ? LevelSelectMenu.Title(pendingLevel, true)
                : GameplayMenuController.ModeName(pendingMode).ToUpperInvariant(), role: PaletteRole.Accent);
            UIFactory.Image(t, "Rule", skin.pixel, 110f, 40f, 101f, 1f, PaletteRole.Foreground, true);
            // Campaign: the world's name and "Difficulty"; endless modes: "Difficulty".
            LiveText.Create(skin, t, "Subtitle", 160f, 44f, () => campaign()
                ? Worlds.Name(Worlds.Of(pendingLevel)) + "  " + Loc.Get(WordIDs.Difficulty)
                : Loc.Get(WordIDs.Difficulty), dim: true);
            WordIDs[] names = { WordIDs.Normal, WordIDs.Hard, WordIDs.Impossible };
            for (int i = 0; i < names.Length; i++)
            {
                var d = (Difficulty)i;
                WordIDs w = names[i];
                float y = 64f + i * 16f;
                s.Add(MenuButton.Create(skin, t, w.ToString(), 160f, y, () => Loc.Get(w), () =>
                {
                    GameSettings.SetDifficulty(d);
                    if (campaign()) SceneFlow.LoadLevel(pendingLevel);
                    else SceneFlow.LoadMode(pendingMode);
                }));
                LiveText.Create(skin, t, "Best" + i, 214f, y, () =>
                {
                    int best = campaign() ? Progress.Best(pendingLevel, d) : Progress.ModeBest(pendingMode, d);
                    return best > 0 ? Loc.Get(WordIDs.Best) + " " + best : "";
                }, HorizontalAlignmentOptions.Left, dim: true);
            }
            // What the highlighted difficulty means and what it pays: the reason to pick a harder one.
            System.Func<Difficulty?> highlighted = () =>
            {
                int i = s.SelectedItem != null ? s.Items.IndexOf(s.SelectedItem) : -1;
                return i >= 0 && i < 3 ? (Difficulty)i : (Difficulty?)null;
            };
            LiveText desc = LiveText.Create(skin, t, "Description", 160f, 118f,
                () => highlighted() is Difficulty d ? DifficultySettings.Description(d) : "", dim: true);
            LiveText rewards = LiveText.Create(skin, t, "Rewards", 160f, 130f,
                () => highlighted() is Difficulty d ? DifficultySettings.Rewards(d) : "", role: PaletteRole.Accent);
            SelectionWatcher.Watch(s, () => { desc.Refresh(); rewards.Refresh(); });
            s.Add(MenuButton.Create(skin, t, "Back", 160f, 150f, () => Loc.Get(WordIDs.Back), navigator.Back));
            return s;
        }

        private MenuScreen BuildAbout()
        {
            MenuScreen s = MenuScreen.Create(root, "About");
            Transform t = s.transform;
            LiveText.Create(skin, t, "Title", 160f, 14f, () => Loc.Get(WordIDs.About).ToUpperInvariant(), role: PaletteRole.Accent);
            LiveText.Create(skin, t, "Game", 160f, 40f, () => "AntiBacteria");
            LiveText.Create(skin, t, "CreatedBy", 160f, 56f, () => Loc.Get(WordIDs.CreatedBy) + " " + Application.companyName,
                dim: true);
            LiveText.Create(skin, t, "Original", 160f, 72f, () => Loc.Get(WordIDs.OriginalGame), dim: true);
            LiveText.Create(skin, t, "FontTitle", 160f, 96f, () => Loc.Get(WordIDs.Font) + ": m6x11");
            LiveText.Create(skin, t, "FontBy", 160f, 112f, () => "Daniel Linssen, Natto Games", dim: true);
            s.Add(MenuButton.Create(skin, t, "Back", 160f, 140f, () => Loc.Get(WordIDs.Back), navigator.Back));
            return s;
        }
    }
}
