using System;
using System.Collections.Generic;
using AntiBacteria.Core;
using AntiBacteria.Gameplay.Player;
using AntiBacteria.Rendering;
using UnityEngine;

namespace AntiBacteria.Gameplay.Enemies
{
    /// <summary>Anything the level can spawn from an EnemyDefinition (a single enemy or a chain).</summary>
    public interface IEnemyRoot
    {
        void Init(EnemyDefinition definition);
    }

    /// <summary>
    /// One enemy, driven by its EnemyDefinition: fades in, moves (wander, homing, spiral, zig-zag, dash, dodge),
    /// shoots, spawns children, takes hits, dies and pays out. Ported from Agent/EnemyAgent and the group A
    /// classes (Docs/DesignReference/02). Per-update angle steps of the original are converted to degrees per
    /// second (x120, because the original updated every enemy twice a frame at 60 fps).
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class Enemy : MonoBehaviour, IEnemyRoot
    {
        public enum State { Spawning, Alive, Dead }

        [SerializeField] protected SpriteRenderer bodyRenderer;
        [Tooltip("Optional red pupil that slides toward the player (homing enemies never rotate their sprite).")]
        [SerializeField] protected SpriteRenderer eyeRenderer;
        [SerializeField] protected Collider2D hitbox;
        [Tooltip("Optional second look (Chorux defender with its shield down).")]
        [SerializeField] protected Sprite altSprite;

        public EnemyDefinition Definition { get; private set; }
        public State Current { get; private set; } = State.Spawning;

        /// <summary>Shots hit it but do no damage (Tentacle, chain body, shielded defender).</summary>
        public bool Invulnerable { get; set; }

        /// <summary>Hurts the player on contact.</summary>
        public bool IsHarmful => Current == State.Alive && !hidden;
        /// <summary>Player shots stop on it (even when it is invulnerable: it shields what is behind it).</summary>
        public bool BlocksShots => Current == State.Alive && !hidden;
        public bool CanBeHit => BlocksShots && !Invulnerable;

        public SpriteRenderer Body => bodyRenderer;
        public Rigidbody2D Rigidbody => rb;

        public int HitsLeft => hitsLeft;
        public int MaxHits { get; private set; } = 1;

        /// <summary>Speed in points/s replacing the definition's (difficulty, boss phases, Inferno's tentacles). 0 = none.</summary>
        public float SpeedOverridePoints { get; set; }

        /// <summary>Spin replacing the definition's, degrees/s (BigRedNoise spins up while spawning). Null = none.</summary>
        public float? SpinOverride { get; set; }

        /// <summary>Raised on every hit that did damage.</summary>
        public event Action<Enemy> Hurt;

        protected float SpeedPoints => (SpeedOverridePoints > 0f ? SpeedOverridePoints : Definition.speedPoints) * SpeedMultiplier;

        /// <summary>Every enemy moves this much faster (roguelike Mutation 10, Daily twists). Reset by each level.</summary>
        public static float SpeedMultiplier { get; set; } = 1f;
        protected float ShotInterval => EnemyDefinition.ForDifficulty(Definition.shotIntervalByDifficulty, Definition.shotIntervalSeconds) / Mathf.Max(0.1f, FireRateMultiplier);

        /// <summary>Every enemy shoots this much more often (later worlds and rooms, harder difficulties). Reset by each level.</summary>
        public static float FireRateMultiplier { get; set; } = 1f;

        /// <summary>Enemies alive right now.</summary>
        public static int AliveCount { get; private set; }

        public static event Action<Enemy> Killed;

        /// <summary>Every enemy's hits are scaled by this when set up (roguelike rooms; 1 elsewhere).</summary>
        public static float HealthMultiplier { get; set; } = 1f;

        /// <summary>Added to every non-boss enemy's hits (later worlds and rooms, harder difficulties).</summary>
        public static int ExtraHits { get; set; }

        /// <summary>No enemy takes fewer hits than this (2 on Hard and Impossible: nothing dies to one shot).</summary>
        public static int MinHits { get; set; } = 1;

        /// <summary>Resets the level-wide enemy tuning (each level and room sets its own).</summary>
        public static void ResetTuning()
        {
            HealthMultiplier = 1f;
            SpeedMultiplier = 1f;
            FireRateMultiplier = 1f;
            ExtraHits = 0;
            MinHits = 1;
        }

        private int Scaled(int hits)
        {
            bool boss = Definition != null && Definition.isBoss;
            // Chain segments come many at a time: half the extra hits, so a long chain isn't a wall of hits.
            bool segment = Definition != null && Definition.movement == EnemyMovement.Controlled;
            int extra = boss ? 0 : segment ? ExtraHits / 2 : ExtraHits;
            int h = Mathf.RoundToInt(hits * HealthMultiplier) + extra;
            return Mathf.Max(Mathf.Max(1, MinHits), h);
        }

        protected Rigidbody2D rb;
        protected bool hidden;
        protected int hitsLeft;
        private float fadeTime, fadeSeconds;
        private bool counted;
        protected SpriteFlash flash;

        /// <summary>Bosses pause the regular shot while they run a pattern of their own.</summary>
        protected bool PauseBaseShooting { get; set; }
        /// <summary>Multiplies the regular shot interval (boss phases).</summary>
        protected float ShotIntervalScale { get; set; } = 1f;

        private Vector2 target;
        private float legSpeed;
        private float orbitAngle, orbitSpeed, orbitRate;
        private float wobblePhase;
        private int dodgeSide = 1;
        private bool dodging;
        private float shotTimer, spawnTimer;
        private readonly List<Enemy> children = new List<Enemy>();

        protected virtual void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            flash = GetComponent<SpriteFlash>();
        }

