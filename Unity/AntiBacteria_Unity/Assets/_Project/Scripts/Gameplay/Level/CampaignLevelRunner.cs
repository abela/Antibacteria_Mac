using System;
using AntiBacteria.Core;
using AntiBacteria.Gameplay.Enemies;
using AntiBacteria.Gameplay.Player;
using UnityEngine;

namespace AntiBacteria.Gameplay.Level
{
    /// <summary>
    /// Campaign rules for a normal level: survive the clock while waves spawn on the arena border.
    /// Waves every spawnInterval; enemies per wave start at 1 and grow every growInterval up to the cap.
    /// Ported from CompaignGameMode + LevelManager (Docs/DesignReference/04, sections 3 and 9).
    /// Boss levels come later.
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
        public float TimeLeft { get; private set; }
        /// <summary>Seconds of play so far (shown on the result screens).</summary>
        public float ElapsedSeconds { get; private set; }
        public int EnemiesPerWave { get; private set; } = 1;

        public event Action<Phase> PhaseChanged;

        private float phaseTimer, spawnTimer, growTimer;

        private void Awake()
        {
            if (database != null)
            {
                LevelData fromDb = database.Get(GameSession.CurrentLevel);
                if (fromDb != null) level = fromDb;
            }
            if (level != null) GameSession.CurrentLevel = level.number;
        }

        private void Start()
        {
            GameSession.ResetRun();
            TimeLeft = level != null ? level.timeSeconds : 0f;
            spawnTimer = level != null ? level.SpawnInterval(GameSession.Difficulty) : 1f;
            SetPhase(Phase.GetReady, getReadySeconds);
            if (PlayerController.Current != null) PlayerController.Current.Died += OnPlayerDied;
        }

        private void OnDestroy()
        {
            if (PlayerController.Current != null) PlayerController.Current.Died -= OnPlayerDied;
        }

        private void Update()
        {
            if (level == null || PauseController.IsPaused) return;
            float dt = Time.deltaTime;
            switch (Current)
            {
                case Phase.GetReady:
                    phaseTimer -= dt;
                    if (phaseTimer <= 0f) SetPhase(Phase.Playing, 0f);
                    break;

                case Phase.Playing:
                    ElapsedSeconds += dt;
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
                EnemyDefinition def = catalog.Get(id);
                if (def == null || def.prefab == null) continue;
                Vector2 pos = Arena.Current.RandomPointOnBorder(Legacy.Units(spawnInsetPoints) + 0.5f);
                Enemy e = Instantiate(def.prefab, pos, Quaternion.identity);
                e.Init(def);
            }
        }

        private void Win()
        {
            foreach (Enemy e in FindObjectsByType<Enemy>(FindObjectsSortMode.None)) e.Despawn();
            SetPhase(Phase.Won, 0f); // GameplayMenuController shows Level Clear
        }

        private void OnPlayerDied() => SetPhase(Phase.Lost, 0f); // GameplayMenuController shows Game Over

        private void SetPhase(Phase p, float duration)
        {
            Current = p;
            phaseTimer = duration;
            if (p == Phase.Won || p == Phase.Lost)
            {
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
