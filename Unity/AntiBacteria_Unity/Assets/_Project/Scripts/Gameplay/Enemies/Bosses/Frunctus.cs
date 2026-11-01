using System.Collections;
using System.Collections.Generic;
using AntiBacteria.Core;
using AntiBacteria.Gameplay.Player;
using UnityEngine;

namespace AntiBacteria.Gameplay.Enemies.Bosses
{
    /// <summary>
    /// Boss 5, Frunctus (ID 26, level 50, the final boss).
    ///
    /// 2013 base behaviour, kept in every phase: loops toward the player (homing 6 m/s + a 10-20 m/s orbit),
    /// sidesteps when aimed at while the player fires (45 degree cone), fires aimed bullets (1.5 / 0.7 / 0.3 s by
    /// difficulty) and, whenever it gets near a wall, dashes at 80 m/s to a point 500-600 pt from the player.
    /// Frunctus.mm, Docs/DesignReference/03 section 6.5. Fixes: 150 hits (2013 shipped with 1); the dash stops
    /// exactly on its target instead of overshooting.
    ///
    /// New for the port (the original final boss was the easiest boss in the game): three phases with
    /// telegraphed attacks between stretches of the base behaviour. Every attack flashes first; numbers scale with
    /// difficulty (arrays are [Normal, Hard, Impossible]).
    ///   Phase 1 (100-67%): aimed fan volleys.
    ///   Phase 2 (67-33%):  fan, lunge (shakes, charges through where you stood, bursts a ring), bullet spiral,
    ///                      brood (summons Gnida).
    ///   Phase 3 (33-0%):   faster and shoots more; lunge chains, spiral, brood, blink (fades out, fades back in
    ///                      near you - harmless until solid - and fires on arrival).
    /// Between phases it shields itself (the bar shows it), shakes and fires two offset rings.
    /// </summary>
    public class Frunctus : BossEnemy
    {
        private const float EdgeMarginPoints = 40f;
        private const float DashSpeedPoints = 80f * 32f;
        private const float LungeSpeedPoints = 70f * 32f;
        private const float LungeOvershootPoints = 200f; // lands ~40 px past you: room to sidestep the ring
        private const float EscapeMinPoints = 500f, EscapeMaxPoints = 600f;
        private const float BlinkMinPoints = 300f, BlinkMaxPoints = 420f; // stays on screen
        private const int MinionId = 20; // Gnida
        private const float FlashEvery = 0.12f;

        private enum Mode { Free, Hold, Lunge, Gone }
        private enum Attack { Fan, Lunge, Spiral, Brood, Blink }

        // [Normal, Hard, Impossible]
        private static readonly int[] FanBullets = { 3, 5, 7 };
        private static readonly int[] FanVolleys = { 2, 3, 3 };
        private static readonly float[] LungeWarn = { 0.75f, 0.6f, 0.5f };
        private static readonly int[] LungeRing = { 10, 14, 18 };
        private static readonly int[] LungesPhase2 = { 1, 1, 2 };
        private static readonly int[] LungesPhase3 = { 2, 2, 3 };
        private static readonly int[] SpiralArms = { 2, 3, 4 };
        private static readonly float[] SpiralSeconds = { 1.8f, 2.2f, 2.6f };
        private static readonly float[] SpiralEvery = { 0.14f, 0.12f, 0.1f };
        private static readonly int[] BroodCount = { 2, 3, 4 };
        private static readonly int[] BroodMax = { 3, 4, 6 };
        private static readonly int[] ShiftRing = { 12, 16, 20 };
        // Rest between attacks, [phase, difficulty]: the base behaviour (aimed shots, dodging) runs meanwhile.
        private static readonly float[,] Rest = { { 3.0f, 2.5f, 2.0f }, { 2.2f, 1.8f, 1.5f }, { 1.6f, 1.3f, 1.0f } };

        private static readonly Attack[] Phase1Attacks = { Attack.Fan };
        private static readonly Attack[] Phase2Attacks = { Attack.Fan, Attack.Lunge, Attack.Spiral, Attack.Brood };
        private static readonly Attack[] Phase3Attacks = { Attack.Lunge, Attack.Spiral, Attack.Brood, Attack.Blink, Attack.Blink };