        // ------------------------------------------------------------------ setup

        public virtual void Init(EnemyDefinition definition)
        {
            Definition = definition;
            hitsLeft = MaxHits = Scaled(definition.hits);
            Invulnerable = definition.invulnerable;
            if (!definition.isBoss && !definition.invulnerable) EnemyHealthBar.Attach(this);
            Current = State.Spawning;
            fadeTime = 0f;
            fadeSeconds = definition.fadeInSeconds;
            if (!counted) { AliveCount++; counted = true; }

            SpeedOverridePoints = EnemyDefinition.ForDifficulty(definition.speedPointsByDifficulty, 0f);
            legSpeed = SpeedPoints;
            PickTarget();
            RerollOrbit();
            orbitAngle = UnityEngine.Random.Range(0f, 360f);
            dodgeSide = UnityEngine.Random.value < 0.5f ? 1 : -1;
            shotTimer = ShotInterval;
            spawnTimer = definition.spawnIntervalSeconds;

            if (definition.fadeInSeconds > 0f)
            {
                if (hitbox != null) hitbox.enabled = false;
                SetAlpha(0f);
            }
            else
            {
                BecomeAlive();
            }
        }

        /// <summary>Switch to another definition without fading in again (Lotus petals breaking free).</summary>
        public void Reconfigure(EnemyDefinition definition)
        {
            Definition = definition;
            hitsLeft = MaxHits = Scaled(definition.hits);
            Invulnerable = definition.invulnerable;
            legSpeed = SpeedPoints;
            RerollOrbit();
            PickTarget();
        }

        public void SetHits(int hits) => hitsLeft = MaxHits = Scaled(hits);

        /// <summary>Change the body sprite and fit the hitbox to it (chain segments).</summary>
        public void SetSprite(Sprite sprite)
        {
            if (bodyRenderer == null || sprite == null) return;
            bodyRenderer.sprite = sprite;
            if (hitbox is CircleCollider2D c)
                c.radius = Mathf.Max(1f, sprite.rect.width / 2f - 1f) / Legacy.PixelsPerUnit;
        }

        public void UseAltSprite()
        {
            if (altSprite != null && bodyRenderer != null) bodyRenderer.sprite = altSprite;
        }

        private void BecomeAlive()
        {
            Current = State.Alive;
            SetAlpha(1f);
            if (hitbox != null) hitbox.enabled = !hidden;
        }

        // ------------------------------------------------------------------ per frame

        protected virtual void Update()
        {
            if (Definition == null || Current == State.Dead) return;
            float dt = Time.deltaTime;

            if (Current == State.Spawning)
            {
                fadeTime += dt;
                float t = fadeTime / fadeSeconds;
                SetAlpha(Mathf.Clamp01(t));
                if (t >= 1f) BecomeAlive();
            }

            float spin = SpinOverride ?? Definition.spinDegreesPerSecond;
            if (spin != 0f && bodyRenderer != null)
                bodyRenderer.transform.Rotate(0f, 0f, -spin * dt);

            UpdateEye();
            if (dodging) flash?.Flash(); // white while dodging, like the original Gnida

            if (Current != State.Alive || hidden) return;
            UpdateShooting(dt);
            UpdateSpawning(dt);
        }

