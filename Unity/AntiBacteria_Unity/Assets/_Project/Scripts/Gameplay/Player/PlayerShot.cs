using AntiBacteria.Audio;
using System;
using AntiBacteria.Core;
using AntiBacteria.Gameplay.Effects;
using AntiBacteria.Gameplay.Enemies;
using UnityEngine;

namespace AntiBacteria.Gameplay.Player
{
    /// <summary>
    /// One player bullet. Flies straight; one hit on an enemy, gone on a wall. Pooled by PlayerController.
    /// Original: 40 m/s, no lifetime (relied on the walls), every bullet could hurt the player (Docs 01, section 6).
    /// Feel (Effects.ShotFeel): starts as a round puff, then a pill with a faded trail; hits flash, throw sparks and
    /// push the enemy back; walls throw sparks off the wall; cancelling an enemy bullet pops red sparks.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerShot : MonoBehaviour
    {
        private int piercesLeft; // roguelike Piercing perk: enemies this shot can still pass through
        private ShotMods mods = ShotMods.Plain;
        private int bouncesLeft; // Ricochet perk
        private Rigidbody2D rb;
        private float lifeLeft, age;
        private Action<PlayerShot> release;
        private bool live;
        private SpriteRenderer body, trail;
        private Sprite pill;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            body = GetComponentInChildren<SpriteRenderer>();
            if (body == null) return;
            pill = body.sprite;

            ShotFeel f = ShotFeel.Current;
            if (f.shotTrail == null) return;
            var go = new GameObject("Trail");
            go.layer = gameObject.layer;
            go.transform.SetParent(transform, false);
            // Right behind the pill: half the pill plus half the trail, both pointing along +x.
            float back = (pill.rect.width + f.shotTrail.rect.width) / 2f / Legacy.PixelsPerUnit;
            go.transform.localPosition = new Vector3(-back, 0f, 0f);
            trail = go.AddComponent<SpriteRenderer>();
            trail.sprite = f.shotTrail;
            trail.sharedMaterial = body.sharedMaterial;
            trail.sortingOrder = body.sortingOrder - 1;
        }

        public void Launch(Vector2 position, Vector2 direction, float speed, float lifetime, Action<PlayerShot> releaser) =>
            Launch(position, direction, speed, lifetime, releaser, ShotMods.Player());

