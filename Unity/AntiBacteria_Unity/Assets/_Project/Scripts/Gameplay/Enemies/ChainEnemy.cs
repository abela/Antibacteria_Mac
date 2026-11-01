using System.Collections.Generic;
using AntiBacteria.Core;
using AntiBacteria.Gameplay.Player;
using UnityEngine;

namespace AntiBacteria.Gameplay.Enemies
{
    /// <summary>
    /// A chain of segments (Serpentus 14, ShootingSerpentus 19, CaterPillar 10, LotusBacterius 15).
    /// The head roams (half its targets are where the player is at that moment); the body follows the head's path at a
    /// fixed spacing. Only the head can be hurt; the body blocks shots and hurts on contact.
    /// What happens when the head dies is set per type (ChainDeath). Docs/DesignReference/03, sections 2-3.
    /// Fixes: spacing no longer depends on the frame rate (the original lerped 13 % per frame); a dead
    /// ShootingSerpentus no longer lingers invisibly and starts shooting again; CaterPillar pays out and shows its head.
    /// </summary>
    public class ChainEnemy : MonoBehaviour, IEnemyRoot
    {
        protected const float TrailStep = 1f / 16f; // record the head's path every pixel

        protected EnemyDefinition def;
        protected readonly List<Enemy> segments = new List<Enemy>();
        protected readonly List<Vector2> trail = new List<Vector2>(); // [0] = newest (head position)
        protected Enemy head;
        private Vector2 target;
        protected float spacing;
        private float shotTimer;
        private bool bloomed;

        private float ShotInterval => EnemyDefinition.ForDifficulty(def.shotIntervalByDifficulty, def.shotIntervalSeconds);

        public IReadOnlyList<Enemy> Segments => segments;
        public Enemy Head => head;

        public virtual void Init(EnemyDefinition definition)
        {
            def = definition;
            spacing = Legacy.Units(def.segmentSpacingPoints);
            EnemyDefinition segDef = def.segmentDefinition;
            Vector2 start = transform.position;

            // Lay the body out behind the head, away from the arena centre (the head starts on the border).
            Vector2 centre = Arena.Current != null ? Arena.Current.Bounds.center : Vector2.zero;
            Vector2 back = (start - centre).sqrMagnitude > 0.01f ? (start - centre).normalized : Vector2.right;

            for (int i = 0; i < def.chainSegments; i++)
            {
                Vector2 p = start + back * spacing * i;
                GameObject go = Instantiate(segDef.prefab, p, Quaternion.identity, null);
                go.name = $"{def.displayName} segment {i}";
                Enemy e = go.GetComponent<Enemy>();
                e.Init(segDef);
                segments.Add(e);
            }
            for (int k = 0; k <= Mathf.CeilToInt(spacing * def.chainSegments / TrailStep); k++)
                trail.Add(start + back * TrailStep * k);

            SetHead(segments[0], true);
            for (int i = 1; i < segments.Count; i++)
            {
                segments[i].Invulnerable = true;
                segments[i].SetSprite(BodySprite(i));
            }
            PickTarget();
            shotTimer = ShotInterval;
        }

        private Sprite BodySprite(int index)
        {
            if (def.bodySprites == null || def.bodySprites.Length == 0) return null;
            float t = (index - 1f) / Mathf.Max(1, def.chainSegments - 2);
            return def.bodySprites[Mathf.Clamp(Mathf.FloorToInt(t * def.bodySprites.Length), 0, def.bodySprites.Length - 1)];
        }

        protected void SetHead(Enemy e, bool first)
        {
            if (!first)
            {
                // The new head sits one spacing back along the path: drop the stretch the old head had covered.
                int drop = Mathf.Min(trail.Count - 1, Mathf.RoundToInt(spacing / TrailStep));
                if (drop > 0) trail.RemoveRange(0, drop);
                if (trail.Count == 0 || (trail[0] - e.Rigidbody.position).sqrMagnitude > 0.01f) trail.Insert(0, e.Rigidbody.position);
            }
            head = e;
            e.Invulnerable = false;
            if (def.headSprite != null) e.SetSprite(def.headSprite);
            if (first && def.headHits > 0) e.SetHits(def.headHits);
        }

