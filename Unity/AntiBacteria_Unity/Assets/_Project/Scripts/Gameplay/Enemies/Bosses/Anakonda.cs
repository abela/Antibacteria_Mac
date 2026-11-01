using System;
using System.Collections.Generic;
using AntiBacteria.Core;
using UnityEngine;

namespace AntiBacteria.Gameplay.Enemies.Bosses
{
    /// <summary>
    /// Boss 4, Anakonda (ID 25, level 40): a 24-segment giant snake (17 m/s) that shoots aimed bullets
    /// (1.5 / 1.0 / 0.5 s) and keeps growing: every 3.0 / 2.0 / 1.8 s a new 5-hit segment appears on the arena edge
    /// and streaks across to join its tail (dangerous on the way). Shoot head after head (14 hits each) until one
    /// segment is left. Anakonda.mm, Docs/DesignReference/03 section 6.4.
    /// Fixes: the fly-in is time-based (was 13 % per frame); the 10-vitamin drop lands where it died (was the
    /// world origin); the Normal fire rate can't collapse to a bullet per frame.
    /// </summary>
    public class Anakonda : ChainEnemy, IBoss
    {
        private const int AddedSegmentHits = 5;
        private const float FlyInSpeedPoints = 1600f;
        private const int DeathVitamins = 10;

        private readonly List<Enemy> incoming = new List<Enemy>();
        private float growTimer;
        private int mostSegments;

        public float Health01 => IsDefeated ? 0f : Mathf.Clamp01((segments.Count + incoming.Count - 1f) / Mathf.Max(1, mostSegments - 1));
        public bool Shielded => false;
        public bool IsDefeated { get; private set; }
        public event Action Defeated;

        public override void Init(EnemyDefinition definition)
        {
            base.Init(definition);
            mostSegments = segments.Count;
            growTimer = GrowInterval;
        }

        private float GrowInterval => EnemyDefinition.ForDifficulty(def.growIntervalByDifficulty, 3f);

        protected override void Tick(float dt)
        {
            growTimer -= dt;
            if (growTimer <= 0f)
            {
                growTimer += GrowInterval;
                SpawnIncoming();
            }

            // New segments fly straight at the tail; once they reach it they join the chain.
            incoming.RemoveAll(e => e == null || e.Current == Enemy.State.Dead);
            Vector2 tail = segments[segments.Count - 1].Rigidbody.position;
            float step = Legacy.Units(FlyInSpeedPoints) * dt;
            for (int i = incoming.Count - 1; i >= 0; i--)
            {
                Enemy e = incoming[i];
                Vector2 pos = e.Rigidbody.position, to = tail - pos;
                if (to.magnitude <= spacing + step)
                {
                    incoming.RemoveAt(i);
                    segments.Add(e);
                    mostSegments = Mathf.Max(mostSegments, segments.Count + incoming.Count);
                }
                else
                {
                    e.MoveTo(pos + to.normalized * step);
                }
            }
        }

        private void SpawnIncoming()
        {
            Vector2 p = Arena.Current != null ? Arena.Current.RandomPointOnBorder(1f) : (Vector2)transform.position;
            GameObject go = Instantiate(def.segmentDefinition.prefab, p, Quaternion.identity);
            go.name = def.displayName + " new segment";
            Enemy e = go.GetComponent<Enemy>();
            e.Init(def.segmentDefinition);
            e.SetHits(AddedSegmentHits);
            e.Invulnerable = true;
            if (def.bodySprites != null && def.bodySprites.Length > 0) e.SetSprite(def.bodySprites[def.bodySprites.Length - 1]);
            incoming.Add(e);
            mostSegments = Mathf.Max(mostSegments, segments.Count + incoming.Count);
        }

        protected override void OnChainKilled(Vector2 lastHeadPosition)
        {
            if (IsDefeated) return;
            IsDefeated = true;
            foreach (Enemy e in incoming) if (e != null) e.Pop();
            incoming.Clear();
            RewardSpawner.Instance?.DropVitamins(lastHeadPosition, DeathVitamins);
            GameSession.AddScore(def.killScore);
            Defeated?.Invoke();
        }

        private void OnDestroy()
        {
            // Level reset: take the incoming segments along.
            foreach (Enemy e in incoming) if (e != null) e.Despawn();
        }
    }
}