        protected virtual void FixedUpdate()
        {
            if (Definition == null || Definition.movement == EnemyMovement.Controlled) return;
            if (Current != State.Alive) { rb.linearVelocity = Vector2.zero; return; }
            float slow = Time.time < slowUntil ? slowFactor : 1f; // Frost Shots perk
            rb.linearVelocity = DesiredVelocity(Time.fixedDeltaTime) * slow + knockback;
            knockback *= Mathf.Exp(-Effects.ShotFeel.Current.knockbackDecay * Time.fixedDeltaTime);
        }

        /// <summary>Move to a point by the next physics step (used by chains; keeps interpolation smooth).</summary>
        public void MoveTo(Vector2 position)
        {
            rb.linearVelocity = (position - rb.position) / Time.fixedDeltaTime;
        }

        public void Stop() => rb.linearVelocity = Vector2.zero;

        private Vector2 knockback;
        private float slowUntil, slowFactor = 1f;

        /// <summary>Move at 'factor' speed for a while (roguelike Frost Shots). Bosses ignore it.</summary>
        public void Slow(float factor, float seconds)
        {
            if (Definition == null || Current == State.Dead || Definition.isBoss) return;
            slowFactor = Mathf.Clamp(factor, 0.05f, 1f);
            slowUntil = Mathf.Max(slowUntil, Time.time + seconds);
        }

        public bool IsSlowed => Time.time < slowUntil;

        /// <summary>
        /// Shot feedback: a push (units/s) that fades out (Effects.ShotFeel). Bosses, stationary enemies and
        /// chain parts don't budge.
        /// </summary>
        public void Knockback(Vector2 velocity)
        {
            if (Definition == null || Current == State.Dead || Definition.isBoss) return;
            EnemyMovement m = Definition.movement;
            if (m == EnemyMovement.Stationary || m == EnemyMovement.Controlled) return;
            knockback += velocity;
        }

        // ------------------------------------------------------------------ movement

        protected virtual Vector2 DesiredVelocity(float dt)
        {
            EnemyDefinition d = Definition;
            Vector2 pos = rb.position;
            PlayerController p = PlayerController.Current;
            bool hasPlayer = p != null && p.IsAlive;
            Vector2 toPlayer = hasPlayer ? ((Vector2)p.transform.position - pos).normalized : Vector2.zero;
            float speed = Legacy.Units(SpeedPoints);
            Vector2 v;

            switch (d.movement)
            {
                case EnemyMovement.Homing:
                    v = toPlayer * speed;
                    break;

                case EnemyMovement.Spiral:
                    // Original SpiralCorosia: homing velocity plus radius*(cos a, sin a); radius and step re-rolled per turn.
                    v = toPlayer * speed + new Vector2(Mathf.Cos(orbitAngle * Mathf.Deg2Rad), Mathf.Sin(orbitAngle * Mathf.Deg2Rad)) * orbitSpeed;
                    orbitAngle += orbitRate * dt;
                    if (orbitAngle >= 360f) { orbitAngle -= 360f; RerollOrbit(); }
                    break;

                case EnemyMovement.ZigZag:
                    // Fix: the original wobbled along the fixed world diagonal; here it is across the heading.
                    v = toPlayer * speed + Perp(toPlayer) * Legacy.Units(d.wobbleSpeedPoints) * Mathf.Cos(wobblePhase * Mathf.Deg2Rad);
                    wobblePhase += d.wobbleDegreesPerSecond * dt;
                    break;

                case EnemyMovement.Dash:
                {
                    float s = Legacy.Units(legSpeed);
                    float arrive = Mathf.Max(Legacy.Units(d.arriveDistancePoints), s * dt * 1.5f);
                    if (Vector2.Distance(pos, target) <= arrive) PickTarget();
                    v = (target - pos).normalized * s;
                    break;
                }

                case EnemyMovement.Wander:
                    if (Vector2.Distance(pos, target) <= Mathf.Max(Legacy.Units(d.arriveDistancePoints), speed * dt * 1.5f)) PickTarget();
                    v = (target - pos).normalized * speed;
                    break;

                default:
                    v = Vector2.zero;
                    break;
            }

            dodging = false;
            if (d.dodgeWhenAimedAt && hasPlayer && p.IsShooting &&
                Vector2.Angle(p.AimDirection, pos - (Vector2)p.transform.position) <= d.dodgeConeDegrees)
            {
                // Fix: the original sidestep used an un-normalised vector (50 m/s); this is a tuned, true sidestep.
                v += Perp(toPlayer) * dodgeSide * Legacy.Units(d.dodgeSpeedPoints);
                dodging = true;
            }
            return v;
        }