        /// <summary>0, 1 or 2.</summary>
        public int Phase { get; private set; }

        private Mode mode = Mode.Free;
        private bool evading;
        private Vector2 escapePoint;
        private Vector2 lungeTarget;
        private bool lungeArrived;
        private Attack? last;
        private float spiralSign = 1f;
        private Coroutine brain;
        private Vector3 bodyHome;
        private readonly List<Enemy> minions = new List<Enemy>();

        private int D => Mathf.Clamp((int)GameSession.Difficulty, 0, 2);
        private static bool PlayerAlive => PlayerController.Current != null && PlayerController.Current.IsAlive;

        protected override void Awake()
        {
            base.Awake();
            if (bodyRenderer != null) bodyHome = bodyRenderer.transform.localPosition;
        }

        public override void Init(EnemyDefinition definition)
        {
            base.Init(definition);
            // 2013 ignored the edge spawn point and appeared 500-600 pt from the player.
            Vector2 p = PointNearPlayer(EscapeMinPoints, EscapeMaxPoints);
            transform.position = p;
            rb.position = p;
            Phase = 0;
            Restart(false);
        }

        public override void TakeHit()
        {
            base.TakeHit();
            if (IsDefeated) return;
            int p = Health01 <= 1f / 3f ? 2 : Health01 <= 2f / 3f ? 1 : 0;
            if (p <= Phase) return;
            Phase = p;
            Restart(true);
        }

        protected override void OnDefeated()
        {
            StopAllCoroutines();
            foreach (Enemy m in minions) if (m != null) m.Pop();
            minions.Clear();
        }

        // ------------------------------------------------------------------ movement

        protected override Vector2 DesiredVelocity(float dt)
        {
            Vector2 pos = rb.position;
            switch (mode)
            {
                case Mode.Hold:
                case Mode.Gone:
                    return Vector2.zero;

                case Mode.Lunge:
                {
                    float speed = Legacy.Units(LungeSpeedPoints);
                    Vector2 to = lungeTarget - pos;
                    if (to.magnitude <= speed * dt) { lungeArrived = true; mode = Mode.Hold; return to / dt; }
                    return to.normalized * speed;
                }
            }

            if (evading)
            {
                float step = Legacy.Units(DashSpeedPoints) * dt;
                Vector2 to = escapePoint - pos;
                if (to.magnitude <= step) { evading = false; return to / dt; } // land exactly on it
                return to.normalized * Legacy.Units(DashSpeedPoints);
            }

            if (Arena.Current != null)
            {
                Rect r = Arena.Current.Bounds;
                float m = Legacy.Units(EdgeMarginPoints);
                if (pos.x < r.xMin + m || pos.x > r.xMax - m || pos.y < r.yMin + m || pos.y > r.yMax - m)
                {
                    evading = true;
                    escapePoint = PointNearPlayer(EscapeMinPoints, EscapeMaxPoints);
                    return (escapePoint - pos).normalized * Legacy.Units(DashSpeedPoints);
                }
            }
            return base.DesiredVelocity(dt); // spiral homing + dodge, from the definition
        }

        /// <summary>A point a given distance from the player, kept inside the arena (SpawnManager.getValidSpawnPosFromCharacter).</summary>
        private Vector2 PointNearPlayer(float minPoints, float maxPoints)
        {
            PlayerController p = PlayerController.Current;
            Vector2 from = p != null ? (Vector2)p.transform.position : Vector2.zero;
            Arena arena = Arena.Current;
            float margin = Legacy.Units(EdgeMarginPoints) * 2f;
            for (int tries = 0; tries < 12; tries++)
            {
                float a = Random.Range(0f, Mathf.PI * 2f);
                Vector2 c = from + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * Legacy.Units(Random.Range(minPoints, maxPoints));
                if (arena == null) return c;
                Vector2 inside = arena.Clamp(c, margin);
                if ((inside - c).sqrMagnitude < 0.01f) return c;
            }
            return arena != null ? arena.Clamp(from + Vector2.one * 5f, margin) : from;
        }

        // ------------------------------------------------------------------ attack loop

        private void Restart(bool phaseShift)
        {
            if (brain != null) StopCoroutine(brain);
            Release();
            brain = StartCoroutine(Brain(phaseShift));
        }

