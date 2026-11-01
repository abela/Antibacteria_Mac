using System;
using AntiBacteria.Core;
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

        [Header("Shooting (original: 0.1 s interval, 1280 pt/s, spawned 40 pt ahead)")]
        [SerializeField] private PlayerShot shotPrefab;
        [SerializeField] private float fireInterval = 0.1f;
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
        public int MaxLives => maxLives;
        public bool IsAlive => Lives > 0;
        public bool IsShooting { get; private set; }
        public Vector2 AimDirection { get; private set; } = Vector2.up;

        public event Action LivesChanged;
        public event Action Died;

        private Rigidbody2D rb;
        private Collider2D bodyCollider;
        private Camera cam;
        private ObjectPool<PlayerShot> shots;
        private float fireCooldown;
        private float invulnerableUntil = -1f;

        private void Awake()
        {
            Current = this;
            rb = GetComponent<Rigidbody2D>();
            bodyCollider = GetComponent<Collider2D>();
            Lives = maxLives;
            shots = new ObjectPool<PlayerShot>(
                () => { PlayerShot s = Instantiate(shotPrefab); s.gameObject.SetActive(false); return s; },
                s => s.gameObject.SetActive(true),
                s => s.gameObject.SetActive(false),
                s => { if (s != null) Destroy(s.gameObject); },
                false, 32, 512);
        }

        private void Start() => cam = Camera.main;

        private void OnDestroy()
        {
            if (Current == this) Current = null;
            shots?.Dispose();
        }

        private void Update()
        {
            if (!IsAlive || PauseController.IsPaused) return;
            UpdateAim();
            UpdateFire();
            UpdateBlink();
        }

        private void FixedUpdate()
        {
            if (!IsAlive) return;
            Vector2 target = GameInput.Move * Legacy.Units(maxSpeedPoints);
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

            if (nucleusRenderer != null)
            {
                Vector2 o = AimDirection * nucleusOffsetPixels;
                nucleusRenderer.transform.localPosition =
                    new Vector3(Mathf.Round(o.x), Mathf.Round(o.y), 0f) / Legacy.PixelsPerUnit;
            }
        }

        private void UpdateFire()
        {
            bool stickHeld = GameInput.UsingGamepad && GameInput.AimStick.magnitude > GameInput.AimDeadZone;
            IsShooting = GameInput.FireHeld || (gamepadAutoFire && stickHeld);

            fireCooldown -= Time.deltaTime;
            if (!IsShooting)
            {
                if (fireCooldown < 0f) fireCooldown = 0f;
                return;
            }
            while (fireCooldown <= 0f)
            {
                Fire();
                fireCooldown += fireInterval;
            }
        }

        private void Fire()
        {
            if (shotPrefab == null) return;
            Vector2 origin = (Vector2)transform.position + AimDirection * Legacy.Units(shotSpawnOffsetPoints);
            PlayerShot shot = shots.Get();
            shot.Launch(origin, AimDirection, Legacy.Units(shotSpeedPoints), shotLifetime, s => shots.Release(s));
        }

        /// <summary>Lose one life unless still blinking from the last hit.</summary>
        public void Hit()
        {
            if (!IsAlive || Time.time < invulnerableUntil) return;
            Lives--;
            LivesChanged?.Invoke();
            if (Lives <= 0) { Die(); return; }
            invulnerableUntil = Time.time + invulnerableSeconds;
            GetComponent<SpriteFlash>()?.Flash();
        }

        private void UpdateBlink()
        {
            bool blinking = Time.time < invulnerableUntil;
            bool visible = !blinking || Mathf.FloorToInt((invulnerableUntil - Time.time) / blinkInterval) % 2 == 0;
            if (bodyRenderer != null) bodyRenderer.enabled = visible;
            if (nucleusRenderer != null) nucleusRenderer.enabled = visible;
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
