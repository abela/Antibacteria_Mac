using System;
using AntiBacteria.Core;
using AntiBacteria.Gameplay.Enemies;
using AntiBacteria.Gameplay.Player;
using UnityEngine;
using UnityEngine.SceneManagement;

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

        [SerializeField] private LevelData level;
        [SerializeField] private EnemyCatalog catalog;
        [SerializeField] private float getReadySeconds = 2f;
        [SerializeField] private float endScreenSeconds = 3f;
        [Tooltip("Enemies appear this far inside the walls (original: on the border, inset 20 pt).")]
        [SerializeField] private float spawnInsetPoints = 20f;

        public LevelData Level => level;
        public Phase Current { get; private set; } = Phase.GetReady;
        public float TimeLeft { get; private set; }
        public int EnemiesPerWave { get; private set; } = 1;

        public event Action<Phase> PhaseChanged;

        private float phaseTimer, spawnTimer, growTimer;

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

                case Phase.Won:
                case Phase.Lost:
                    phaseTimer -= Time.unscaledDeltaTime;
                    if (phaseTimer <= 0f) SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex); // slice: replay
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
            SetPhase(Phase.Won, endScreenSeconds);
        }

        private void OnPlayerDied() => SetPhase(Phase.Lost, endScreenSeconds);

        private void SetPhase(Phase p, float duration)
        {
            Current = p;
            phaseTimer = duration;
            if (p == Phase.Won || p == Phase.Lost) Time.timeScale = 1f;
            PhaseChanged?.Invoke(p);
        }
    }
}
