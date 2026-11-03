using AntiBacteria.Audio;
using System;
using AntiBacteria.Core;
using System.Collections.Generic;
using AntiBacteria.Gameplay.Bonuses;
using AntiBacteria.Gameplay.Effects;
using AntiBacteria.Gameplay.Roguelike;
using AntiBacteria.Gameplay.Weapons;
using AntiBacteria.Input;
using AntiBacteria.Rendering;
using UnityEngine;
using UnityEngine.Pool;

namespace AntiBacteria.Gameplay.Player
{
    /// <summary>
    /// The player cell: moves, aims (mouse or right stick), shoots, takes hits.
    /// Ported from MainCharacter.mm (Docs/DesignReference/01, section 2). Original values are kept in
    /// points/seconds and converted with <see cref="Legacy"/>.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement (original: impulse per frame + damping 6, top speed ~480 pt/s)")]
        [SerializeField] private float maxSpeedPoints = 480f;
        [Tooltip("How fast velocity reaches the target. 6 matches the original damping (90 % in ~0.37 s).")]
        [SerializeField] private float responsiveness = 6f;

        [Header("Shooting (original: 1280 pt/s, spawned 40 pt ahead; rates and patterns: WeaponSettings)")]
        [SerializeField] private PlayerShot shotPrefab;
        [Tooltip("The player's own shot when no weapon bonus is active (the upgrade shop changes it).")]
        [SerializeField] private ShotKind baseShot = ShotKind.Normal;
        [SerializeField] private float shotSpeedPoints = 1280f;
        [SerializeField] private float shotSpawnOffsetPoints = 40f;
        [SerializeField] private float shotLifetime = 3f;
        [Tooltip("Twin-stick: fire automatically while the right stick is held.")]
        [SerializeField] private bool gamepadAutoFire = true;

        [Header("Lives (original: 3 lives, 2 s blinking after a hit)")]
        [SerializeField] private int maxLives = 3;
        [SerializeField] private float invulnerableSeconds = 2f;
        [SerializeField] private float blinkInterval = 0.1f;

        [Header("Visual")]
        [SerializeField] private SpriteRenderer bodyRenderer;
        [SerializeField] private SpriteRenderer nucleusRenderer;
        [Tooltip("How far (pixels) the nucleus shifts toward the aim. The cell itself never rotates.")]
        [SerializeField] private float nucleusOffsetPixels = 2f;

        public static PlayerController Current { get; private set; }

        public int Lives { get; private set; }
        public int MaxLives => maxLives + RunModifiers.ExtraMaxLives; // roguelike Vitality perk
        public bool IsAlive => Lives > 0;
        public bool IsShooting { get; private set; }

        /// <summary>Timed bonuses (and later perks) on this player.</summary>
        public PlayerBuffs Buffs { get; private set; }

        /// <summary>The player's own shot type (upgrade shop).</summary>
        public ShotKind BaseShot { get => baseShot; set => baseShot = value; }

        /// <summary>The shot being fired now: a weapon bonus if one is active, otherwise the base shot.</summary>
        public ShotKind CurrentShot => Buffs != null && Buffs.Weapon.HasValue ? Buffs.Weapon.Value : baseShot;

        /// <summary>Bullets per volley for Normal / Water (campaign: grows with the level number).</summary>
        public int VolleySize { get; set; } = 1;
        public Vector2 AimDirection { get; private set; } = Vector2.up;

        /// <summary>Off during the level-over interlude (2013 bug: the player could keep acting after "You Won").</summary>
        public bool ControlsEnabled { get; set; } = true;

        /// <summary>Off in Peace Walker, where the player can only dodge.</summary>
        public bool CanShoot { get; set; } = true;

        public event Action LivesChanged;
        public event Action Died;

        /// <summary>Any player's volley left the barrel (the crosshair kicks).</summary>
        public static event Action Fired;

        private Rigidbody2D rb;
        private Collider2D bodyCollider;
        private Camera cam;
        private ObjectPool<PlayerShot> shots;
        private float fireCooldown;
        private float invulnerableUntil = -1f;
        private bool fireBlockedUntilRelease;
        private bool leftBarrel;
        private float recoilLeft;
        private float spinAngle; // Tornado: keeps turning across activations, as in 2013
        private readonly List<float> volleyAngles = new List<float>(40);
        private int volleysFired; // Overdrive perk counts volleys