        /// <summary>Back to the base behaviour.</summary>
        private void Release()
        {
            mode = Mode.Free;
            PauseBaseShooting = false;
            if (bodyRenderer != null) bodyRenderer.transform.localPosition = bodyHome;
        }

        private IEnumerator Brain(bool phaseShift)
        {
            while (Current != State.Alive) yield return null;
            if (phaseShift) yield return PhaseShift();
            else yield return new WaitForSeconds(1.5f);

            while (true)
            {
                if (!PlayerAlive || Current != State.Alive) { yield return null; continue; }
                Attack a = Pick();
                last = a;
                switch (a)
                {
                    case Attack.Fan: yield return FanAttack(); break;
                    case Attack.Lunge: yield return LungeAttack(Phase >= 2 ? LungesPhase3[D] : LungesPhase2[D]); break;
                    case Attack.Spiral: yield return SpiralAttack(); break;
                    case Attack.Brood: yield return BroodAttack(); break;
                    case Attack.Blink: yield return BlinkAttack(); break;
                }
                Release();
                yield return new WaitForSeconds(Rest[Phase, D]);
            }
        }

        private Attack Pick()
        {
            Attack[] pool = Phase >= 2 ? Phase3Attacks : Phase == 1 ? Phase2Attacks : Phase1Attacks;
            minions.RemoveAll(m => m == null || m.Current == State.Dead);
            for (int tries = 0; tries < 8; tries++)
            {
                Attack a = pool[Random.Range(0, pool.Length)];
                if (pool.Length > 1 && last.HasValue && a == last.Value) continue;
                if (a == Attack.Brood && minions.Count >= BroodMax[D]) continue;
                return a;
            }
            return pool[0];
        }

        /// <summary>Stop, flash (and shake) so the player sees something is coming.</summary>
        private IEnumerator Telegraph(float seconds, bool shake)
        {
            mode = Mode.Hold;
            PauseBaseShooting = true;
            float t = 0f, nextFlash = 0f;
            while (t < seconds)
            {
                if (t >= nextFlash) { flash?.Flash(); nextFlash += FlashEvery; }
                if (shake && bodyRenderer != null)
                    bodyRenderer.transform.localPosition = bodyHome +
                        new Vector3(Random.Range(-1, 2), Random.Range(-1, 2), 0f) / Legacy.PixelsPerUnit;
                t += Time.deltaTime;
                yield return null;
            }
            if (bodyRenderer != null) bodyRenderer.transform.localPosition = bodyHome;
        }

        private IEnumerator PhaseShift()
        {
            Invulnerable = true; // the boss bar shows the shield
            yield return Telegraph(1.0f, true);
            int n = ShiftRing[D];
            float a = Random.Range(0f, 360f);
            Ring(n, 0.6f, a);
            yield return new WaitForSeconds(0.35f);
            Ring(n, 0.6f, a + 180f / n); // second ring fills the gaps of the first
            yield return new WaitForSeconds(0.35f);
            Invulnerable = false;

            ShotIntervalScale = Phase >= 2 ? 0.75f : 0.9f;
            SpeedOverridePoints = Phase >= 2 ? Definition.speedPoints * 1.35f : 0f;
            Release();
            yield return new WaitForSeconds(1f);
        }

        private IEnumerator FanAttack()
        {
            yield return Telegraph(0.35f, false);
            for (int v = 0; v < FanVolleys[D]; v++)
            {
                Spread(AimDirection(), FanBullets[D], 14f, 0.85f);
                yield return new WaitForSeconds(0.3f);
            }
        }

        private IEnumerator LungeAttack(int count)
        {
            float margin = Legacy.Units(EdgeMarginPoints) * 2f;
            for (int i = 0; i < count; i++)
            {
                yield return Telegraph(LungeWarn[D], true);
                if (!PlayerAlive) yield break;
                Vector2 player = PlayerController.Current.transform.position;
                Vector2 dir = AimDirection();
                // Through where the player stood, not onto them: stepping aside during the shake is the answer.
                Vector2 target = player + dir * Legacy.Units(LungeOvershootPoints);
                lungeTarget = Arena.Current != null ? Arena.Current.Clamp(target, margin) : target;
                lungeArrived = false;
                mode = Mode.Lunge;
                float t = 0f;
                while (!lungeArrived && t < 0.8f) { t += Time.deltaTime; yield return null; }
                mode = Mode.Hold;
                Ring(LungeRing[D], 0.7f, Random.Range(0f, 360f));
                yield return new WaitForSeconds(0.35f);
            }
        }