        private void FixedUpdate()
        {
            segments.RemoveAll(s => s == null || s.Current == Enemy.State.Dead);
            if (segments.Count == 0) { Destroy(gameObject); return; }
            if (bloomed) return;
            if (head == null || head.Current == Enemy.State.Dead)
            {
                OnHeadLost();
                if (bloomed || this == null || segments.Count == 0) return;
            }

            if (head.Current != Enemy.State.Alive) { foreach (Enemy s in segments) s.Stop(); return; } // still fading in
            float dt = Time.fixedDeltaTime;
            Tick(dt);

            // Head: straight at the target at constant speed (no steering, as in the original).
            Vector2 pos = head.Rigidbody.position;
            float speed = Legacy.Units(def.speedPoints);
            if (Vector2.Distance(pos, target) <= Mathf.Max(Legacy.Units(def.arriveDistancePoints), speed * dt * 1.5f)) PickTarget();
            Vector2 dir = (target - pos).normalized;
            Vector2 next = pos + dir * speed * dt;
            head.MoveTo(next);
            Face(head, dir);

            // Path: record the head every pixel, then place each segment `spacing` further back along it.
            while ((next - trail[0]).magnitude >= TrailStep)
                trail.Insert(0, trail[0] + (next - trail[0]).normalized * TrailStep);
            int needed = Mathf.CeilToInt(spacing * segments.Count / TrailStep) + 2;
            if (trail.Count > needed) trail.RemoveRange(needed, trail.Count - needed);

            for (int i = 1; i < segments.Count; i++)
            {
                int idx = Mathf.Min(trail.Count - 1, Mathf.RoundToInt(spacing * i / TrailStep));
                segments[i].MoveTo(trail[idx]);
            }
        }

        private void Update()
        {
            if (bloomed || head == null || head.Current != Enemy.State.Alive) return;
            if (def.shotPattern == ShotPattern.None || def.bulletPrefab == null) return;
            shotTimer -= Time.deltaTime;
            if (shotTimer > 0f) return;
            shotTimer += ShotInterval;
            PlayerController p = PlayerController.Current;
            if (p == null || !p.IsAlive) return;
            Vector2 from = head.transform.position;
            Vector2 aim = ((Vector2)p.transform.position - from).normalized;
            EnemyBullet.Spawn(def.bulletPrefab, from + aim * Legacy.Units(def.shotOffsetPoints), aim, Legacy.Units(def.shotSpeedPoints), head);
        }

        /// <summary>Extra per-step behaviour for subclasses (Anakonda's growth). Runs while the head is alive.</summary>
        protected virtual void Tick(float dt) { }

        /// <summary>The chain was destroyed by the player (not by a level reset).</summary>
        protected virtual void OnChainKilled(Vector2 lastHeadPosition) { }

        private void OnHeadLost()
        {
            Vector2 at = segments.Count > 0 ? (Vector2)segments[0].transform.position : (Vector2)transform.position;
            switch (def.chainDeath)
            {
                case ChainDeath.PromoteNext:
                    // A chain of N dies after N-1 head kills: the last segment pops with the second-to-last head.
                    if (segments.Count <= 1)
                    {
                        foreach (Enemy s in segments) s.Pop();
                        segments.Clear();
                        OnChainKilled(at);
                        Destroy(gameObject);
                        return;
                    }
                    SetHead(segments[0], false);
                    break;

                case ChainDeath.KillAll:
                    foreach (Enemy s in segments) s.Pop();
                    segments.Clear();
                    OnChainKilled(at);
                    Destroy(gameObject);
                    break;

                case ChainDeath.Bloom:
                    bloomed = true;
                    foreach (Enemy s in segments)
                    {
                        s.Reconfigure(def.bloomDefinition);
                        s.Invulnerable = false;
                    }
                    break;
            }
        }

        private void PickTarget()
        {
            PlayerController p = PlayerController.Current;
            if (p != null && p.IsAlive && Random.value < 0.5f) target = p.transform.position; // snapshot, not a live track
            else target = Arena.Current != null ? Arena.Current.RandomPointInside(1f) : (Vector2)transform.position;
        }

        private void Face(Enemy e, Vector2 dir)
        {
            if (def.headFacingDegrees < 0f || e.Body == null || dir.sqrMagnitude < 0.0001f) return;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - def.headFacingDegrees;
            e.Body.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
        }
    }
}