        /// <summary>The bullet prefab (the Germ Buddy fires the same bullets through FireShot).</summary>
        public PlayerShot ShotPrefab => shotPrefab;

        private void Awake()
        {
            Current = this;
            rb = GetComponent<Rigidbody2D>();
            bodyCollider = GetComponent<Collider2D>();
            Lives = MaxLives;
            Buffs = GetComponent<PlayerBuffs>();
            if (Buffs == null) Buffs = gameObject.AddComponent<PlayerBuffs>();
            shots = new ObjectPool<PlayerShot>(
                () => { PlayerShot s = Instantiate(shotPrefab); s.gameObject.SetActive(false); return s; },
                s => s.gameObject.SetActive(true),
                s => s.gameObject.SetActive(false),
                s => { if (s != null) Destroy(s.gameObject); },
                false, 64, 2048); // Circle and Tornado put a lot of bullets in the air
        }

        private void Start()
        {
            cam = Camera.main;
            baseShot = Progress.SelectedShot; // upgrade shop
        }

        private void OnDestroy()
        {
            if (Current == this) Current = null;
            shots?.Dispose();
        }

        private void Update()
        {
            if (!IsAlive || PauseController.IsPaused) return;
            UpdateBlink();
            if (!ControlsEnabled) { IsShooting = false; return; }
            UpdateAim();
            UpdateFire();
        }