        private IEnumerator SpiralAttack()
        {
            yield return Telegraph(0.4f, false);
            int arms = SpiralArms[D];
            spiralSign = -spiralSign; // alternate the turn each time
            float turn = (Phase >= 2 ? 200f : 150f) * spiralSign;
            float angle = Random.Range(0f, 360f), t = 0f, next = 0f;
            while (t < SpiralSeconds[D])
            {
                while (t >= next)
                {
                    for (int k = 0; k < arms; k++) Shoot(Deg(angle + k * 360f / arms), 0.55f);
                    next += SpiralEvery[D];
                }
                angle += turn * Time.deltaTime;
                t += Time.deltaTime;
                yield return null;
            }
        }

        private IEnumerator BroodAttack()
        {
            yield return Telegraph(0.4f, false);
            minions.RemoveAll(m => m == null || m.Current == State.Dead);
            int n = Mathf.Min(BroodCount[D], BroodMax[D] - minions.Count);
            float a0 = Random.Range(0f, 360f), margin = Legacy.Units(EdgeMarginPoints);
            for (int i = 0; i < n; i++)
            {
                Vector2 pos = rb.position + Deg(a0 + i * 360f / n) * Legacy.Units(60f);
                if (Arena.Current != null) pos = Arena.Current.Clamp(pos, margin);
                GameObject go = EnemyCatalog.SpawnById(MinionId, pos);
                Enemy e = go != null ? go.GetComponent<Enemy>() : null;
                if (e != null) minions.Add(e);
            }
            yield return new WaitForSeconds(0.2f);
        }

        private IEnumerator BlinkAttack()
        {
            mode = Mode.Gone;
            PauseBaseShooting = true;
            Vanish(); // harmless and unhittable from the first frame of the fade
            for (float t = 0f; t < 0.25f; t += Time.deltaTime)
            {
                SetAlpha(1f - t / 0.25f);
                yield return null;
            }
            SetAlpha(0f);
            yield return new WaitForSeconds(0.25f);

            evading = false;
            Reappear(PointNearPlayer(BlinkMinPoints, BlinkMaxPoints), 0.5f); // fades in like a spawn: fair warning
            while (Current != State.Alive) yield return null;
            mode = Mode.Hold;
            if (Random.value < 0.5f) Ring(ShiftRing[D], 0.65f, Random.Range(0f, 360f));
            else Spread(AimDirection(), FanBullets[D] + 2, 12f, 0.85f);
            yield return new WaitForSeconds(0.3f);
        }

        // ------------------------------------------------------------------ bullets

        private Vector2 AimDirection()
        {
            PlayerController p = PlayerController.Current;
            if (p == null) return Vector2.down;
            Vector2 d = (Vector2)p.transform.position - rb.position;
            return d.sqrMagnitude > 0.0001f ? d.normalized : Vector2.down;
        }

        private static Vector2 Deg(float degrees) =>
            new Vector2(Mathf.Cos(degrees * Mathf.Deg2Rad), Mathf.Sin(degrees * Mathf.Deg2Rad));

        private void Shoot(Vector2 dir, float speedScale)
        {
            EnemyDefinition d = Definition;
            if (d.bulletPrefab == null) return;
            EnemyBullet.Spawn(d.bulletPrefab, rb.position + dir * Legacy.Units(d.shotOffsetPoints), dir,
                Legacy.Units(d.shotSpeedPoints) * speedScale, this);
        }

        private void Spread(Vector2 aim, int count, float stepDegrees, float speedScale)
        {
            float a = Mathf.Atan2(aim.y, aim.x) * Mathf.Rad2Deg;
            for (int i = 0; i < count; i++) Shoot(Deg(a + (i - (count - 1) / 2f) * stepDegrees), speedScale);
        }

        private void Ring(int count, float speedScale, float startDegrees)
        {
            for (int i = 0; i < count; i++) Shoot(Deg(startDegrees + i * 360f / count), speedScale);
        }
    }
}
