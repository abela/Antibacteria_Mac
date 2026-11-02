using System.Collections.Generic;
using AntiBacteria.Audio;
using AntiBacteria.Core;
using AntiBacteria.Gameplay.Bonuses;
using AntiBacteria.Gameplay.Effects;
using AntiBacteria.Gameplay.Enemies;
using AntiBacteria.Gameplay.Player;
using AntiBacteria.Rendering;
using UnityEngine;

namespace AntiBacteria.Gameplay.Roguelike
{
    /// <summary>
    /// The roguelike perks that live in the room rather than on a bullet: Germ Buddies and Orbiters (kept in step
    /// with their ranks), Chain Zap (a kill zaps nearby enemies) and Vampire (kills heal). One per room,
    /// made by RoguelikeRoom.
    /// </summary>
    public class PerkEffects : MonoBehaviour
    {
        private readonly List<GermBuddy> buddies = new List<GermBuddy>();
        private readonly List<Orbiter> orbiters = new List<Orbiter>();
        private bool zapping;

        public static PerkEffects Create()
        {
            var go = new GameObject("PerkEffects");
            return go.AddComponent<PerkEffects>();
        }

        private void Start()
        {
            Enemy.Killed += OnKilled;
            RoguelikeRun.PerkTaken += OnPerkTaken;
            Sync();
        }

        private void OnDestroy()
        {
            Enemy.Killed -= OnKilled;
            RoguelikeRun.PerkTaken -= OnPerkTaken;
            foreach (GermBuddy b in buddies) if (b != null) Destroy(b.gameObject);
            foreach (Orbiter o in orbiters) if (o != null) Destroy(o.gameObject);
        }

        private void OnPerkTaken(PerkId p) => Sync();

        /// <summary>As many buddies and orbiters as the ranks say.</summary>
        private void Sync()
        {
            if (PlayerController.Current == null) return;
            while (buddies.Count < RunModifiers.Buddies) buddies.Add(GermBuddy.Create(buddies.Count));
            int want = RunModifiers.Orbiters;
            if (orbiters.Count != want)
            {
                foreach (Orbiter o in orbiters) if (o != null) Destroy(o.gameObject);
                orbiters.Clear();
                for (int i = 0; i < want; i++) orbiters.Add(Orbiter.Create(i, want));
            }
        }

        private void OnKilled(Enemy e)
        {
            if (e == null) return;
            PlayerController p = PlayerController.Current;
            if (p == null || !p.IsAlive) return;
            RoguelikeSettings s = RoguelikeSettings.Current;

            if (RunModifiers.Vampire && s.vampireKills > 0)
            {
                RoguelikeRun.VampireKills = Mathf.Min(s.vampireKills, RoguelikeRun.VampireKills + 1);
                if (RoguelikeRun.VampireKills >= s.vampireKills && p.Lives < p.MaxLives)
                {
                    RoguelikeRun.VampireKills = 0;
                    p.Heal(1);
                    Fx.Sparks(p.transform.position, Vector2.up, 70f, 14, 1f, 0.3f);
                    Sound.Play(Sfx.BonusPickup, 0.8f, 1.3f);
                }
            }

            // Chain Zap: bolts to the nearest enemies around the kill. A zap's own kill doesn't chain again.
            int zaps = RunModifiers.ChainZaps;
            if (zaps <= 0 || zapping) return;
            Vector2 from = e.transform.position;
            var targets = new List<Enemy>();
            foreach (Collider2D c in Physics2D.OverlapCircleAll(from, s.chainZapPixels / Legacy.PixelsPerUnit, 1 << GameLayers.Get(GameLayers.Enemy)))
            {
                Enemy t = c.GetComponentInParent<Enemy>();
                if (t != null && t != e && t.CanBeHit && !targets.Contains(t)) targets.Add(t);
            }
            targets.Sort((a, b) => ((Vector2)a.transform.position - from).sqrMagnitude.CompareTo(((Vector2)b.transform.position - from).sqrMagnitude));
            zapping = true;
            for (int i = 0; i < targets.Count && i < zaps; i++)
            {
                Enemy t = targets[i];
                if (t == null) continue;
                Vector2 to = t.transform.position;
                Bolt.Draw(from, to);
                t.TakeHit();
                Fx.Sparks(to, Vector2.zero, 0f, 6, 1f);
            }
            zapping = false;
            if (targets.Count > 0) Sound.Play(Sfx.BulletCancel, 0.8f, 1.6f);
        }
    }

    /// <summary>A jagged accent line of pixels that flickers out (Chain Zap).</summary>
    public class Bolt : MonoBehaviour
    {
        private float left = 0.14f;
        private SpriteRenderer[] dots;

