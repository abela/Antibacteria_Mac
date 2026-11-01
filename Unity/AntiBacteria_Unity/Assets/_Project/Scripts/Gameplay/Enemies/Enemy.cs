using System;
using AntiBacteria.Core;
using AntiBacteria.Gameplay.Player;
using AntiBacteria.Rendering;
using UnityEngine;

namespace AntiBacteria.Gameplay.Enemies
{
    /// <summary>
    /// Base enemy: fades in, moves (wander or homing), takes hits, dies and pays out.
    /// Ported from Agent/EnemyAgent (Docs/DesignReference/02, section 2). Specific enemies with extra
    /// behaviour will subclass this.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class Enemy : MonoBehaviour
    {
        public enum State { Spawning, Alive, Dead }

        [SerializeField] protected SpriteRenderer bodyRenderer;
        [SerializeField] protected Collider2D hitbox;

        public EnemyDefinition Definition { get; private set; }
        public State Current { get; private set; } = State.Spawning;

        /// <summary>Hurts the player on contact.</summary>
        public bool IsHarmful => Current == State.Alive;
        /// <summary>Player shots hit it (they pass through while it is fading in, like the original).</summary>
        public bool CanBeHit => Current == State.Alive;

        /// <summary>Enemies alive right now (used by wave caps and boss levels).</summary>
        public static int AliveCount { get; private set; }

        public static event Action<Enemy> Killed;

        protected Rigidbody2D rb;
        private int hitsLeft;
        private float fadeTime;
        private Vector2 wanderTarget;
        private bool counted;

        protected virtual void Awake() => rb = GetComponent<Rigidbody2D>();

        public virtual void Init(EnemyDefinition definition)
        {
            Definition = definition;
            hitsLeft = Mathf.Max(1, definition.hits);
            Current = State.Spawning;
            fadeTime = 0f;
            if (hitbox != null) hitbox.enabled = false;
            SetAlpha(0f);
            AliveCount++;
            counted = true;
            PickWanderTarget();
        }

        protected virtual void Update()
        {
            if (Definition == null) return;
            if (Current == State.Spawning)
            {
                fadeTime += Time.deltaTime;
                float t = Definition.fadeInSeconds <= 0f ? 1f : fadeTime / Definition.fadeInSeconds;
                SetAlpha(Mathf.Clamp01(t));
                if (t >= 1f)
                {
                    Current = State.Alive;
                    if (hitbox != null) hitbox.enabled = true;
                }
            }
            if (Definition.spinDegreesPerSecond != 0f && bodyRenderer != null)
                bodyRenderer.transform.Rotate(0f, 0f, -Definition.spinDegreesPerSecond * Time.deltaTime);
        }

        protected virtual void FixedUpdate()
        {
            if (Current != State.Alive) { rb.linearVelocity = Vector2.zero; return; }
            rb.linearVelocity = DesiredVelocity();
        }

        /// <summary>Velocity in units/s for this physics step. Override for special movement.</summary>
        protected virtual Vector2 DesiredVelocity()
        {
            float speed = Legacy.Units(Definition.speedPoints);
            Vector2 pos = rb.position;
            switch (Definition.movement)
            {
                case EnemyMovement.Homing:
                {
                    PlayerController p = PlayerController.Current;
                    if (p == null || !p.IsAlive) return Vector2.zero;
                    return ((Vector2)p.transform.position - pos).normalized * speed;
                }
                default:
                {
                    if (Vector2.Distance(pos, wanderTarget) <= Legacy.Units(Definition.arriveDistancePoints)) PickWanderTarget();
                    return (wanderTarget - pos).normalized * speed;
                }
            }
        }

        /// <summary>
        /// Original: a random field point offset by ~510 pt in a random direction (SpawnManager.getRandomPointAroundPoint),
        /// which could land outside the field. Simplified to a random point inside the arena.
        /// </summary>
        protected void PickWanderTarget()
        {
            wanderTarget = Arena.Current != null ? Arena.Current.RandomPointInside(1f) : (Vector2)transform.position;
        }

        /// <summary>One player bullet = one hit (the original ignored bullet damage values).</summary>
        public virtual void TakeHit()
        {
            if (!CanBeHit) return;
            hitsLeft--;
            GetComponent<SpriteFlash>()?.Flash();
            if (hitsLeft <= 0) Die(true);
        }

        protected virtual void Die(bool killedByPlayer)
        {
            if (Current == State.Dead) return;
            Current = State.Dead;
            Uncount();
            if (killedByPlayer)
            {
                GameSession.AddScore(Definition.killScore);
                RewardSpawner.Instance?.DropVitamins(transform.position, Definition.vitamins);
                RewardSpawner.Instance?.SpawnPop(transform.position);
                Killed?.Invoke(this);
            }
            Destroy(gameObject);
        }

        /// <summary>Remove without rewards (level won / restarted).</summary>
        public void Despawn() => Die(false);

        private void OnDestroy() => Uncount();

        private void Uncount()
        {
            if (!counted) return;
            counted = false;
            AliveCount--;
        }

        private void SetAlpha(float a)
        {
            if (bodyRenderer == null) return;
            Color c = bodyRenderer.color;
            c.a = a;
            bodyRenderer.color = c;
        }
    }
}
