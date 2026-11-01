using System.Collections.Generic;
using UnityEngine;

namespace AntiBacteria.Gameplay.Enemies.Bosses
{
    /// <summary>
    /// Boss 3, BigRedNoise (ID 24, level 30): a giant red chaser (12 / 15 / 18 m/s by difficulty, 400 hits).
    /// Each time it drops below 300, 200 and 100 hits it stops, spins up, becomes invulnerable and pours out
    /// 60 / 80 / 90 RedNoise minions (one per 0.1 s). Afterwards it moves at a third of its speed until every
    /// minion is dead. BigRedNoise.mm, Docs/DesignReference/03 section 6.3.
    /// Fix: a wave ends after that many SPAWNS (2013 waited until that many were alive at once, so a player who
    /// kept killing them kept the boss invulnerable forever).
    /// </summary>
    public class BigRedNoise : BossEnemy
    {
        public const int MinionId = -2;
        private static readonly int[] WaveThresholds = { 300, 200, 100 };
        private static readonly int[] WaveSizes = { 60, 80, 90 };
        private const float SpawnEvery = 0.1f;
        private const float WaveSpin = 900f;

        private readonly List<Enemy> minions = new List<Enemy>();
        private int wavesDone;
        private int spawnedThisWave = -1; // -1 = not spawning
        private float waveSpawnTimer;
        private bool slowed;

        public bool SpawningWave => spawnedThisWave >= 0;
        public override bool Shielded => SpawningWave;

        public override void Init(EnemyDefinition definition)
        {
            base.Init(definition);
            Hurt += OnHurt;
        }

        private void OnHurt(Enemy _)
        {
            if (SpawningWave || wavesDone >= WaveThresholds.Length || HitsLeft <= 0) return;
            if (HitsLeft < WaveThresholds[wavesDone]) StartWave();
        }

        private void StartWave()
        {
            spawnedThisWave = 0;
            waveSpawnTimer = 0f;
            Invulnerable = true;   // shots still stop on it
            SpinOverride = WaveSpin;
        }

        protected override void Update()
        {
            base.Update();
            if (Current != State.Alive) return;
            minions.RemoveAll(m => m == null);

            if (SpawningWave)
            {
                waveSpawnTimer -= Time.deltaTime;
                while (waveSpawnTimer <= 0f && SpawningWave)
                {
                    waveSpawnTimer += SpawnEvery;
                    GameObject go = EnemyCatalog.SpawnById(MinionId, transform.position);
                    Enemy m = go != null ? go.GetComponent<Enemy>() : null;
                    if (m != null) minions.Add(m);
                    if (++spawnedThisWave >= WaveSizes[wavesDone]) EndWave();
                }
            }
            else if (slowed && minions.Count == 0)
            {
                slowed = false;
                SpeedOverridePoints = EnemyDefinition.ForDifficulty(Definition.speedPointsByDifficulty, Definition.speedPoints);
            }
        }

        private void EndWave()
        {
            wavesDone++;
            spawnedThisWave = -1;
            Invulnerable = false;
            SpinOverride = null;
            slowed = true;
            SpeedOverridePoints = EnemyDefinition.ForDifficulty(Definition.speedPointsByDifficulty, Definition.speedPoints) / 3f;
        }

        protected override Vector2 DesiredVelocity(float dt) => SpawningWave ? Vector2.zero : base.DesiredVelocity(dt);

        protected override void OnDefeated()
        {
            foreach (Enemy m in minions) if (m != null) m.Pop();
        }
    }
}
