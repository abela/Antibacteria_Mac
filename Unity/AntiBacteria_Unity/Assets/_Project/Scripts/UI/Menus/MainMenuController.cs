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

        private MenuScreen main, modes, options, about;

        public MenuScreen Main => main;
        public MenuScreen Modes => modes;
        public MenuScreen Options => options;
        public MenuScreen About => about;

        private void Start()
        {
            MenuBackdrop backdrop = MenuBackdrop.Create(skin, root);
            main = BuildMain();
            modes = BuildModes();
            options = OptionsMenu.Build(skin, root, navigator, false);
            about = BuildAbout();
            main.OnShown = modes.OnShown = about.OnShown = () => backdrop.ShowBacteria(true);
            options.OnShown = () => backdrop.ShowBacteria(false);
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
            MenuButton upgrade = s.Add(MenuButton.Create(skin, s.transform, "Upgrade", 160f, y, () => Loc.Get(WordIDs.UpgradeCharacter), null));
            upgrade.Interactable = false; // shop not ported yet
            y += 16f;
            s.Add(MenuButton.Create(skin, s.transform, "Options", 160f, y, () => Loc.Get(WordIDs.GameOptions), () => navigator.Push(options)));
            y += 16f;
            s.Add(MenuButton.Create(skin, s.transform, "About", 160f, y, () => Loc.Get(WordIDs.About), () => navigator.Push(about)));
            y += 16f;
            if (GameSettings.HasDisplayOption) // no Quit on consoles
                s.Add(MenuButton.Create(skin, s.transform, "Quit", 160f, y, () => Loc.Get(WordIDs.Quit), SceneFlow.Quit));

            LiveText.Create(skin, s.transform, "Version", 4f, 162f, () => "v" + Application.version,
                HorizontalAlignmentOptions.Left, 1, PaletteRole.Foreground, true);
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
                float wa = UIFactory.Width(anti, "ANTI");
                float total = UIFactory.Width(rest, "ANTIBACTERIA");
                float x0 = Mathf.Round(160f - total / 2f);
                anti.rectTransform.anchoredPosition = new Vector2(x0, -y);
                rest.rectTransform.anchoredPosition = new Vector2(x0 + wa, -y);
            });
        }

        private MenuScreen BuildModes()
        {
            MenuScreen s = MenuScreen.Create(root, "Modes");
            Transform t = s.transform;
            LiveText.Create(skin, t, "Title", 160f, 22f, () => Loc.Get(WordIDs.ChooseGame).ToUpperInvariant(), role: PaletteRole.Accent);
            UIFactory.Image(t, "Rule", skin.pixel, 110f, 40f, 101f, 1f, PaletteRole.Foreground, true);

            float y = 56f;
            s.Add(MenuButton.Create(skin, t, "Campaign", 160f, y, () => Loc.Get(WordIDs.Campaign), () => SceneFlow.LoadLevel(1)));
            WordIDs[] notYet = { WordIDs.CaptureFlag, WordIDs.Rush, WordIDs.PeaceWalker };
            foreach (WordIDs w in notYet)
            {
                y += 16f;
                WordIDs id = w;
                s.Add(MenuButton.Create(skin, t, w.ToString(), 160f, y, () => Loc.Get(id), null)).Interactable = false;
            }
            s.Add(MenuButton.Create(skin, t, "Back", 160f, 132f, () => Loc.Get(WordIDs.Back), navigator.Back));
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
