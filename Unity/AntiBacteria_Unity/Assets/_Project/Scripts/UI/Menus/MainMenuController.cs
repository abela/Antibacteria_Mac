using AntiBacteria.Core;
using AntiBacteria.Rendering;
using AntiBacteria.UI.Intro;
using AntiBacteria.UI.Menu;
using TMPro;
using UnityEngine;

namespace AntiBacteria.UI.Menus
{
    /// <summary>
    /// Main menu scene: title + main list, game mode, options, about.
    /// Port of MainMenuLayer / ChooseGameModeLayer / SettingsLayer / AboutLayer (Docs/DesignReference/05, section 3),
    /// minus the real-money Vitamin Store. Leaderboards show the Steam top 20 (only when a store is running).
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        [SerializeField] private UISkin skin;
        [SerializeField] private MenuNavigator navigator;
        [Tooltip("The 320x180 area everything is laid out in.")]
        [SerializeField] private RectTransform root;

        private MenuScreen main, modes, levelSelect, options, about, upgrade, difficulty, leaderboards, runSetup, daily;
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
            runSetup = BuildRunSetup();
            daily = BuildDaily();
            PromptBar.Create(skin, root, navigator); // [A] Select  [B] Back, on every screen
            levelSelect = LevelSelectMenu.Build(skin, root, navigator, level =>
            {
                pendingMode = GameMode.Campaign;
                pendingLevel = level;
                navigator.Push(difficulty);
            });
            options = OptionsMenu.Build(skin, root, navigator, false, WatchIntro);
            about = BuildAbout();
            upgrade = UpgradeMenu.Build(skin, root, navigator);
            leaderboards = LeaderboardMenu.Build(skin, root, navigator);
            System.Action boardsShown = leaderboards.OnShown;
            leaderboards.OnShown = () => { backdrop.ShowBacteria(false); boardsShown?.Invoke(); }; // the rows use the full width
            upgrade.OnShown = () => backdrop.ShowBacteria(false); // the rows use the full width
            main.OnShown = about.OnShown = () => backdrop.ShowBacteria(true);
            modes.OnShown = () => backdrop.ShowBacteria(false); // the mode descriptions use the full width
            System.Action setupShown = runSetup.OnShown, dailyShown = daily.OnShown;
            runSetup.OnShown = () => { backdrop.ShowBacteria(false); setupShown?.Invoke(); };
            daily.OnShown = () => { backdrop.ShowBacteria(false); dailyShown?.Invoke(); };
            difficulty.OnShown = () =>
            {
                backdrop.ShowBacteria(true);
                difficulty.DefaultIndex = (int)GameSettings.Difficulty; // start on the last one played
            };
            options.OnShown = () => backdrop.ShowBacteria(false);
            System.Action levelShown = levelSelect.OnShown;
            levelSelect.OnShown = () => { backdrop.ShowBacteria(false); levelShown?.Invoke(); };
            if (IntroFlow.WantsLaunchIntro)
            {
                // First launch: the story first, then straight into Level 1 (skipping goes there too).
                MenuNavigator.Blocked = true;
                IntroPlayer.Play(skin, root, skipped =>
                {
                    MenuNavigator.Blocked = false;
                    IntroFlow.MarkShown();
                    IntroFlow.GoToLevelOne();
                });
                return;
            }
            navigator.Show(main);
        }

        /// <summary>Options > Watch Intro: the story again, then back to Options with the menu music.</summary>
        private void WatchIntro()
        {
            MenuNavigator.Blocked = true;
            IntroPlayer.Play(skin, root, skipped =>
            {
                MenuNavigator.Blocked = false;
                IntroFlow.MarkShown();
                AntiBacteria.Audio.Sound.Music(AntiBacteria.Audio.MusicTrack.Menu);
            });
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
            if (AntiBacteria.Platform.PlatformServices.LeaderboardsAvailable)
            {
                s.Add(MenuButton.Create(skin, s.transform, "Leaderboards", 160f, y, () => Loc.Get(WordIDs.Leaderboards), () => navigator.Push(leaderboards)));
                y += 16f;
            }
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

            // Rows a little higher than the other screens to leave two lines under them for the description.
            // Rush is not listed: Capture The Flag plays its rules.
            float y = 46f;
            var described = new System.Collections.Generic.Dictionary<MenuItem, System.Func<int, string>>();
            described[s.Add(MenuButton.Create(skin, t, "Campaign", 160f, y, () => Loc.Get(WordIDs.Campaign),
                () => navigator.Push(levelSelect)))] = n => ModeDescription(GameMode.Campaign, n);
            y += 16f;
            // Roguelike: strain and Mutation first, then the difficulty.
            described[s.Add(MenuButton.Create(skin, t, "Roguelike", 160f, y, () => Loc.Get(WordIDs.Roguelike), () =>
            {
                pendingMode = GameMode.Roguelike;
                navigator.Push(runSetup);
            }))] = n => ModeDescription(GameMode.Roguelike, n);
            y += 16f;
            described[s.Add(MenuButton.Create(skin, t, "DailyRun", 160f, y, () => Loc.Get(WordIDs.DailyRun), () => navigator.Push(daily)))] =
                n => Loc.Get(n == 1 ? WordIDs.ModeDailyRunDesc1 : WordIDs.ModeDailyRunDesc2);
            GameMode[] endless = { GameMode.CaptureFlag, GameMode.PeaceWalker };
            foreach (GameMode m in endless)
            {
                y += 16f;
                GameMode mode = m;
                WordIDs id = (WordIDs)System.Enum.Parse(typeof(WordIDs), m.ToString());
                // Endless modes: pick the difficulty on the next screen (the campaign picks it in level select).
                described[s.Add(MenuButton.Create(skin, t, m.ToString(), 160f, y, () => Loc.Get(id), () =>
                {
                    pendingMode = mode;
                    navigator.Push(difficulty);
                }))] = n => ModeDescription(mode, n);
            }

            // What the highlighted mode is about, in two lines (nothing on Back).
            System.Func<int, string> line = n =>
                s.SelectedItem != null && described.TryGetValue(s.SelectedItem, out System.Func<int, string> text) ? text(n) : "";
            LiveText desc1 = LiveText.Create(skin, t, "Description1", 160f, 125f, () => line(1), dim: true);
            LiveText desc2 = LiveText.Create(skin, t, "Description2", 160f, 137f, () => line(2), dim: true);
            SelectionWatcher.Watch(s, () => { desc1.Refresh(); desc2.Refresh(); });
            s.Add(MenuButton.Create(skin, t, "Back", 160f, 150f, () => Loc.Get(WordIDs.Back), navigator.Back));
            return s;
        }

        /// <summary>Line 1 or 2 of a mode's description ("ModeRoguelikeDesc1"), in the current language.</summary>
        private static string ModeDescription(GameMode mode, int lineNumber) =>
            System.Enum.TryParse("Mode" + mode + "Desc" + lineNumber, out WordIDs id) ? Loc.Get(id) : "";

        /// <summary>
        /// Roguelike setup: the strain (character) and the Mutation level, each with what it means, then the difficulty.
        /// Locked strains can be browsed (with how to unlock them) but not taken.
        /// </summary>
        private MenuScreen BuildRunSetup()
        {
            MenuScreen s = MenuScreen.Create(root, "RunSetup");
            Transform t = s.transform;
            LiveText.Create(skin, t, "Title", 160f, 22f, () => Loc.Get(WordIDs.Roguelike).ToUpperInvariant(), role: PaletteRole.Accent);
            UIFactory.Image(t, "Rule", skin.pixel, 110f, 40f, 101f, 1f, PaletteRole.Foreground, true);

            StrainId strain = Progress.SelectedStrain;
            int mutation = Progress.SelectedMutation;
            MenuButton go = null;
            LiveText strain1 = null, strain2 = null, mut1 = null, mut2 = null;
            System.Action changed = () =>
            {
                strain1?.Refresh(); strain2?.Refresh(); mut1?.Refresh(); mut2?.Refresh();
                if (go != null) { go.Interactable = Strains.IsUnlocked(strain); go.Refresh(); }
            };

            s.Add(MenuOption.Choice(skin, t, "Strain", 48f, () => Loc.Get(WordIDs.Strain),
                () => Strains.Name(strain) + (Strains.IsUnlocked(strain) ? "" : " (" + Loc.Get(WordIDs.Locked) + ")"), dir =>
                {
                    strain = (StrainId)(((int)strain + dir + Strains.Count) % Strains.Count);
                    changed();
                }));
            strain1 = LiveText.Create(skin, t, "Strain1", 160f, 64f, () => Strains.Line1(strain), dim: true);
            strain2 = LiveText.Create(skin, t, "Strain2", 160f, 76f, () => Strains.Line2(strain), dim: true);

            s.Add(MenuOption.Choice(skin, t, "Mutation", 94f, () => Loc.Get(WordIDs.Mutation),
                () => mutation == 0 ? Loc.Get(WordIDs.Off) : mutation.ToString(), dir =>
                {
                    int top = Progress.MutationUnlocked;
                    mutation = top == 0 ? 0 : (mutation + dir + top + 1) % (top + 1);
                    changed();
                }));
            // The rule this level adds (all lower ones apply too) and what it pays; or how to get the first one.
            mut1 = LiveText.Create(skin, t, "Mutation1", 160f, 110f, () =>
                Progress.MutationUnlocked == 0 ? Loc.Get(WordIDs.MutationLocked)
                : mutation == 0 ? Loc.Get(WordIDs.MutationNone)
                : "+ " + Mutations.Rule(mutation), dim: true);
            mut2 = LiveText.Create(skin, t, "Mutation2", 160f, 122f, () => mutation > 0 ? Mutations.ScoreLine(mutation) : "",
                role: PaletteRole.Accent);

            go = s.Add(MenuButton.Create(skin, t, "Continue", 160f, 136f, () => Loc.Get(WordIDs.Continue), () =>
            {
                if (!Strains.IsUnlocked(strain)) return;
                Progress.SelectedStrain = strain;
                Progress.SelectedMutation = mutation;
                navigator.Push(difficulty);
            }));
            s.Add(MenuButton.Create(skin, t, "Back", 160f, 152f, () => Loc.Get(WordIDs.Back), navigator.Back));
            s.OnShown = () =>
            {
                strain = Progress.SelectedStrain;
                mutation = Progress.SelectedMutation;
                changed();
                s.RefreshAll();
            };
            return s;
        }

        /// <summary>
        /// The Daily Run: today's date and twist, whether the scored attempt is still there, and Start.
        /// Always Normal, no Mutations, Penicillin (the same run for everyone).
        /// </summary>
        private MenuScreen BuildDaily()
        {
            MenuScreen s = MenuScreen.Create(root, "Daily");
            Transform t = s.transform;
            LiveText.Create(skin, t, "Title", 160f, 22f, () => Loc.Get(WordIDs.DailyRun).ToUpperInvariant(), role: PaletteRole.Accent);
            UIFactory.Image(t, "Rule", skin.pixel, 110f, 40f, 101f, 1f, PaletteRole.Foreground, true);
            int twist() => DailyRun.TwistFor(DailyRun.Today);
            var texts = new System.Collections.Generic.List<LiveText>
            {
                LiveText.Create(skin, t, "Date", 160f, 46f, () => DailyRun.Today, dim: true),
                LiveText.Create(skin, t, "TwistLabel", 160f, 62f, () => Loc.Get(WordIDs.DailyTwist), dim: true),
                LiveText.Create(skin, t, "TwistName", 160f, 74f, () => DailyRun.TwistName(twist()), role: PaletteRole.Accent),
                LiveText.Create(skin, t, "Twist1", 160f, 88f, () => Loadouts.PerksLine(DailyRun.Twist(twist()))),
                LiveText.Create(skin, t, "Twist2", 160f, 100f, () => Loadouts.DrawbackLine(DailyRun.Twist(twist())), dim: true),
                LiveText.Create(skin, t, "Status", 160f, 116f, () => DailyRun.ScoredAttemptAvailable
                    ? Loc.Get(WordIDs.DailyReady)
                    : Loc.Get(WordIDs.DailyPractice).Replace("{0}", Progress.DailyScore.ToString()), role: PaletteRole.Accent),
            };
            s.Add(MenuButton.Create(skin, t, "Start", 160f, 134f, () => Loc.Get(WordIDs.Start), () =>
            {
                RoguelikeRun.NextRunDaily = true;
                SceneFlow.LoadMode(GameMode.Roguelike);
            }));
            s.Add(MenuButton.Create(skin, t, "Back", 160f, 150f, () => Loc.Get(WordIDs.Back), navigator.Back));
            s.OnShown = () => { foreach (LiveText lt in texts) lt.Refresh(); }; // the day may have changed
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
                    RoguelikeRun.NextRunDaily = false; // the Daily Run starts from its own screen
                    if (campaign()) SceneFlow.LoadLevel(pendingLevel);
                    else SceneFlow.LoadMode(pendingMode);
                }));
                LiveText.Create(skin, t, "Best" + i, 214f, y, () =>
                {
                    int best = campaign() ? Progress.Best(pendingLevel, d) : Progress.ModeBest(pendingMode, d);
                    Medal medal = campaign() ? Progress.GetMedal(pendingLevel, d) : Medal.None;
                    return (best > 0 ? Loc.Get(WordIDs.Best) + " " + best : "") + (medal != Medal.None ? "  " + Medals.Name(medal) : "");
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
            LiveText.Create(skin, t, "FontsExtra", 160f, 124f, () => "Noto Sans CJK (OFL), DejaVu Sans", dim: true);
            s.Add(MenuButton.Create(skin, t, "Back", 160f, 140f, () => Loc.Get(WordIDs.Back), navigator.Back));
            return s;
        }
    }
}