        private static Vector2 Perp(Vector2 v) => new Vector2(-v.y, v.x);

        private void RerollOrbit()
        {
            if (Definition == null) return;
            orbitSpeed = Legacy.Units(UnityEngine.Random.Range(Definition.orbitSpeedPointsMin, Definition.orbitSpeedPointsMax));
            orbitRate = UnityEngine.Random.Range(Definition.orbitDegreesPerSecondMin, Definition.orbitDegreesPerSecondMax);
        }

        /// <summary>
        /// Wander / Dash target. The original took a random field point offset by ~510 pt (SpawnManager.getRandomPointAroundPoint),
        /// which could land outside the field; here it is a random point inside the arena.
        /// Dash: a share of the targets is where the player is right now (a snapshot, not a live track).
        /// </summary>
        private void PickTarget()
        {
            if (Definition == null) return;
            PlayerController p = PlayerController.Current;
            if (Definition.movement == EnemyMovement.Dash)
            {
                legSpeed = SpeedOverridePoints > 0f
                    ? SpeedOverridePoints
                    : UnityEngine.Random.Range(Definition.dashSpeedPointsMin, Definition.dashSpeedPointsMax);
                if (p != null && p.IsAlive && UnityEngine.Random.value < Definition.dashPlayerChance)
                {
                    target = p.transform.position;
                    return;
                }
            }
            target = Arena.Current != null ? Arena.Current.RandomPointInside(1f) : (Vector2)transform.position;
        }

        private void UpdateEye()
        {
            if (eyeRenderer == null || Definition == null || Definition.eyeOffsetPixels <= 0) return;
            PlayerController p = PlayerController.Current;
            Vector2 dir = p != null ? ((Vector2)p.transform.position - (Vector2)transform.position).normalized : Vector2.zero;
            Vector2 o = dir * Definition.eyeOffsetPixels;
            eyeRenderer.transform.localPosition = new Vector3(Mathf.Round(o.x), Mathf.Round(o.y), 0f) / Legacy.PixelsPerUnit;
        }

        // ------------------------------------------------------------------ shooting & spawning

        private void UpdateShooting(float dt)
        {
            EnemyDefinition d = Definition;
            if (d.shotPattern == ShotPattern.None || d.bulletPrefab == null || ShotInterval <= 0f || PauseBaseShooting) return;
            shotTimer -= dt;
            if (shotTimer > 0f) return;
            shotTimer += ShotInterval * ShotIntervalScale;
            Fire();
        }

        /// <summary>Fire one volley: a radial burst (ShootingCorosia 4, Sunux 8) or one aimed bullet.</summary>
        public void Fire()
        {
            EnemyDefinition d = Definition;
            Vector2 pos = transform.position;
            float offset = Legacy.Units(d.shotOffsetPoints), speed = Legacy.Units(d.shotSpeedPoints);
            if (d.shotPattern == ShotPattern.Aimed)
            {
                PlayerController p = PlayerController.Current;
                if (p == null || !p.IsAlive) return;
                Vector2 dir = ((Vector2)p.transform.position - pos).normalized;
                EnemyBullet.Spawn(d.bulletPrefab, pos + dir * offset, dir, speed, this);
                return;
            }
            float start = d.shotsFollowSpin && bodyRenderer != null ? bodyRenderer.transform.eulerAngles.z : 0f;
            for (int i = 0; i < d.shotCount; i++)
            {
                float a = (start + i * 360f / d.shotCount) * Mathf.Deg2Rad;
                var dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                // Fix: the original placed diagonal bullets closer (un-normalised offsets); all start at the same distance.
                EnemyBullet.Spawn(d.bulletPrefab, pos + dir * offset, dir, speed, this);
            }
        }

        private void UpdateSpawning(float dt)
        {
            EnemyDefinition d = Definition;
            if (d.spawnIntervalSeconds <= 0f || d.spawnIds == null || d.spawnIds.Length == 0) return;
            spawnTimer -= dt;
            if (spawnTimer > 0f) return;
            spawnTimer += d.spawnIntervalSeconds; // fix: the original halved this after any slow-motion toggle
            children.RemoveAll(c => c == null);
            if (children.Count >= d.maxChildren) return;
            int id = d.spawnIds[UnityEngine.Random.Range(0, d.spawnIds.Length)];
            GameObject go = EnemyCatalog.SpawnById(id, transform.position);
            Enemy child = go != null ? go.GetComponent<Enemy>() : null;
            if (child != null) children.Add(child);
        }