        /// <summary>Fire with roguelike modifiers (damage, pierce, bounces, crits, frost, size).</summary>
        public void Launch(Vector2 position, Vector2 direction, float speed, float lifetime, Action<PlayerShot> releaser, ShotMods m)
        {
            mods = m;
            transform.localScale = new Vector3(Mathf.Max(1, m.scale), Mathf.Max(1, m.scale), 1f);
            transform.SetPositionAndRotation(position, Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg));
            rb.position = position;
            rb.linearVelocity = direction * speed;
            lifeLeft = lifetime;
            age = 0f;
            release = releaser;
            live = true;
            piercesLeft = m.pierce; // roguelike Piercing perk
            bouncesLeft = m.bounces;
            UpdateLook();
        }

        private void Update()
        {
            lifeLeft -= Time.deltaTime;
            age += Time.deltaTime;
            UpdateLook();
            if (lifeLeft <= 0f) Release();
        }

        private void UpdateLook()
        {
            if (body == null) return;
            ShotFeel f = ShotFeel.Current;
            bool born = age >= f.birthSeconds || f.shotBirth == null;
            Sprite s = born ? pill : f.shotBirth;
            if (body.sprite != s) body.sprite = s;
            if (trail != null)
            {
                trail.enabled = born && f.trail;
                Color c = trail.color;
                if (!Mathf.Approximately(c.a, f.trailAlpha)) { c.a = f.trailAlpha; trail.color = c; }
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!live) return;
            int layer = other.gameObject.layer;
            ShotFeel f = ShotFeel.Current;
            Vector2 at = rb.position;
            Vector2 dir = rb.linearVelocity.sqrMagnitude > 0.0001f ? rb.linearVelocity.normalized : (Vector2)transform.right;

            if (layer == GameLayers.Get(GameLayers.Enemy))
            {
                Enemy enemy = other.GetComponentInParent<Enemy>();
                if (enemy == null || !enemy.BlocksShots) return; // fading in / broken Chorux core: pass through
                bool hurts = enemy.CanBeHit;
                // Heavy Shots / Buddy Punch (fractions roll), Critical Hit doubles.
                float raw = mods.damage;
                int damage = Mathf.Max(1, Mathf.FloorToInt(raw));
                float frac = raw - Mathf.Floor(raw);
                if (frac > 0f && UnityEngine.Random.value < frac) damage++;
                bool crit = hurts && mods.critChance > 0f && UnityEngine.Random.value < mods.critChance;
                if (crit) damage *= 2;
                if (hurts && damage > 1) enemy.TakeHits(damage);
                else enemy.TakeHit(); // does nothing to shields, chain bodies and Tentacles, but the shot is spent
                if (crit) { Fx.Sparks(at, dir, 120f, 10, 1f, 0.5f); Sound.Play(Sfx.EnemyHit, 1f, 1.5f); }
                if (hurts && mods.frostChance > 0f && UnityEngine.Random.value < mods.frostChance && enemy != null)
                {
                    RoguelikeSettings rs = RoguelikeSettings.Current;
                    enemy.Slow(rs.frostSlow, rs.frostSeconds);
                    Fx.Sparks(at, Vector2.zero, 0f, 6, 0f);
                }
                if (hurts) enemy.Knockback(dir * f.knockbackPixelsPerSecond / Legacy.PixelsPerUnit);
                Fx.Flash(f.hitFrames, at, 0f, 30f);
                Sound.Play(hurts ? Sfx.EnemyHit : Sfx.ShieldHit);
                // Sprays back toward the shooter; shields (no damage) spark white only.
                Fx.Sparks(at, -dir, 70f, f.enemyHitSparks, hurts ? 0.5f : 0f);
                if (hurts && piercesLeft > 0) { piercesLeft--; return; } // flies on through
                Release();
            }
            else if (layer == GameLayers.Get(GameLayers.Wall))
            {
                Vector2 normal = WallNormal(at, out Vector2 onWall);
                Fx.Flash(f.wallFrames, onWall, 0f, 25f);
                Sound.Play(Sfx.WallHit);
                // Bounce-ish: off the wall, between the reflection and straight out.
                Vector2 bounce = (Vector2.Reflect(dir, normal) + normal).normalized;
                Fx.Sparks(onWall + normal * 0.5f / Legacy.PixelsPerUnit, bounce, 60f, f.wallHitSparks, 0.3f, 0.15f);
                if (bouncesLeft > 0 && normal != Vector2.zero)
                {
                    // Ricochet perk: mirror off the wall and fly on.
                    bouncesLeft--;
                    Vector2 v = Vector2.Reflect(rb.linearVelocity, normal);
                    if (Vector2.Dot(v, normal) <= 0f) v = normal * v.magnitude;
                    rb.linearVelocity = v;
                    rb.position = onWall + normal * (2f / Legacy.PixelsPerUnit);
                    transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg);
                    return;
                }
                Release();
            }
            else if (layer == GameLayers.Get(GameLayers.EnemyShot))
            {
                // Shots cancel each other, as in 2013 (the player can shoot bullets down).
                other.GetComponent<EnemyBullet>()?.Kill();
                Sound.Play(Sfx.BulletCancel);
                Fx.Flash(f.hitFrames, at, 0f, 30f);
                Fx.Sparks(at, Vector2.zero, 0f, f.bulletCancelSparks, 1f);
                Release();
            }
        }

        /// <summary>Which arena wall was hit (the nearest edge), its inward normal, and the impact point on it.</summary>
        private static Vector2 WallNormal(Vector2 p, out Vector2 onWall)
        {
            onWall = p;
            if (Arena.Current == null) return Vector2.zero;
            Rect r = Arena.Current.Bounds;
            float left = p.x - r.xMin, right = r.xMax - p.x, bottom = p.y - r.yMin, top = r.yMax - p.y;
            float m = Mathf.Min(Mathf.Min(left, right), Mathf.Min(bottom, top));
            if (m == left) { onWall = new Vector2(r.xMin, p.y); return Vector2.right; }
            if (m == right) { onWall = new Vector2(r.xMax, p.y); return Vector2.left; }
            if (m == bottom) { onWall = new Vector2(p.x, r.yMin); return Vector2.up; }
            onWall = new Vector2(p.x, r.yMax);
            return Vector2.down;
        }

        private void Release()
        {
            if (!live) return;
            live = false;
            rb.linearVelocity = Vector2.zero;
            release?.Invoke(this);
        }
    }

    /// <summary>What a single bullet carries from the roguelike perks. Plain outside the roguelike.</summary>
    public struct ShotMods
    {
        public float damage;
        public int pierce, bounces, scale;
        public float critChance, frostChance;

        public static ShotMods Plain => new ShotMods { damage = 1f, scale = 1 };

        /// <summary>The player's own bullets with the run's perks.</summary>
        public static ShotMods Player() => new ShotMods
        {
            damage = RunModifiers.ShotDamage,
            pierce = RunModifiers.Pierce,
            bounces = RunModifiers.Bounces,
            scale = RunModifiers.ShotScale,
            critChance = RunModifiers.CritChance,
            frostChance = RunModifiers.FrostChance,
        };
    }
}
