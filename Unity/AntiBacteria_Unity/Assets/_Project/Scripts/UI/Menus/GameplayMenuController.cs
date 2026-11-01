using System.Collections;
using AntiBacteria.Core;
using AntiBacteria.Gameplay.Level;
using AntiBacteria.Gameplay.Player;
using AntiBacteria.Input;
using AntiBacteria.Rendering;
using AntiBacteria.UI.Menu;
using UnityEngine;

namespace AntiBacteria.UI.Menus
{
    /// <summary>
    /// In-game menus: Pause (Esc / P / Start), Game Over and Level Clear with the level's stats.
    /// Replaces LevelOverLayer's overlay menus (Docs/DesignReference/05, section 4.3).
    /// </summary>
    public class GameplayMenuController : MonoBehaviour
    {
        [SerializeField] private UISkin skin;
        [SerializeField] private MenuNavigator navigator;
        [SerializeField] private RectTransform root;
        [SerializeField] private CampaignLevelRunner runner;
        [SerializeField] private LevelDatabase database;
        [Tooltip("Pause after the level ends before the result panel appears (real seconds).")]
        [SerializeField] private float resultDelaySeconds = 1.2f;

        private MenuScreen pause, options, gameOver, levelWon;
        private MenuButton nextLevelButton;
        private LiveText banner;
        private GameObject bossBar;
        private UnityEngine.UI.Image bossFill;
        private PaletteGraphic bossFillTint;
        private const float BossBarInner = 97f;

        public MenuScreen PauseScreen => pause;
        public MenuScreen GameOverScreen => gameOver;
        public MenuScreen LevelWonScreen => levelWon;
        public MenuNavigator Navigator => navigator;

        private void Start()
        {
            if (runner == null) runner = FindFirstObjectByType<CampaignLevelRunner>(); // survives a Vertical Slice rebuild
            pause = BuildPause();
            options = OptionsMenu.Build(skin, root, navigator, true);
            gameOver = BuildGameOver();
            levelWon = BuildLevelWon();
            BuildBanner();
            BuildBossBar();
            if (runner != null) runner.PhaseChanged += OnPhaseChanged;
        }

        private void OnDestroy()
        {
            if (runner != null) runner.PhaseChanged -= OnPhaseChanged;
        }

        private void Update()
        {
            UpdateBannerAndBossBar();

            // JustResumed: the Esc that closed the pause menu must not reopen it in the same frame.
            if (navigator.IsOpen || PauseController.IsPaused || PauseController.Locked || PauseController.JustResumed) return;
            if (GameInput.PausePressed) OpenPause();
        }

        public void OpenPause()
        {
            if (runner != null && (runner.Current == CampaignLevelRunner.Phase.Won || runner.Current == CampaignLevelRunner.Phase.Lost)) return;
            PauseController.Pause();
            navigator.Show(pause);
        }

        public void Resume()
        {
            navigator.Clear();
            PauseController.Resume();
        }

        private void OnPhaseChanged(CampaignLevelRunner.Phase phase)
        {
            if (phase == CampaignLevelRunner.Phase.Won || phase == CampaignLevelRunner.Phase.Lost)
                StartCoroutine(ShowResult(phase == CampaignLevelRunner.Phase.Won ? levelWon : gameOver));
        }

        private IEnumerator ShowResult(MenuScreen screen)
        {
            yield return new WaitForSecondsRealtime(resultDelaySeconds);
            if (PauseController.IsPaused) PauseController.Resume();
            navigator.Show(screen);
        }

        // ------------------------------------------------------------------ banner & boss bar

        /// <summary>"GET READY" / "GET READY FOR THE BOSS" during the level's opening seconds (2013 HUD label).</summary>
        private void BuildBanner()
        {
            banner = LiveText.Create(skin, root, "Banner", 160f, 70f,
                () => Loc.Get(runner != null && runner.IsBossLevel ? WordIDs.GetReadyForBoss : WordIDs.GetReady).ToUpperInvariant(),
                scale: 2, role: PaletteRole.Accent, maxWidth: 300f);
            banner.gameObject.SetActive(false);
        }