        public static void Draw(Vector2 from, Vector2 to)
        {
            var go = new GameObject("Bolt");
            var b = go.AddComponent<Bolt>();
            float ppu = Legacy.PixelsPerUnit;
            float length = (to - from).magnitude * ppu;
            Vector2 dir = (to - from).normalized, across = new Vector2(-dir.y, dir.x);
            int joints = Mathf.Max(2, Mathf.RoundToInt(length / 8f));
            var points = new List<Vector2> { from };
            for (int i = 1; i < joints; i++)
                points.Add(Vector2.Lerp(from, to, (float)i / joints) + across * Random.Range(-3f, 3f) / ppu);
            points.Add(to);
            var list = new List<SpriteRenderer>();
            Sprite dot = PixelShapes.Dot(1, true);
            Material mat = BonusSettings.Current.material;
            for (int i = 0; i < points.Count - 1; i++)
            {
                float seg = (points[i + 1] - points[i]).magnitude * ppu;
                int steps = Mathf.Max(1, Mathf.CeilToInt(seg));
                for (int k = 0; k < steps; k++)
                {
                    Vector2 at = Vector2.Lerp(points[i], points[i + 1], (float)k / steps);
                    var d = new GameObject("p");
                    d.transform.SetParent(go.transform, false);
                    d.transform.position = new Vector3(Mathf.Round(at.x * ppu) / ppu, Mathf.Round(at.y * ppu) / ppu, 0f);
                    var sr = d.AddComponent<SpriteRenderer>();
                    sr.sprite = dot;
                    sr.sharedMaterial = mat;
                    sr.sortingOrder = 20;
                    list.Add(sr);
                }
            }
            b.dots = list.ToArray();
        }

        private void Update()
        {
            left -= Time.deltaTime;
            if (left <= 0f) { Destroy(gameObject); return; }
            bool on = Mathf.FloorToInt(left * 40f) % 2 == 0;
            foreach (SpriteRenderer d in dots) d.enabled = on;
        }
    }

    /// <summary>Orbiter perk: a small cell circling the player that hurts enemies it touches and eats their bullets.</summary>
    public class Orbiter : MonoBehaviour
    {
        private static readonly string[] Rows = { ".###.", "#RRR#", "#RRR#", "#RRR#", ".###." };
        private readonly Dictionary<Enemy, float> lastHit = new Dictionary<Enemy, float>();
        private int index, count;
        private float angle;

        public static Orbiter Create(int index, int count)
        {
            var go = new GameObject("Orbiter" + index);
            var o = go.AddComponent<Orbiter>();
            o.index = index;
            o.count = Mathf.Max(1, count);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = PixelShapes.FromRows("orbiter", Rows);
            sr.sharedMaterial = BonusSettings.Current.material;
            sr.sortingOrder = 8;
            return o;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            PlayerController p = PlayerController.Current;
            if (p == null) return;
            RoguelikeSettings s = RoguelikeSettings.Current;
            angle += s.orbiterDegreesPerSecond * dt;
            float a = (angle + index * 360f / count) * Mathf.Deg2Rad;
            float ppu = Legacy.PixelsPerUnit;
            Vector2 at = (Vector2)p.transform.position + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * s.orbiterRadiusPixels / ppu;
            transform.position = new Vector3(Mathf.Round(at.x * ppu) / ppu, Mathf.Round(at.y * ppu) / ppu, 0f);
            if (!p.IsAlive) return;

            int mask = (1 << GameLayers.Get(GameLayers.Enemy)) | (1 << GameLayers.Get(GameLayers.EnemyShot));
            foreach (Collider2D c in Physics2D.OverlapCircleAll(at, 3f / ppu, mask))
            {
                if (c.gameObject.layer == GameLayers.Get(GameLayers.EnemyShot))
                {
                    EnemyBullet bullet = c.GetComponent<EnemyBullet>();
                    if (bullet != null) { bullet.Kill(); Sound.Play(Sfx.BulletCancel, 0.6f); }
                    continue;
                }
                Enemy e = c.GetComponentInParent<Enemy>();
                if (e == null || !e.CanBeHit) continue;
                if (lastHit.TryGetValue(e, out float t) && Time.time - t < s.orbiterHitCooldown) continue;
                lastHit[e] = Time.time;
                e.TakeHit();
                e.Knockback(((Vector2)e.transform.position - (Vector2)p.transform.position).normalized
                            * ShotFeel.Current.knockbackPixelsPerSecond / ppu);
                Fx.Sparks(at, Vector2.zero, 0f, 5, 0.5f);
                Sound.Play(Sfx.EnemyHit, 0.6f, 1.2f);
            }
        }
    }
}
