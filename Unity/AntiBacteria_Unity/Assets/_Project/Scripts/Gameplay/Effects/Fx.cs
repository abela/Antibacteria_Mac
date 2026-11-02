using AntiBacteria.Audio;
using System.Collections.Generic;
using AntiBacteria.Core;
using AntiBacteria.Gameplay.Enemies;
using UnityEngine;

namespace AntiBacteria.Gameplay.Effects
{
    /// <summary>
    /// Pooled pixel sparks and one-shot flash animations (muzzle, hits, walls), plus the reaction to kills
    /// (spark burst + screen shake). Created on first use in each gameplay scene. Numbers come from ShotFeel.
    /// Sparks blink out instead of fading so every pixel stays one of the three palette colours.
    /// </summary>
    public class Fx : MonoBehaviour
    {
        private const int MaxSparks = 320;
        private const float BlinkPeriod = 0.04f;
        private const int SortingOrder = 21; // above the kill pop (20)

        private struct Spark
        {
            public SpriteRenderer sr;
            public Vector2 pos, vel;
            public float life, maxLife;
        }

        private class FlashAnim
        {
            public SpriteRenderer sr;
            public Sprite[] frames;
            public float fps, time;
        }

        private static Fx instance;

        private readonly List<Spark> sparks = new List<Spark>();
        private readonly Stack<SpriteRenderer> freeSparks = new Stack<SpriteRenderer>();
        private readonly List<FlashAnim> flashes = new List<FlashAnim>();
        private readonly Stack<FlashAnim> freeFlashes = new Stack<FlashAnim>();

        private static Fx Instance
        {
            get
            {
                if (instance == null && Application.isPlaying)
                    instance = new GameObject("Fx").AddComponent<Fx>();
                return instance;
            }
        }

        private static ShotFeel Feel => ShotFeel.Current;

        private void OnEnable() => Enemy.Killed += OnEnemyKilled;
        private void OnDisable() => Enemy.Killed -= OnEnemyKilled;

        private void OnDestroy()
        {
            if (instance == this) instance = null;
        }

        // ------------------------------------------------------------------ public API

        /// <summary>
        /// Throw <paramref name="count"/> sparks from a point. dir = Vector2.zero for all directions, otherwise
        /// they fly within +- spreadDegrees of dir. accentShare = how many of them are red (0..1).
        /// </summary>
        public static void Sparks(Vector2 at, Vector2 dir, float spreadDegrees, int count, float accentShare, float bigShare = 0f,
            float speedScale = 1f)
        {
            if (count <= 0) return;
            Fx fx = Instance;
            if (fx != null) fx.SpawnSparks(at, dir, spreadDegrees, count, accentShare, bigShare, speedScale);
        }

        /// <summary>Play frames once at a point, rotated (degrees). follow: move along with this transform.</summary>
        public static void Flash(Sprite[] frames, Vector2 at, float angleDegrees, float fps, Transform follow = null)
        {
            if (frames == null || frames.Length == 0) return;
            Fx fx = Instance;
            if (fx != null) fx.SpawnFlash(frames, at, angleDegrees, fps, follow);
        }

        // ------------------------------------------------------------------ sparks

        private void SpawnSparks(Vector2 at, Vector2 dir, float spreadDegrees, int count, float accentShare, float bigShare,
            float speedScale = 1f)
        {
            ShotFeel f = Feel;
            float baseAngle = dir.sqrMagnitude > 0.0001f ? Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg : 0f;
            bool radial = dir.sqrMagnitude <= 0.0001f;
            for (int i = 0; i < count && sparks.Count < MaxSparks; i++)
            {
                float a = radial ? Random.Range(0f, 360f) : baseAngle + Random.Range(-spreadDegrees, spreadDegrees);
                float speed = Random.Range(f.sparkSpeedMin, f.sparkSpeedMax) * speedScale / Legacy.PixelsPerUnit;
                Sprite sprite = Random.value < bigShare ? f.sparkBig : Random.value < accentShare ? f.sparkAccent : f.sparkForeground;
                if (sprite == null) continue;
                SpriteRenderer sr = TakeSparkRenderer();
                sr.sprite = sprite;
                sr.enabled = true;
                float life = Random.Range(f.sparkLifeMin, f.sparkLifeMax);
                var s = new Spark
                {
                    sr = sr,
                    pos = at,
                    vel = new Vector2(Mathf.Cos(a * Mathf.Deg2Rad), Mathf.Sin(a * Mathf.Deg2Rad)) * speed,
                    life = life,
                    maxLife = life,
                };
                sr.transform.position = at;
                sparks.Add(s);
            }
        }