        /// <summary>A real boss health bar (2013 had none: its "boss_life" art was the slow-motion meter).</summary>
        private void BuildBossBar()
        {
            // Anchored to the top of the SCREEN (like the HUD hearts), not the 320x180 menu layout, and drawn
            // under the menus.
            var holder = UIFactory.Rect(root.parent, "BossBar", 0f, 0f, 0f, 0f, new Vector2(0f, 1f));
            holder.anchorMin = holder.anchorMax = new Vector2(0.5f, 1f);
            holder.anchoredPosition = new Vector2(-PixelCanvasScaler.Width / 2f, 0f);
            holder.SetAsFirstSibling();
            UIFactory.Frame(skin, holder, 110f, 7f, 210f, 12f, PaletteRole.Foreground, false);
            bossFill = UIFactory.Image(holder, "Fill", skin.pixel, 112f, 9f, BossBarInner, 2f, PaletteRole.Accent);
            bossFillTint = bossFill.GetComponent<PaletteGraphic>();
            bossBar = holder.gameObject;
            bossBar.SetActive(false);
        }

        private void UpdateBannerAndBossBar()
        {
            if (runner == null) return;
            bool showBanner = runner.Current == CampaignLevelRunner.Phase.GetReady && !navigator.IsOpen;
            if (banner.gameObject.activeSelf != showBanner)
            {
                banner.gameObject.SetActive(showBanner);
                if (showBanner) banner.Refresh();
            }

            var boss = runner.Boss;
            bool showBar = boss != null && !boss.IsDefeated && runner.Current == CampaignLevelRunner.Phase.Playing;
            if (bossBar.activeSelf != showBar) bossBar.SetActive(showBar);
            if (!showBar) return;
            bossFill.rectTransform.sizeDelta = new Vector2(Mathf.Round(BossBarInner * boss.Health01), 2f);
            // While the boss can't be hurt (BigRedNoise spawning) the bar turns white.
            PaletteRole role = boss.Shielded ? PaletteRole.Foreground : PaletteRole.Accent;
            if (bossFillTint.Role != role) bossFillTint.Role = role;
        }

        // ------------------------------------------------------------------ screens

        private MenuScreen BuildPause()
        {
            MenuScreen s = MenuScreen.Create(root, "Pause");
            Transform t = s.transform;
            s.OnBack = Resume;
            s.ResetSelectionOnShow = true;
            UIFactory.Panel(skin, t, 104f, 38f, 216f, 142f);
            LiveText.Create(skin, t, "Title", 160f, 46f, () => Loc.Get(WordIDs.Paused).ToUpperInvariant(), role: PaletteRole.Accent);
            s.Add(MenuButton.Create(skin, t, "Resume", 160f, 68f, () => Loc.Get(WordIDs.Resume), Resume));
            s.Add(MenuButton.Create(skin, t, "Restart", 160f, 84f, () => Loc.Get(WordIDs.Restart), SceneFlow.RestartLevel));
            s.Add(MenuButton.Create(skin, t, "Options", 160f, 100f, () => Loc.Get(WordIDs.GameOptions), () => navigator.Push(options)));
            s.Add(MenuButton.Create(skin, t, "MainMenu", 160f, 116f, () => Loc.Get(WordIDs.GoToMenu), SceneFlow.LoadMainMenu));
            return s;
        }

        private MenuScreen BuildGameOver()
        {
            MenuScreen s = MenuScreen.Create(root, "GameOver");
            Transform t = s.transform;
            s.CanGoBack = false;
            s.ResetSelectionOnShow = true;
            UIFactory.Panel(skin, t, 68f, 14f, 252f, 168f);
            LiveText.Create(skin, t, "Title", 160f, 22f, () => Loc.Get(WordIDs.GameOver).ToUpperInvariant(), scale: 2,
                role: PaletteRole.Accent, maxWidth: 176f);
            LiveText.Create(skin, t, "Level", 160f, 50f, LevelName);
            AddStats(t, 84f, 236f, 70f, 14f);
            s.Add(MenuButton.Create(skin, t, "Retry", 160f, 130f, () => Loc.Get(WordIDs.Retry), SceneFlow.RestartLevel));
            s.Add(MenuButton.Create(skin, t, "MainMenu", 160f, 146f, () => Loc.Get(WordIDs.GoToMenu), SceneFlow.LoadMainMenu));
            return s;
        }