        // ------------------------------------------------------------------ damage & death

        /// <summary>One player bullet = one hit (the original ignored bullet damage values).</summary>
        public virtual void TakeHit()
        {
            if (!CanBeHit) return;
            hitsLeft--;
            flash?.Flash();
            UpdateDamageStage();
            Hurt?.Invoke(this);
            if (hitsLeft <= 0) OnLethalHit();
        }

        /// <summary>Several hits at once (Boom bonus). Stops early when it dies or becomes untouchable.</summary>
        public void TakeHits(int count)
        {
            for (int i = 0; i < count && CanBeHit && hitsLeft > 0; i++) TakeHit();
        }

        /// <summary>Bosses erode as they lose hits (replaces BigBoss going white to black and the others fading out).</summary>
        private void UpdateDamageStage()
        {
            Sprite[] stages = Definition.damageStages;
            if (stages == null || stages.Length == 0 || bodyRenderer == null) return;
            float lost = 1f - (float)Mathf.Max(0, hitsLeft) / MaxHits;
            Sprite s = stages[Mathf.Clamp(Mathf.FloorToInt(lost * stages.Length), 0, stages.Length - 1)];
            if (s != null && bodyRenderer.sprite != s) bodyRenderer.sprite = s;
        }

        /// <summary>Default: die and pay out. Chorux overrides (its core breaks instead).</summary>
        protected virtual void OnLethalHit() => Die(true);

        /// <summary>Score, kill count, vitamins, pop effect.</summary>
        protected void PayOut()
        {
            GameSession.AddScore(Definition.killScore);
            GameSession.AddKill();
            RewardSpawner.Instance?.DropVitamins(transform.position, Definition.vitamins);
            RewardSpawner.Instance?.SpawnPop(transform.position);
            Killed?.Invoke(this);
        }

        protected virtual void Die(bool killedByPlayer)
        {
            if (Current == State.Dead) return;
            Current = State.Dead;
            Uncount();
            if (killedByPlayer)
            {
                PayOut();
                EnemyBullet.ClearOwner(this); // the original removed a shooter's bullets when it was shot down
                foreach (Enemy c in children) if (c != null) c.Pop(); // a Spawner's brood dies with it
            }
            Destroy(gameObject);
        }

        /// <summary>Remove without rewards (level won / restarted).</summary>
        public void Despawn() => Die(false);

        /// <summary>Remove without rewards but with the pop effect (the rest of a dead chain, a dead nest's brood).</summary>
        public void Pop()
        {
            if (Current == State.Dead) return;
            RewardSpawner.Instance?.SpawnPop(transform.position);
            Die(false);
        }

        /// <summary>Invisible, harmless and intangible, but still moving (Chorux after its core breaks).</summary>
        protected void Hide()
        {
            hidden = true;
            if (bodyRenderer != null) bodyRenderer.enabled = false;
            if (eyeRenderer != null) eyeRenderer.enabled = false;
            if (hitbox != null) hitbox.enabled = false;
        }

        /// <summary>Start vanishing: harmless and intangible at once (the caller fades the alpha). Frunctus's blink.</summary>
        protected void Vanish()
        {
            hidden = true;
            if (hitbox != null) hitbox.enabled = false;
        }

        /// <summary>Come back somewhere else, fading in like a fresh spawn (harmless and unhittable until solid).</summary>
        protected void Reappear(Vector2 position, float seconds)
        {
            hidden = false;
            transform.position = position;
            rb.position = position;
            rb.linearVelocity = Vector2.zero;
            if (bodyRenderer != null) bodyRenderer.enabled = true;
            if (eyeRenderer != null) eyeRenderer.enabled = true;
            if (hitbox != null) hitbox.enabled = false;
            Current = State.Spawning;
            fadeTime = 0f;
            fadeSeconds = Mathf.Max(0.01f, seconds);
            SetAlpha(0f);
        }

        private void OnDestroy() => Uncount();

        private void Uncount()
        {
            if (!counted) return;
            counted = false;
            AliveCount--;
        }

        protected void SetAlpha(float a)
        {
            if (bodyRenderer != null) { Color c = bodyRenderer.color; c.a = a; bodyRenderer.color = c; }
            if (eyeRenderer != null) { Color c = eyeRenderer.color; c.a = a; eyeRenderer.color = c; }
        }
    }
}