        private void FixedUpdate()
        {
            if (!IsAlive) return;
            // The bought Double Speed and the bonus don't stack (2013: the bonus did nothing with the upgrade on).
            float upgrade = Progress.DoubleSpeedOn ? ShopSettings.Current.doubleSpeedMultiplier : 1f;
            float speed = Legacy.Units(maxSpeedPoints) * Mathf.Max(upgrade, Buffs != null ? Buffs.SpeedMultiplier : 1f) * RunModifiers.SpeedMultiplier;
            Vector2 target = ControlsEnabled ? GameInput.Move * speed : Vector2.zero;
            rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, target, 1f - Mathf.Exp(-responsiveness * Time.fixedDeltaTime));
        }

        private void UpdateAim()
        {
            if (GameInput.UsingGamepad)
            {
                Vector2 stick = GameInput.AimStick;
                if (stick.magnitude > GameInput.AimDeadZone) AimDirection = stick.normalized;
            }
            else if (cam != null)
            {
                Vector2 mouse = cam.ScreenToWorldPoint(GameInput.MouseScreenPosition);
                Vector2 d = mouse - (Vector2)transform.position;
                if (d.sqrMagnitude > 0.0001f) AimDirection = d.normalized;
            }

            // Recoil: the sprites (never the body) kick back for a moment after each shot.
            recoilLeft -= Time.deltaTime;
            Vector2 recoil = recoilLeft > 0f ? -AimDirection * ShotFeel.Current.recoilPixels : Vector2.zero;
            if (bodyRenderer != null)
                bodyRenderer.transform.localPosition =
                    new Vector3(Mathf.Round(recoil.x), Mathf.Round(recoil.y), 0f) / Legacy.PixelsPerUnit;
            if (nucleusRenderer != null)
            {
                Vector2 o = AimDirection * nucleusOffsetPixels + recoil;
                nucleusRenderer.transform.localPosition =
                    new Vector3(Mathf.Round(o.x), Mathf.Round(o.y), 0f) / Legacy.PixelsPerUnit;
            }
        }

        private void UpdateFire()
        {
            if (!CanShoot) { IsShooting = false; return; }
            bool stickHeld = GameInput.UsingGamepad && GameInput.AimStick.magnitude > GameInput.AimDeadZone;
            // The click / key that closed the pause menu is still held: don't let it fire until released.
            if (PauseController.JustResumed) fireBlockedUntilRelease = true;
            if (fireBlockedUntilRelease && !GameInput.FireHeld) fireBlockedUntilRelease = false;
            IsShooting = !fireBlockedUntilRelease &&
                         (GameSettings.AutoShoot || GameInput.FireHeld || (gamepadAutoFire && stickHeld));

            fireCooldown -= Time.deltaTime;
            if (!IsShooting)
            {
                if (fireCooldown < 0f) fireCooldown = 0f;
                return;
            }
            ShotPattern pattern = WeaponSettings.Current.Get(CurrentShot);
            if (pattern == null) return;
            while (fireCooldown <= 0f)
            {
                Fire(pattern);
                fireCooldown += pattern.intervalSeconds * RunModifiers.FireIntervalMultiplier; // Rapid Fire perk
            }
        }

        private void Fire(ShotPattern pattern)
        {
            if (shotPrefab == null) return;
            Fired?.Invoke();
            ShotFeel f = ShotFeel.Current;
            Vector2 pos = transform.position;
            float aimAngle = Mathf.Atan2(AimDirection.y, AimDirection.x) * Mathf.Rad2Deg;
            pattern.Angles(aimAngle, VolleySize, ref spinAngle, volleyAngles);

            // Not a laser: a little spread and speed variation, alternating left / right barrel (aimed patterns only).
            leftBarrel = !leftBarrel;
            Vector2 side = pattern.feelSpread
                ? new Vector2(-AimDirection.y, AimDirection.x) * (leftBarrel ? 1f : -1f) * f.barrelOffsetPixels / Legacy.PixelsPerUnit
                : Vector2.zero;
            ShotMods mods = ShotMods.Player();
            foreach (float angle in volleyAngles)
            {
                float a = (angle + (pattern.feelSpread ? UnityEngine.Random.Range(-f.spreadDegrees, f.spreadDegrees) : 0f)) * Mathf.Deg2Rad;
                var dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                Vector2 origin = pos + dir * Legacy.Units(shotSpawnOffsetPoints) + side;
                float jitter = pattern.feelSpread ? UnityEngine.Random.Range(-f.speedJitter, f.speedJitter) : 0f;
                // Extra Bullet perk: parallel lines side by side (aimed patterns only).
                int lines = pattern.feelSpread ? 1 + RunModifiers.ExtraLines : 1;
                Vector2 across = new Vector2(-dir.y, dir.x) * RoguelikeSettings.Current.lineSpacingPixels / Legacy.PixelsPerUnit;
                for (int k = 0; k < lines; k++)
                {
                    Vector2 o = origin + across * (k - (lines - 1) / 2f);
                    PlayerShot shot = shots.Get();
                    shot.Launch(o, dir, Legacy.Units(shotSpeedPoints) * (1f + jitter), shotLifetime, s => shots.Release(s), mods);
                }
            }
            volleysFired++;
            RoguelikeSettings rs = RoguelikeSettings.Current;
            if (RunModifiers.Overdrive && pattern.muzzle && rs.overdriveEvery > 0 && volleysFired % rs.overdriveEvery == 0)
            {
                // Overdrive perk: a huge piercing blast down the aim.
                ShotMods big = mods;
                big.damage = mods.damage * Mathf.Max(1, rs.overdriveDamage);
                big.pierce = 99;
                big.bounces = 0;
                big.scale = Mathf.Max(1, rs.overdriveScale);
                FireShot(pos + AimDirection * Legacy.Units(shotSpawnOffsetPoints), AimDirection, big, 0.8f);
                Sound.Play(Sfx.ShootHeavy, 1f, 0.8f);
                CameraFollow.Shake(2f, 0.12f);
            }
            Sound.Play(pattern.layout == ShotPattern.Layout.Ring ? Sfx.ShootHeavy : Sfx.Shoot);
            if (!pattern.muzzle) return;

            if (f.muzzleFlash && f.muzzleFrames.Length > 0)
            {
                float half = f.muzzleFrames[0] != null ? f.muzzleFrames[0].rect.width / 2f : 3f;
                Vector2 at = pos + AimDirection * (f.muzzleDistancePixels + half) / Legacy.PixelsPerUnit;
                Fx.Flash(f.muzzleFrames, at, aimAngle, f.muzzleFlashFps, transform);
            }
            recoilLeft = f.recoilSeconds;
            // Fast weapons kick less per volley, so a hose rumbles instead of shoving the camera to its limit.
            CameraFollow.Kick(-AimDirection * f.cameraKickPixels * Mathf.Min(1f, pattern.intervalSeconds / 0.1f));
            if (f.fireShakePixels > 0f) CameraFollow.Shake(f.fireShakePixels, 0.06f);
        }

        /// <summary>One bullet from this player's pool (the Germ Buddy's shots, the Overdrive blast).</summary>
        public void FireShot(Vector2 origin, Vector2 direction, ShotMods mods, float speedScale = 1f)
        {
            if (shotPrefab == null) return;
            PlayerShot shot = shots.Get();
            shot.Launch(origin, direction.normalized, Legacy.Units(shotSpeedPoints) * speedScale, shotLifetime, s => shots.Release(s), mods);
        }

        /// <summary>Lose one life unless still blinking from the last hit.</summary>
        public void Hit()
        {
            if (!IsAlive || Time.time < invulnerableUntil) return;
            if (Buffs != null && Buffs.IsImmortal) return;
            if (Progress.ImmortalityOn) return; // bought Immortality: never lose a life (2013 upgrade)
            if (RoguelikeRun.Active && RoguelikeRun.ShieldsLeft > 0)
            {
                // Shield perk: this hit is blocked (once per room per rank).
                RoguelikeRun.ShieldsLeft--;
                Sound.Play(Sfx.ShieldHit);
                Fx.Sparks(transform.position, Vector2.zero, 0f, 16, 0.2f);
                invulnerableUntil = Time.time + invulnerableSeconds;
                GetComponent<SpriteFlash>()?.Flash();
                return;
            }
            if (RoguelikeRun.Active && RunModifiers.Bodyguard && RoguelikeRun.BodyguardLeft > 0)
            {
                // Bodyguard perk: the Germ Buddy jumps in and takes this hit (once per room).
                RoguelikeRun.BodyguardLeft--;
                GermBuddy.Guard(transform.position);
                Sound.Play(Sfx.ShieldHit);
                invulnerableUntil = Time.time + invulnerableSeconds;
                GetComponent<SpriteFlash>()?.Flash();
                return;
            }
            Lives--;
            if (Lives <= 0 && RunModifiers.CanRevive) { RoguelikeRun.ReviveUsed = true; Lives = 1; } // Second Wind perk
            LivesChanged?.Invoke();
            if (RunModifiers.NovaOnHit && Buffs != null) Buffs.Apply(BonusType.Boom); // Nova perk
            if (Lives <= 0) { Sound.Play(Sfx.PlayerDie); Die(); return; }
            Sound.Play(Sfx.PlayerHurt);
            invulnerableUntil = Time.time + invulnerableSeconds;
            GetComponent<SpriteFlash>()?.Flash();
        }

        /// <summary>+1 life, never above the maximum (Life bonus).</summary>
        public void Heal(int lives)
        {
            if (!IsAlive) return;
            Lives = Mathf.Min(MaxLives, Lives + lives);
            LivesChanged?.Invoke();
        }

        /// <summary>Roguelike: start a room with the lives carried from the last one.</summary>
        public void SetLives(int lives)
        {
            Lives = Mathf.Clamp(lives, 1, MaxLives);
            LivesChanged?.Invoke();
        }

        private void UpdateBlink()
        {
            bool blinking = Time.time < invulnerableUntil;
            bool visible = !blinking || Mathf.FloorToInt((invulnerableUntil - Time.time) / blinkInterval) % 2 == 0;
            // Immortality bonus: see-through (2013: opacity 50 of 255).
            float alpha = Buffs != null && Buffs.IsImmortal ? BonusSettings.Current.immortalAlpha : 1f;
            foreach (SpriteRenderer r in new[] { bodyRenderer, nucleusRenderer })
            {
                if (r == null) continue;
                r.enabled = visible;
                if (!Mathf.Approximately(r.color.a, alpha)) { Color c = r.color; c.a = alpha; r.color = c; }
            }
        }

        private void Die()
        {
            rb.linearVelocity = Vector2.zero;
            rb.simulated = false;
            if (bodyCollider != null) bodyCollider.enabled = false;
            if (bodyRenderer != null) bodyRenderer.enabled = false;
            if (nucleusRenderer != null) nucleusRenderer.enabled = false;
            Died?.Invoke();
        }

        // Enemies are triggers. Stay (not only Enter) so overlapping an enemy keeps hurting after the
        // blink ends - the original only hurt on first contact (Docs QUIRK, fixed).
        private void OnTriggerEnter2D(Collider2D other) => HandleContact(other);
        private void OnTriggerStay2D(Collider2D other) => HandleContact(other);

        private void HandleContact(Collider2D other)
        {
            if (!IsAlive) return;
            int layer = other.gameObject.layer;
            if (layer == GameLayers.Get(GameLayers.Enemy))
            {
                var enemy = other.GetComponentInParent<Enemies.Enemy>();
                if (enemy != null && enemy.IsHarmful) Hit();
            }
            else if (layer == GameLayers.Get(GameLayers.EnemyShot))
            {
                Hit();
            }
        }
    }
}