        private MenuScreen BuildLevelWon()
        {
            MenuScreen s = MenuScreen.Create(root, "LevelClear");
            Transform t = s.transform;
            s.CanGoBack = false;
            s.ResetSelectionOnShow = true;
            UIFactory.Panel(skin, t, 68f, 6f, 252f, 176f);
            LiveText.Create(skin, t, "Title", 160f, 10f, () => IsLastLevel ? Loc.Get(WordIDs.FinishedGame) : Loc.Get(WordIDs.LevelClear).ToUpperInvariant(),
                scale: 2, maxWidth: 176f);
            LiveText.Create(skin, t, "Level", 160f, 38f, LevelName, role: PaletteRole.Accent);
            AddStats(t, 84f, 236f, 56f, 13f);

            StatRow.Create(skin, t, "Lives", 84f, 236f, 108f, () => Loc.Get(WordIDs.Lives), null);
            var hearts = new UnityEngine.UI.Image[3];
            float hw = skin.heartFull.rect.width, hh = skin.heartFull.rect.height;
            for (int i = 0; i < hearts.Length; i++)   // right-aligned to the stat column, 2 px apart
                hearts[i] = UIFactory.Sprite(skin, t, "Heart" + i, skin.heartFull, 237f - (3 - i) * (hw + 2f) + 2f + hw / 2f, 112f + hh / 2f);
            s.OnShown = () =>
            {
                PlayerController p = PlayerController.Current;
                int lives = p != null ? p.Lives : 0, max = p != null ? p.MaxLives : 3;
                for (int i = 0; i < hearts.Length; i++)
                {
                    hearts[i].enabled = i < max;
                    hearts[i].sprite = i < lives ? skin.heartFull : skin.heartEmpty;
                }
                nextLevelButton.gameObject.SetActive(!IsLastLevel);
            };

            nextLevelButton = s.Add(MenuButton.Create(skin, t, "NextLevel", 160f, 126f, () => Loc.Get(WordIDs.NextLevel),
                () => SceneFlow.LoadLevel(GameSession.CurrentLevel + 1)));
            s.Add(MenuButton.Create(skin, t, "Retry", 160f, 141f, () => Loc.Get(WordIDs.Retry), SceneFlow.RestartLevel));
            s.Add(MenuButton.Create(skin, t, "MainMenu", 160f, 156f, () => Loc.Get(WordIDs.GoToMenu), SceneFlow.LoadMainMenu));
            return s;
        }

        private void AddStats(Transform t, float x0, float x1, float y, float step)
        {
            StatRow.Create(skin, t, "Score", x0, x1, y, () => Loc.Get(WordIDs.Score), () => GameSession.Score.ToString());
            StatRow.Create(skin, t, "Kills", x0, x1, y + step, () => Loc.Get(WordIDs.Kills), () => GameSession.Kills.ToString());
            StatRow.Create(skin, t, "Vitamins", x0, x1, y + 2f * step, () => Loc.Get(WordIDs.Vitamins), () => GameSession.Vitamins.ToString());
            StatRow.Create(skin, t, "Time", x0, x1, y + 3f * step, () => Loc.Get(WordIDs.Time), () => FormatTime(runner != null ? runner.ElapsedSeconds : 0f));
        }

        private bool IsLastLevel => database != null ? database.IsLast(GameSession.CurrentLevel) : GameSession.CurrentLevel >= SceneFlow.LevelCount;

        private string LevelName() =>
            Loc.Get(WordIDs.Level) + " " + (runner != null && runner.Level != null ? runner.Level.Label : GameSession.CurrentLevel.ToString());

        public static string FormatTime(float seconds)
        {
            int s = Mathf.FloorToInt(seconds);
            return (s / 60) + ":" + (s % 60).ToString("00");
        }
    }
}
