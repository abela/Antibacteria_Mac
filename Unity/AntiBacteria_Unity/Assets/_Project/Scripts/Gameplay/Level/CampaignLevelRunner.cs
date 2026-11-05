using AntiBacteria.Audio;
using System;
using AntiBacteria.Core;
using AntiBacteria.Gameplay.Bonuses;
using AntiBacteria.Gameplay.Enemies;
using AntiBacteria.Gameplay.Weapons;
using AntiBacteria.Gameplay.Enemies.Bosses;
using AntiBacteria.Gameplay.Modes;
using AntiBacteria.Gameplay.Player;
using UnityEngine;

namespace AntiBacteria.Gameplay.Level
{
    /// <summary>
    /// Campaign rules for a normal level: survive the clock while waves spawn on the arena border.
    /// Waves every spawnInterval; enemies per wave start at 1 and grow every growInterval up to the cap.
    /// Ported from CompaignGameMode + LevelManager (Docs/DesignReference/04, sections 3 and 9).
    /// Boss levels: one boss, no clock, won by killing it (see IsBossLevel).
    /// </summary>
    public class CampaignLevelRunner : MonoBehaviour
    {
        public enum Phase { GetReady, Playing, Won, Lost }

        [Tooltip("Levels by number; the one played is GameSession.CurrentLevel.")]
        [SerializeField] private LevelDatabase database;
        [Tooltip("Used when there is no database, or when testing a single level.")]
        [SerializeField] private LevelData level;
        [SerializeField] private EnemyCatalog catalog;
        [SerializeField] private float getReadySeconds = 2f;
        [Tooltip("Enemies appear this far inside the walls (original: on the border, inset 20 pt).")]
        [SerializeField] private float spawnInsetPoints = 20f;

        public LevelData Level => level;
        public Phase Current { get; private set; } = Phase.GetReady;
        public float TimeLeft { get; internal set; }

        /// <summary>The endless mode being played (Capture the Flag, Rush, Peace Walker), or null in the campaign.</summary>
        public EndlessMode Mode { get; private set; }
        public bool IsEndless => Mode != null;
        /// <summary>Normal levels and Peace Walker count time down; boss levels and the other modes have no clock.</summary>
        public bool ShowsTimer => IsEndless ? Mode.ShowsTimer : !IsBossLevel;
        /// <summary>The run ended because the clock ran out (Peace Walker), not because the player died.</summary>
        public bool LostByTimeOut { get; private set; }
        /// <summary>An endless run ended with a new best score for its mode and difficulty.</summary>
        public bool NewModeBest { get; private set; }
        /// <summary>A roguelike run ended further than ever on this difficulty.</summary>
        public bool NewRoomBest { get; private set; }
        /// <summary>Seconds of play so far (shown on the result screens).</summary>
        public float ElapsedSeconds { get; private set; }
        public int EnemiesPerWave { get; private set; } = 1;
        /// <summary>The medal of a won campaign level (Level Clear screen).</summary>
        public Medal EarnedMedal { get; private set; }

        /// <summary>The first-play tutorial is on screen: the clock, the waves and the mode wait for it.</summary>
        public bool HoldForTutorial { get; set; }

        public event Action<Phase> PhaseChanged;

        /// <summary>Levels 10, 20 ... 50: one boss, no clock, won by killing it (Docs/DesignReference/03 section 5.2).</summary>
        public bool IsBossLevel => (Mode == null || Mode.UsesLevelData) && level != null && level.IsBoss;

        /// <summary>The boss of this level once it has appeared (null before, and on normal levels).</summary>
        public IBoss Boss { get; private set; }

        private bool bossSpawned;
        private bool modeBegun, modeEnded;

        private float phaseTimer, spawnTimer, growTimer;

        private void Awake()
        {
            Enemies.Enemy.HealthMultiplier = 1f; // a roguelike room sets its own when it begins
            Enemies.Enemy.SpeedMultiplier = 1f;
            EnemyBullet.SpeedMultiplier = 1f;
            EnemyCatalog.Active = catalog;
            Mode = EndlessMode.For(GameSession.Mode);
            if (IsEndless && !Mode.UsesLevelData) return; // no level data: the mode spawns everything
            if (database != null)
            {
                LevelData fromDb = database.Get(GameSession.CurrentLevel);
                if (fromDb != null) level = fromDb;
            }
            if (level != null) GameSession.CurrentLevel = level.number;
        }