        private SpriteRenderer TakeSparkRenderer()
        {
            if (freeSparks.Count > 0) return freeSparks.Pop();
            var go = new GameObject("Spark");
            go.transform.SetParent(transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sharedMaterial = Feel.material;
            sr.sortingOrder = SortingOrder;
            return sr;
        }

        // ------------------------------------------------------------------ flashes

        private void SpawnFlash(Sprite[] frames, Vector2 at, float angleDegrees, float fps, Transform follow)
        {
            FlashAnim a = freeFlashes.Count > 0 ? freeFlashes.Pop() : NewFlash();
            a.frames = frames;
            a.fps = fps;
            a.time = 0f;
            Transform t = a.sr.transform;
            t.SetParent(follow != null ? follow : transform, true);
            t.SetPositionAndRotation(at, Quaternion.Euler(0f, 0f, angleDegrees));
            a.sr.sprite = frames[0];
            a.sr.enabled = true;
            flashes.Add(a);
        }

        private FlashAnim NewFlash()
        {
            var go = new GameObject("Flash");
            go.transform.SetParent(transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sharedMaterial = Feel.material;
            sr.sortingOrder = SortingOrder;
            return new FlashAnim { sr = sr };
        }

        // ------------------------------------------------------------------ update

        private void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return; // paused
            ShotFeel f = Feel;
            float drag = Mathf.Exp(-f.sparkDrag * dt);

            for (int i = sparks.Count - 1; i >= 0; i--)
            {
                Spark s = sparks[i];
                s.life -= dt;
                if (s.life <= 0f)
                {
                    s.sr.enabled = false;
                    freeSparks.Push(s.sr);
                    sparks.RemoveAt(i);
                    continue;
                }
                s.pos += s.vel * dt;
                s.vel *= drag;
                s.sr.transform.position = s.pos;
                float left = s.life / s.maxLife;
                s.sr.enabled = left > f.sparkBlinkFrom || Mathf.FloorToInt(s.life / BlinkPeriod) % 2 == 0;
                sparks[i] = s;
            }

            for (int i = flashes.Count - 1; i >= 0; i--)
            {
                FlashAnim a = flashes[i];
                if (a.sr == null) { flashes.RemoveAt(i); continue; } // its follow target was destroyed
                a.time += dt;
                int frame = Mathf.FloorToInt(a.time * a.fps);
                if (frame >= a.frames.Length)
                {
                    a.sr.enabled = false;
                    a.sr.transform.SetParent(transform, false);
                    freeFlashes.Push(a);
                    flashes.RemoveAt(i);
                    continue;
                }
                a.sr.sprite = a.frames[frame];
            }
        }

        // ------------------------------------------------------------------ kills

        private void OnEnemyKilled(Enemy e)
        {
            if (e == null) return;
            ShotFeel f = Feel;
            Vector2 at = e.transform.position;
            bool boss = e.Definition != null && e.Definition.isBoss;
            Sound.Play(boss ? Sfx.BossKill : Sfx.EnemyKill);
            if (boss)
            {
                SpawnSparks(at, Vector2.zero, 0f, f.bossKillSparks, 0.5f, 0.25f);
                CameraFollow.Shake(f.bossKillShakePixels, f.bossKillShakeSeconds);
            }
            else
            {
                SpawnSparks(at, Vector2.zero, 0f, f.killSparks, 0.4f, 0.15f);
                CameraFollow.Shake(f.killShakePixels, f.killShakeSeconds);
            }
        }
    }
}
