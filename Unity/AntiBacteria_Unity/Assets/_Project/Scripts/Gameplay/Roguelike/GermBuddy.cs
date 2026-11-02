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
    /// Roguelike Germ Buddy: a small friendly bacterium that floats beside the player (left or right of the aim,
    /// a little behind), lags as it follows, and shoots the nearest enemy in range with the player's own bullets.
    /// Enemies and their bullets pass through it. Its upgrades are the buddy perks (RunModifiers.Buddy*).
    /// Spawned and removed by PerkEffects to match the Germ Buddy rank.
    /// </summary>
    public class GermBuddy : MonoBehaviour
    {
        private static readonly List<GermBuddy> Live = new List<GermBuddy>();
        private static readonly string[][] Frames =
        {
            new[] { "..###..", ".#...#.", "#.R.R.#", "#.....#", "#..R..#", ".#...#.", "..###..", ".#...#." },
            new[] { ".......", ".#####.", "#.R.R.#", "#.....#", "#..R..#", ".#####.", ".#...#.", "......." },
        };

        public int Slot { get; private set; }
        public Vector2 Position => pos;

        private SpriteRenderer sr;
        private Sprite[] sprites;
        private Vector2 pos, recoil;
        private float fireTimer, fetchTimer, age, blinkLeft;

        public static IReadOnlyList<GermBuddy> All => Live;

        public static GermBuddy Create(int slot)
        {
            PlayerController p = PlayerController.Current;
            Vector2 at = p != null ? (Vector2)p.transform.position : Vector2.zero;
            var go = new GameObject("GermBuddy" + slot);
            var b = go.AddComponent<GermBuddy>();
            b.Slot = slot;
            b.pos = at;
            b.sprites = new[] { PixelShapes.FromRows("buddy0", Frames[0]), PixelShapes.FromRows("buddy1", Frames[1]) };
            b.sr = go.AddComponent<SpriteRenderer>();
            b.sr.sprite = b.sprites[0];
            b.sr.sharedMaterial = BonusSettings.Current.material;
            SpriteRenderer body = p != null ? p.GetComponentInChildren<SpriteRenderer>() : null;
            b.sr.sortingOrder = body != null ? body.sortingOrder - 1 : 7;
            b.fireTimer = 0.3f;
            b.age = slot * 0.37f;
            go.transform.position = at;
            Fx.Sparks(at, Vector2.zero, 0f, 10, 0.5f);
            return b;
        }

        /// <summary>Bodyguard perk: the nearest buddy jumps onto the hit and takes it.</summary>
        public static void Guard(Vector2 at)
        {
            GermBuddy best = null;
            float bestD = float.MaxValue;
            foreach (GermBuddy b in Live)
            {
                float d = (b.pos - at).sqrMagnitude;
                if (d < bestD) { bestD = d; best = b; }
            }
            if (best == null) return;
            best.pos = at;
            best.blinkLeft = 0.6f;
            Fx.Sparks(at, Vector2.zero, 0f, 18, 1f, 0.3f);
        }

        private void OnEnable() => Live.Add(this);
        private void OnDisable() => Live.Remove(this);

        private void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            PlayerController p = PlayerController.Current;
            if (p == null) return;
            RoguelikeSettings s = RoguelikeSettings.Current;
            age += dt;

            // Beside the player, on its side of the aim and a bit behind; a slow bob; lags as it follows.
            Vector2 aim = p.AimDirection.sqrMagnitude > 0.01f ? p.AimDirection.normalized : Vector2.up;
            Vector2 side = Slot % 2 == 0 ? new Vector2(-aim.y, aim.x) : new Vector2(aim.y, -aim.x);
            Vector2 offset = (side - aim * 0.6f).normalized * s.buddyDistancePixels;
            offset.y += Mathf.Sin(age * 3f) * 1.5f;
            Vector2 home = (Vector2)p.transform.position + offset / Legacy.PixelsPerUnit;
            pos = Vector2.Lerp(pos, home, 1f - Mathf.Exp(-s.buddyFollow * dt));
            recoil = Vector2.Lerp(recoil, Vector2.zero, 1f - Mathf.Exp(-20f * dt));
            Vector2 shown = pos + recoil;
            transform.position = new Vector3(Mathf.Round(shown.x * Legacy.PixelsPerUnit) / Legacy.PixelsPerUnit,
                Mathf.Round(shown.y * Legacy.PixelsPerUnit) / Legacy.PixelsPerUnit, 0f);

            sr.sprite = sprites[Mathf.FloorToInt(age * 4f) % 2];
            if (blinkLeft > 0f) blinkLeft -= dt;
            sr.enabled = blinkLeft <= 0f || Mathf.FloorToInt(blinkLeft * 20f) % 2 == 0;

            if (!p.IsAlive || !p.ControlsEnabled) return;

            fireTimer -= dt;
            if (fireTimer <= 0f)
            {
                Enemy target = Nearest(s.buddyRangePixels);
                if (target != null) { Shoot(p, target); fireTimer = RunModifiers.BuddyFireInterval; }
                else fireTimer = 0.1f; // look again soon
            }

            if (RunModifiers.Fetch)
            {
                fetchTimer -= dt;
                if (fetchTimer <= 0f) { fetchTimer = 0.2f; Fetch(s.fetchPixels); }
            }
        }

        private Enemy Nearest(float rangePixels)
        {
            Enemy best = null;
            float bestD = float.MaxValue;
            foreach (Collider2D c in Physics2D.OverlapCircleAll(pos, rangePixels / Legacy.PixelsPerUnit, 1 << GameLayers.Get(GameLayers.Enemy)))
            {
                Enemy e = c.GetComponentInParent<Enemy>();
                if (e == null || !e.CanBeHit) continue;
                float d = ((Vector2)e.transform.position - pos).sqrMagnitude;
                if (d < bestD) { bestD = d; best = e; }
            }
            return best;
        }

        private void Shoot(PlayerController p, Enemy target)
        {
            Vector2 dir = ((Vector2)target.transform.position - pos).normalized;
            var mods = new ShotMods { damage = RunModifiers.BuddyDamage, pierce = RunModifiers.BuddyPierce, scale = 1 };
            if (RunModifiers.Copycat)
            {
                mods.critChance = RunModifiers.CritChance;
                mods.frostChance = RunModifiers.FrostChance;
                mods.bounces = RunModifiers.Bounces;
            }
            int lines = RunModifiers.BuddyLines;
            Vector2 across = new Vector2(-dir.y, dir.x) * RoguelikeSettings.Current.lineSpacingPixels / Legacy.PixelsPerUnit;
            Vector2 origin = pos + dir * (4f / Legacy.PixelsPerUnit);
            for (int k = 0; k < lines; k++) p.FireShot(origin + across * (k - (lines - 1) / 2f), dir, mods, 0.85f);
            recoil = -dir * (1.5f / Legacy.PixelsPerUnit);
            Sound.Play(Sfx.Shoot, 0.35f, 1.5f);
        }

        private void Fetch(float rangePixels)
        {
            foreach (Collider2D c in Physics2D.OverlapCircleAll(pos, rangePixels / Legacy.PixelsPerUnit, 1 << GameLayers.Get(GameLayers.Pickup)))
            {
                XpOrb orb = c.GetComponent<XpOrb>();
                if (orb != null) { orb.Call(); continue; }
                Pickups.Vitamin v = c.GetComponentInParent<Pickups.Vitamin>();
                if (v != null) v.Attract();
            }
        }
    }
}