        private void Start()
        {
            // Roguelike rooms after the first keep the run's score, kills and vitamins.
            Pickups.Vitamin.CollectAll = false;
            Unlocks.Clear(); // the result screen shows only what this level or run unlocked
            bool nextRoom = RoguelikeRun.Active && RoguelikeRun.ContinuingRoom;
            RoguelikeRun.ContinuingRoom = false;
            if (!nextRoom)
            {
                GameSession.ResetRun();
                Progress.CountPlay(); // first-time hints fade out after a few plays
            }
            TimeLeft = IsEndless ? (Mode.ShowsTimer ? ModeSettings.Current.secondsPerFlag : 0f) : level != null ? level.timeSeconds : 0f;
            spawnTimer = level != null ? level.SpawnInterval(GameSession.Difficulty) : 1f;
            SetPhase(Phase.GetReady, getReadySeconds);
            Sound.Music(IsBossLevel ? MusicTrack.Boss : MusicTrack.Level);
            Sound.Play(Sfx.GetReady);
            if (PlayerController.Current != null)
            {
                PlayerController.Current.Died += OnPlayerDied;
                // 2013: more bullets per volley as the campaign goes on (1 / 2 / 3 / 4 from levels 1 / 11 / 21 / 31).
                // Endless modes start at 1 and gain one every few objectives (EndlessMode).
                PlayerController.Current.VolleySize = RoguelikeRun.Active ? RoguelikeSettings.Current.baseVolley // perks are the power curve
                    : IsEndless && !Mode.UsesLevelData ? 1
                    : WeaponSettings.Current.VolleyForLevel(GameSession.CurrentLevel);
                if (RoguelikeRun.Active && RoguelikeRun.CarriedLives > 0) PlayerController.Current.SetLives(RoguelikeRun.CarriedLives);
                // A bonus picked up before the gate keeps running in this room, with the time it had left.
                if (nextRoom) PlayerController.Current.Buffs?.Restore(RoguelikeRun.CarriedBuffs);
                PlayerController.Current.CanShoot = !IsEndless || Mode.PlayerCanShoot;
            }
            if (IsBossLevel && GetComponent<SlowMotionAbility>() is SlowMotionAbility slow) slow.Enabled = false; // as in 2013
            if (!IsBossLevel && (!IsEndless || Mode.UsesLevelData) && GetComponent<BonusSpawner>() == null) gameObject.AddComponent<BonusSpawner>(); // none on boss levels
        }

        private void OnDestroy()
        {
            if (PlayerController.Current != null) PlayerController.Current.Died -= OnPlayerDied;
            // A roguelike room is left through the gate (the scene reloads) without a Won/Lost: end it here too, or its
            // listeners on Enemy.Killed / RoguelikeRun.PerkTaken outlive the room and every later kill pays XP again,
            // once per room passed (and Vitality / Shield apply once per room passed).
            EndMode();
        }

        /// <summary>Ends the endless mode exactly once (run over, or the scene unloading).</summary>
        private void EndMode()
        {
            if (Mode == null || !modeBegun || modeEnded) return;
            modeEnded = true;
            Mode.End();
        }

        private void Update()
        {
            if ((level == null && !IsEndless) || PauseController.IsPaused) return;
            float dt = Time.deltaTime;
            switch (Current)
            {
                case Phase.GetReady:
                    phaseTimer -= dt;
                    if (phaseTimer <= 0f) SetPhase(Phase.Playing, 0f);
                    break;

                case Phase.Playing:
                    if (HoldForTutorial) break;
                    ElapsedSeconds += dt;
                    if (IsEndless) { Mode.Tick(dt); break; }
                    if (IsBossLevel)
                    {
                        // One spawn after the usual delay; fix: exactly one boss (level 20 could get two Infernos).
                        if (!bossSpawned) { spawnTimer -= dt; if (spawnTimer <= 0f) SpawnBoss(); }
                        break;
                    }
                    TimeLeft = Mathf.Max(0f, TimeLeft - dt);
                    if (level.growIntervalSeconds > 0f)
                    {
                        growTimer += dt;
                        if (growTimer >= level.growIntervalSeconds)
                        {
                            growTimer -= level.growIntervalSeconds;
                            EnemiesPerWave = Mathf.Min(EnemiesPerWave + 1, Mathf.Max(1, level.maxEnemiesPerWave));
                        }
                    }
                    spawnTimer -= dt;
                    if (spawnTimer <= 0f)
                    {
                        SpawnWave();
                        spawnTimer += level.SpawnInterval(GameSession.Difficulty);
                    }
                    if (TimeLeft <= 0f) Win();
                    break;

            }
        }

        private void SpawnWave()
        {
            if (catalog == null || Arena.Current == null || level.enemyPool == null || level.enemyPool.Length == 0) return;
            for (int i = 0; i < EnemiesPerWave; i++)
            {
                int id = level.enemyPool[UnityEngine.Random.Range(0, level.enemyPool.Length)];
                Vector2 pos = Arena.Current.RandomPointOnBorder(Legacy.Units(spawnInsetPoints) + 0.5f);
                EnemyCatalog.Spawn(catalog.Get(id), pos);
            }
        }

        /// <summary>One enemy from the catalog on the arena border (used by the endless modes).</summary>
        public void SpawnAtBorder(int enemyId)
        {
            if (catalog == null || Arena.Current == null) return;
            EnemyDefinition def = catalog.Get(enemyId);
            if (def == null) { Debug.LogWarning("CampaignLevelRunner: no enemy with id " + enemyId); return; }
            Vector2 pos = Arena.Current.RandomPointOnBorder(Legacy.Units(spawnInsetPoints) + 0.5f);
            EnemyCatalog.Spawn(def, pos);
        }

