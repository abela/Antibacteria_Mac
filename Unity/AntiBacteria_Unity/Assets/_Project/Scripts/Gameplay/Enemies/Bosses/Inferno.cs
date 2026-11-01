using System.Collections.Generic;
using UnityEngine;

namespace AntiBacteria.Gameplay.Enemies.Bosses
{
    /// <summary>
    /// Boss 2, Inferno (ID 23, level 20): a huge burning mass that dashes around (half its targets are where the
    /// player stands) and speeds up as it is hurt: 7 m/s, then 10 below 200 hits, 16 below 100. It brings 10 / 20 / 25
    /// indestructible Tentacles (by difficulty) that wander the arena until it dies.
    /// Inferno.mm, Docs/DesignReference/03 section 6.2. Fix: exactly one Inferno (2013 could spawn two).
    /// </summary>
    public class Inferno : BossEnemy
    {
        public const int TentacleId = 17;
        private const float TentacleSpeedPoints = 5f * 32f;
        private static readonly int[] HitLimits = { 200, 100 };
        private static readonly float[] TierSpeedPoints = { 10f * 32f, 16f * 32f };

        private readonly List<Enemy> tentacles = new List<Enemy>();

        public override void Init(EnemyDefinition definition)
        {
            base.Init(definition);
            Hurt += OnHurt;
            int count = EnemyDefinition.ForDifficulty(definition.countByDifficulty, 10);
            for (int i = 0; i < count; i++)
            {
                Vector2 p = Arena.Current != null ? Arena.Current.RandomPointOnBorder(1f) : (Vector2)transform.position;
                GameObject go = EnemyCatalog.SpawnById(TentacleId, p);
                Enemy t = go != null ? go.GetComponent<Enemy>() : null;
                if (t == null) continue;
                t.SpeedOverridePoints = TentacleSpeedPoints;
                tentacles.Add(t);
            }
        }

        private void OnHurt(Enemy _)
        {
            for (int i = HitLimits.Length - 1; i >= 0; i--)
            {
                if (HitsLeft < HitLimits[i])
                {
                    SpeedOverridePoints = TierSpeedPoints[i];
                    return;
                }
            }
        }

        protected override void OnDefeated()
        {
            foreach (Enemy t in tentacles) if (t != null) t.Pop();
        }
    }
}
