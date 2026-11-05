using System.Collections;
using AntiBacteria.Audio;
using AntiBacteria.Core;
using AntiBacteria.Gameplay.Bonuses;
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
        [Tooltip("Level won: longest extra wait for leftover vitamins to reach the player before Level Clear opens (real seconds).")]
        [SerializeField] private float maxVitaminWaitSeconds = 1.5f;

        private MenuScreen pause, options, gameOver, levelWon;
        private MenuButton nextLevelButton;
        private LiveText banner;
        private GameObject bossBar;
        private UnityEngine.UI.Image bossFill;
        private PaletteGraphic bossFillTint;
        private const float BossBarInner = 97f;
        private LiveText toast;
        private BonusType toastType;
        private float toastLeft;
        private PlayerBuffs watchedBuffs;
        private const float ToastSeconds = 1.6f;
        private MenuScreen levelUp;
        private PerkCards perkCards;
        private MenuButton rerollButton;
        private readonly System.Collections.Generic.Dictionary<BonusPickup, LiveText> bonusLabels =
            new System.Collections.Generic.Dictionary<BonusPickup, LiveText>();
        private readonly System.Collections.Generic.List<BonusPickup> goneLabels = new System.Collections.Generic.List<BonusPickup>();

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
            BuildToast();
            TutorialOverlay.CreateIfNeeded(skin, root, runner, navigator); // first play only: move, shoot, slow motion
            if (RoguelikeRun.Active)
            {
                RoguelikeHud.Create(skin, root);
                levelUp = BuildLevelUp();
            }
            PromptBar.Create(skin, root, navigator); // [A] Select  [B] Back on pause, options and result screens
            if (IntroFlow.Handoff) AntiBacteria.UI.Intro.IntroHandoff.Create(skin, root); // from the intro's white flash
            if (runner != null) runner.PhaseChanged += OnPhaseChanged;
            AntiBacteria.Platform.PlatformServices.OverlayChanged += OnOverlayChanged;
        }

        private void OnDestroy()
        {
            if (runner != null) runner.PhaseChanged -= OnPhaseChanged;
            AntiBacteria.Platform.PlatformServices.OverlayChanged -= OnOverlayChanged;
            if (watchedBuffs != null) watchedBuffs.Picked -= OnBonusPicked;
        }

        private void Update()
        {
            UpdateBannerAndBossBar();
            UpdateToast();
            UpdateBonusLabels();
            if (levelUp != null) CheckLevelUp();

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

        /// <summary>The Steam overlay opened over a running level: pause it (the player resumes from the menu).</summary>
        private void OnOverlayChanged(bool open)
        {
            if (open && !navigator.IsOpen && !PauseController.IsPaused && !PauseController.Locked) OpenPause();
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
            if (screen == levelWon)
            {
                // Leftover vitamins are still flying in: give them a moment, then credit any stragglers.
                float until = Time.unscaledTime + maxVitaminWaitSeconds;
                while (Gameplay.Pickups.Vitamin.LiveCount > 0 && Time.unscaledTime < until) yield return null;
                Gameplay.Pickups.Vitamin.CreditRest();
            }
            if (PauseController.IsPaused) PauseController.Resume();
            navigator.Show(screen);
        }

        // ------------------------------------------------------------------ bonus pickup message

        /// <summary>The bonus name under the timer for a moment after a pickup (2013: updateBonusOnLevelOverLayer).</summary>
        /// <summary>
        /// "BONUS" over each pickup for the player's first few plays (BonusSettings.labelForFirstPlays), so a new
        /// player learns what the boxed icons are. Drawn on this canvas at the bonus's screen position.
        /// </summary>
        private void UpdateBonusLabels()
        {
            bool want = Progress.Plays <= BonusSettings.Current.labelForFirstPlays && !navigator.IsOpen;
            Camera cam = Camera.main;
            foreach (BonusPickup b in BonusPickup.All)
            {
                if (b == null || !want || cam == null) continue;
                if (!bonusLabels.TryGetValue(b, out LiveText label))
                {
                    label = LiveText.Create(skin, root, "BonusLabel", 0f, 0f, () => Loc.Get(WordIDs.Bonus).ToUpperInvariant(), role: PaletteRole.Accent);
                    bonusLabels[b] = label;
                }
                // The text row starts 28 px above the centre, so the letters clear the frame and its corner brackets.
                Vector3 world = b.transform.position + new Vector3(0f, 28f / Legacy.PixelsPerUnit, 0f);
                Vector2 screen = cam.WorldToScreenPoint(world);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(root, screen, null, out Vector2 local);
                Rect r = root.rect;
                var rt = (RectTransform)label.transform;
                rt.anchoredPosition = new Vector2(Mathf.Round(local.x - r.xMin), Mathf.Round(local.y - r.yMax));
                label.gameObject.SetActive(b.Visible);
            }
            goneLabels.Clear();
            foreach (var pair in bonusLabels)
                if (pair.Key == null || !want) goneLabels.Add(pair.Key);
            foreach (BonusPickup b in goneLabels)
            {
                if (bonusLabels[b] != null) Destroy(bonusLabels[b].gameObject);
                bonusLabels.Remove(b);
            }
        }

        // ------------------------------------------------------------------ roguelike level up

        /// <summary>"LEVEL UP!", the run's new level and three perk cards; the game is paused until one is taken.</summary>
        private MenuScreen BuildLevelUp()
        {
            MenuScreen s = MenuScreen.Create(root, "LevelUp");
            Transform t = s.transform;
            s.CanGoBack = false;
            s.ResetSelectionOnShow = true;
            UIFactory.Panel(skin, t, 4f, 4f, 315f, 175f);
            LiveText.Create(skin, t, "Title", 160f, 10f, () => Loc.Get(WordIDs.LevelUp).ToUpperInvariant(), scale: 2,
                role: PaletteRole.Accent, maxWidth: 300f);
            LiveText.Create(skin, t, "Subtitle", 160f, 33f, () => Loc.Get(WordIDs.Lv) + " " + RoguelikeRun.Level + "   " + Loc.Get(WordIDs.ChoosePerk), dim: true);
            perkCards = s.Add(PerkCards.Create(skin, t, 50f, RoguelikeSettings.Current.perkChoices, TakePerk));
            // Reroll perk: new cards for this level up (shown once the perk is owned).
            rerollButton = s.Add(MenuButton.Create(skin, t, "Reroll", 160f, 162f,
                () => Loc.Get(WordIDs.Reroll).Replace("{0}", RoguelikeRun.RerollsLeft.ToString()), Reroll));
            return s;
        }

        /// <summary>A level up is waiting and nothing else is on screen: pause and offer perks.</summary>
        private void CheckLevelUp()
        {
            if (RoguelikeRun.PendingLevelUps <= 0 || navigator.IsOpen || PauseController.IsPaused) return;
            if (runner == null || runner.Current != CampaignLevelRunner.Phase.Playing) return;
            var offer = RoguelikeRun.Offer(RunModifiers.PerkChoices);
            if (offer.Count == 0) { RoguelikeRun.ConsumeLevelUp(); return; } // every perk maxed
            PauseController.Pause();
            Sound.Play(Sfx.BonusAppear);
            RoguelikeRun.ResetRerolls();
            perkCards.Show(offer);
            UpdateReroll();
            navigator.Show(levelUp);
        }

        private void TakePerk(PerkId perk)
        {
            RoguelikeRun.Take(perk);
            Sound.Play(Sfx.BonusPickup);
            var offer = RoguelikeRun.PendingLevelUps > 0 ? RoguelikeRun.Offer(RunModifiers.PerkChoices) : null;
            if (offer != null && offer.Count > 0)
            {
                RoguelikeRun.ResetRerolls();
                perkCards.Show(offer); // another level up is waiting
                UpdateReroll();
                levelUp.RefreshAll();
                return;
            }
            navigator.Clear();
            PauseController.Resume();
        }

        private void Reroll()
        {
            if (!RoguelikeRun.UseReroll()) return;
            var offer = RoguelikeRun.Offer(RunModifiers.PerkChoices);
            if (offer.Count > 0) perkCards.Show(offer);
            Sound.Play(Sfx.BonusAppear, 0.8f, 1.2f);
            UpdateReroll();
            levelUp.RefreshAll();
        }

        private void UpdateReroll()
        {
            if (rerollButton == null) return;
            rerollButton.gameObject.SetActive(RoguelikeRun.Rank(PerkId.Reroll) > 0);
            rerollButton.Interactable = RoguelikeRun.RerollsLeft > 0;
            rerollButton.Refresh();
        }

        private void BuildToast()
        {
            toast = LiveText.Create(skin, root, "BonusToast", 160f, 26f, ToastText, role: PaletteRole.Accent);
            toast.gameObject.SetActive(false);
        }

        private string ToastText()
        {
            BonusInfo info = BonusSettings.Current.Info(toastType);
            return info != null ? Loc.Get(info.nameId).ToUpperInvariant() : "";
        }

        private void OnBonusPicked(BonusType type)
        {
            toastType = type;
            toastLeft = ToastSeconds;
            toast.gameObject.SetActive(true);
            toast.Refresh();
        }

        private void UpdateToast()
        {
            if (watchedBuffs == null && PlayerController.Current != null && PlayerController.Current.Buffs != null)
            {
                watchedBuffs = PlayerController.Current.Buffs;
                watchedBuffs.Picked += OnBonusPicked;
            }
            if (toastLeft <= 0f) return;
            if (!PauseController.IsPaused) toastLeft -= Time.unscaledDeltaTime;
            bool show = toastLeft > 0f && !navigator.IsOpen;
            if (toast.gameObject.activeSelf != show) toast.gameObject.SetActive(show);
        }

        // ------------------------------------------------------------------ banner & boss bar

        /// <summary>"GET READY" / "GET READY FOR THE BOSS" during the level's opening seconds (2013 HUD label).</summary>
        private void BuildBanner()
        {
            banner = LiveText.Create(skin, root, "Banner", 160f, 70f,
                () => RoguelikeRun.Active
                    ? (Loc.Get(WordIDs.Room) + " " + RoguelikeRun.Room + (runner != null && runner.IsBossLevel ? "  " + Loc.Get(WordIDs.Boss) : "")).ToUpperInvariant()
                    : Loc.Get(runner != null && runner.IsBossLevel ? WordIDs.GetReadyForBoss : WordIDs.GetReady).ToUpperInvariant(),
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
            LiveText.Create(skin, t, "Title", 160f, 22f, () => Loc.Get(runner != null && runner.LostByTimeOut ? WordIDs.TimeOff : WordIDs.GameOver).ToUpperInvariant(), scale: 2,
                role: PaletteRole.Accent, maxWidth: 176f);
            // Something unlocked in this run (a strain, the next Mutation) takes the level line's place.
            LiveText.Create(skin, t, "Level", 160f, 50f, () => string.IsNullOrEmpty(Unlocks.Message) ? LevelName() : "");
            LiveText.Create(skin, t, "Unlocked", 160f, 50f, () => Unlocks.Message ?? "", role: PaletteRole.Accent);
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
            LiveText.Create(skin, t, "Level", 160f, 38f, () => string.IsNullOrEmpty(Unlocks.Message) ? LevelName() : Unlocks.Message,
                role: PaletteRole.Accent);
            AddStats(t, 84f, 236f, 50f, 12f);

            StatRow.Create(skin, t, "Lives", 84f, 236f, 98f, () => Loc.Get(WordIDs.Lives), null);
            // Campaign medal: gold without losing a life, silver for one, bronze for a clear.
            StatRow.Create(skin, t, "Medal", 84f, 236f, 111f, () => Loc.Get(WordIDs.Medal),
                () => runner != null && !runner.IsEndless ? Medals.Name(runner.EarnedMedal) : "-");
            var hearts = new UnityEngine.UI.Image[3];
            float hw = skin.heartFull.rect.width, hh = skin.heartFull.rect.height;
            for (int i = 0; i < hearts.Length; i++)   // right-aligned to the stat column, 2 px apart
                hearts[i] = UIFactory.Sprite(skin, t, "Heart" + i, skin.heartFull, 237f - (3 - i) * (hw + 2f) + 2f + hw / 2f, 102f + hh / 2f);
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
            StatRow.Create(skin, t, "Time", x0, x1, y + 3f * step, () => Loc.Get(WordIDs.Time), () => FormatTime((runner != null ? runner.ElapsedSeconds : 0f) + (RoguelikeRun.Active ? RoguelikeRun.SecondsBeforeThisRoom : 0f)));
        }

        private bool IsLastLevel => database != null ? database.IsLast(GameSession.CurrentLevel) : GameSession.CurrentLevel >= SceneFlow.LevelCount;

        private string LevelName() => RoguelikeRun.Active && RoguelikeRun.IsDaily
            ? Loc.Get(WordIDs.DailyRun) + "  " + Loc.Get(WordIDs.Room) + " " + RoguelikeRun.Room
            : RoguelikeRun.Active
            ? Loc.Get(WordIDs.Room) + " " + RoguelikeRun.Room + "  " + Loc.Get(WordIDs.Best) + " " + Progress.RoguelikeBestRoom(GameSession.Difficulty)
              + (RoguelikeRun.Mutation > 0 ? "  M" + RoguelikeRun.Mutation : "")
            : runner != null && runner.IsEndless
            ? ModeName(GameSession.Mode) + "  " + Loc.Get(WordIDs.Best) + " " + Progress.ModeBest(GameSession.Mode, GameSession.Difficulty)
            : Loc.Get(WordIDs.Level) + " " + (runner != null && runner.Level != null ? runner.Level.Label : GameSession.CurrentLevel.ToString());

        public static string ModeName(GameMode mode) => Loc.Get((WordIDs)System.Enum.Parse(typeof(WordIDs), mode.ToString()));

        public static string FormatTime(float seconds)
        {
            int s = Mathf.FloorToInt(seconds);
            return (s / 60) + ":" + (s % 60).ToString("00");
        }
    }
}