        /// <summary>Peace Walker: the clock ran out.</summary>
        public void LoseByTimeOut()
        {
            if (Current != Phase.Playing) return;
            LostByTimeOut = true;
            EnemyBullet.ClearAll();
            SetPhase(Phase.Lost, 0f);
        }

        /// <summary>Roguelike boss rooms: the room decides when.</summary>
        public void SpawnBossNow() { if (!bossSpawned) SpawnBoss(); }

        /// <summary>Roguelike: through the gate to the next room (the scene reloads, the run carries on).</summary>
        public void NextRoom()
        {
            if (Current != Phase.Playing) return;
            PlayerController p = PlayerController.Current;
            RoguelikeRun.NextRoom(p != null ? p.Lives : -1, ElapsedSeconds,
                p != null && p.Buffs != null ? p.Buffs.Save() : default);
            Progress.SaveIfDirty();
            SceneFlow.ContinueRun();
        }

        /// <summary>Roguelike: the last room's boss is down. The run is won (no campaign progress is touched).</summary>
        public void CompleteRun()
        {
            if (Current != Phase.Playing) return;
            foreach (Enemy e in FindObjectsByType<Enemy>(FindObjectsSortMode.None)) e.Despawn();
            EnemyBullet.ClearAll();
            SetPhase(Phase.Won, 0f);
        }

        private void SpawnBoss()
        {
            bossSpawned = true;
            Sound.Play(Sfx.BossAppear);
            if (catalog == null || Arena.Current == null || level.enemyPool == null || level.enemyPool.Length == 0) return;
            Vector2 pos = Arena.Current.RandomPointOnBorder(Legacy.Units(spawnInsetPoints) + 0.5f);
            GameObject go = EnemyCatalog.Spawn(catalog.Get(level.enemyPool[0]), pos);
            Boss = go != null ? go.GetComponent<IBoss>() : null;
            if (Boss != null && !IsEndless) Boss.Defeated += Win; // roguelike rooms watch their boss themselves
            else Debug.LogWarning("CampaignLevelRunner: boss level without a boss - win by surviving instead.");
        }

        private void Win()
        {
            if (Current != Phase.Playing) return;
            Progress.RecordWin(GameSession.CurrentLevel, GameSession.Difficulty, GameSession.Score); // unlocks the next level
            // Medal: gold without losing a life, silver for one, bronze for a clear (ReplaySettings).
            PlayerController pc = PlayerController.Current;
            EarnedMedal = Medals.For(pc != null ? pc.LivesLost : 99, Progress.ImmortalityOn);
            Progress.RecordMedal(GameSession.CurrentLevel, GameSession.Difficulty, EarnedMedal);
            Unlocks.MedalsChanged();
            AchievementTracker.LevelWon(GameSession.CurrentLevel, GameSession.Difficulty, IsBossLevel);
            foreach (Enemy e in FindObjectsByType<Enemy>(FindObjectsSortMode.None)) e.Despawn();
            EnemyBullet.ClearAll();
            SetPhase(Phase.Won, 0f); // GameplayMenuController shows Level Clear
        }

        private void OnPlayerDied()
        {
            if (Current == Phase.Won || Current == Phase.Lost) return;
            EnemyBullet.ClearAll();
            SetPhase(Phase.Lost, 0f); // GameplayMenuController shows Game Over
        }

        private void SetPhase(Phase p, float duration)
        {
            Current = p;
            phaseTimer = duration;
            if (p == Phase.Playing && IsEndless) { Mode.Begin(this); modeBegun = true; }
            if (p == Phase.Won || p == Phase.Lost)
            {
                if (IsEndless)
                {
                    EndMode();
                    bool daily = RoguelikeRun.Active && RoguelikeRun.IsDaily;
                    // A Daily Run keeps its own score (the day's scored attempt only), not the roguelike bests.
                    if (daily) NewModeBest = RoguelikeRun.DailyScored && Progress.RecordDailyScore(GameSession.Score);
                    else NewModeBest = Progress.RecordModeScore(GameSession.Mode, GameSession.Difficulty, GameSession.Score);
                    AchievementTracker.RunEnded(GameSession.Mode, GameSession.Score, Mode.Completed);
                    Unlocks.RunEnded(GameSession.Mode, Mode.Completed);
                    if (RoguelikeRun.Active && !daily) NewRoomBest = Progress.RecordRoguelikeRoom(GameSession.Difficulty, RoguelikeRun.Room);
                }
                BonusPickup.ClearAll();
                if (p == Phase.Won) Pickups.Vitamin.CollectAll = true; // leftover vitamins fly in (GameplayMenuController waits for them)
                PlayerController.Current?.Buffs?.ClearAll();
                Progress.SaveIfDirty(); // vitamins picked up this level (a win has saved already)
                Sound.StopMusic();
                Sound.Play(p == Phase.Won ? Sfx.LevelWin : Sfx.GameOver);
                // Level over: no slow motion, no pause, no player input (all three leaked in the 2013 build).
                GetComponent<SlowMotionAbility>()?.ForceEnd();
                Time.timeScale = 1f;
                PauseController.Locked = true;
                if (PlayerController.Current != null) PlayerController.Current.ControlsEnabled = false;
            }
            PhaseChanged?.Invoke(p);
        }
    }
}
