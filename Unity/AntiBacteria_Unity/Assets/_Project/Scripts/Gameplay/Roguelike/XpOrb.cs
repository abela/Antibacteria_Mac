using System.Collections.Generic;
using AntiBacteria.Audio;
using AntiBacteria.Core;
using AntiBacteria.Gameplay.Effects;
using AntiBacteria.Gameplay.Player;
using AntiBacteria.Rendering;
using UnityEngine;

namespace AntiBacteria.Gameplay.Roguelike
{
    /// <summary>
    /// Roguelike XP: a white diamond with an accent core (7x7, bosses drop 9x9 ones), dropped by kills - not
    /// vitamins, which still go to the shop wallet, and a different shape so the two never get mixed up.
    /// Pops out of the kill with a spark burst, bobs and pulses (inverts colours and twinkles), flies to the player
    /// once close (Magnet perk widens that, the buddy's Fetch pulls too) leaving a fading trail, and every orb
    /// flies in when the room is cleared. Created at runtime, no prefab. Look: RoguelikeSettings "XP orb look".
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class XpOrb : MonoBehaviour
    {
        private const int SortingOrder = 6;
        private static readonly List<XpOrb> Live = new List<XpOrb>();
        private static Material material;
        private static float lastPickup = -10f;
        private static int streak;

        /// <summary>Raised on every pickup (the HUD flashes the XP bar).</summary>
        public static event System.Action PickedUp;

        private Rigidbody2D rb;
        private Transform visual;
        private SpriteRenderer sr;
        private Sprite normal, lit;
        private Vector2 spillFrom, spillTo;
        private float spillT = 1f, age, phase, speed, trailTimer;
        private bool homing, taken;

        public static void Spawn(Vector2 at, int count, Material mat, bool boss = false)
        {
            material = mat;
            RoguelikeSettings s = RoguelikeSettings.Current;
            int r = boss ? s.bossOrbRadius : s.orbRadius;
            for (int i = 0; i < count; i++)
            {
                Vector2 to = at + Random.insideUnitCircle * ((10f + r * 2f) / Legacy.PixelsPerUnit) * Mathf.Sqrt(count);
                if (Arena.Current != null) to = Arena.Current.Clamp(to, 0.5f);
                Create(at, to, r);
            }
            if (s.orbBurstSparks > 0) Fx.Sparks(at, Vector2.zero, 0f, s.orbBurstSparks * (boss ? 3 : 1), 1f, 0.2f);
        }

        /// <summary>Room cleared: every orb flies to the player.</summary>
        public static void CallAll()
        {
            foreach (XpOrb o in Live) if (o != null) o.homing = true;
        }

        /// <summary>This orb flies to the player now (the buddy's Fetch).</summary>
        public void Call() => homing = true;

        public static void ClearAll()
        {
            for (int i = Live.Count - 1; i >= 0; i--) if (Live[i] != null) Destroy(Live[i].gameObject);
            Live.Clear();
        }

        /// <summary>Normal frame: white outline, accent core. Lit frame: inverted, with a twinkle on the diagonals.</summary>
        private static Sprite Frame(int r, bool litUp)
        {
            int n = 2 * r + 5, c = n / 2;
            var rows = new string[n];
            for (int y = 0; y < n; y++)
            {
                var line = new char[n];
                for (int x = 0; x < n; x++)
                {
                    int d = Mathf.Abs(x - c) + Mathf.Abs(y - c);
                    char ch = '.';
                    if (d == r) ch = litUp ? 'R' : '#';
                    else if (d < r) ch = litUp ? '#' : 'R';
                    else if (litUp && Mathf.Abs(x - c) == r && Mathf.Abs(y - c) == r) ch = '#'; // twinkle
                    line[x] = ch;
                }
                rows[y] = new string(line);
            }
            return PixelShapes.FromRows("xp_" + r + "_" + litUp, rows);
        }

        private static void Create(Vector2 from, Vector2 to, int r)
        {
            var go = new GameObject("XpOrb") { layer = GameLayers.Get(GameLayers.Pickup) };
            go.transform.position = from;
            var rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = (r + 2f) / Legacy.PixelsPerUnit;
            var o = go.AddComponent<XpOrb>();
            o.rb = rb;
            o.spillFrom = from;
            o.spillTo = to;
            o.spillT = 0f;
            o.phase = Random.value * 10f;
            o.normal = Frame(r, false);
            o.lit = Frame(r, true);
            var v = new GameObject("Visual");
            v.transform.SetParent(go.transform, false);
            o.sr = v.AddComponent<SpriteRenderer>();
            o.sr.sprite = o.normal;
            o.sr.sharedMaterial = material;
            o.sr.sortingOrder = SortingOrder;
            o.visual = v.transform;
        }

        private void OnEnable() => Live.Add(this);
        private void OnDisable() => Live.Remove(this);

        private void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f || visual == null) return;
            age += dt;
            RoguelikeSettings s = RoguelikeSettings.Current;
            // 1 px bob, out of step with its neighbours; a short inverted flash every pulse.
            visual.localPosition = new Vector3(0f, Mathf.Round(Mathf.Sin((age + phase) * 6f)) / Legacy.PixelsPerUnit, 0f);
            float pulse = Mathf.Max(0.2f, s.orbPulseSeconds);
            bool flash = homing ? Mathf.FloorToInt(age * 12f) % 2 == 0 : (age + phase) % pulse < 0.12f;
            sr.sprite = flash ? lit : normal;

            if (homing && s.orbTrail)
            {
                trailTimer -= dt;
                if (trailTimer <= 0f) { trailTimer = 0.03f; XpGhost.Drop(visual.position, normal, material, SortingOrder - 1); }
            }
        }

        private void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            if (spillT < 1f)
            {
                spillT = Mathf.Min(1f, spillT + dt / 0.25f);
                float e = 1f - (1f - spillT) * (1f - spillT);
                rb.MovePosition(Vector2.Lerp(spillFrom, spillTo, e));
                return;
            }
            PlayerController p = PlayerController.Current;
            if (p == null || !p.IsAlive) return;
            RoguelikeSettings s = RoguelikeSettings.Current;
            Vector2 d = (Vector2)p.transform.position - rb.position;
            if (!homing && d.magnitude * Legacy.PixelsPerUnit <= s.orbMagnetPixels * RunModifiers.MagnetMultiplier) homing = true;
            if (!homing) return;
            speed = Mathf.MoveTowards(speed, s.orbSpeedPixels, s.orbSpeedPixels * 3f * dt); // accelerates in
            rb.MovePosition(rb.position + d.normalized * Mathf.Min(d.magnitude, speed / Legacy.PixelsPerUnit * dt));
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (taken || other.gameObject.layer != GameLayers.Get(GameLayers.Player)) return;
            taken = true;
            RoguelikeRun.AddXp(1f);
            streak = Time.time - lastPickup < 0.4f ? Mathf.Min(streak + 1, 14) : 0;
            lastPickup = Time.time;
            Sound.Play(Sfx.Vitamin, 0.7f, 1.25f + streak * 0.05f);
            Fx.Sparks(transform.position, Vector2.zero, 0f, 3, 1f);
            PickedUp?.Invoke();
            Destroy(gameObject);
        }
    }

    /// <summary>A fading copy of an XP orb left behind as it flies (trail).</summary>
    public class XpGhost : MonoBehaviour
    {
        private const float Life = 0.15f;
        private SpriteRenderer sr;
        private float left = Life;

        public static void Drop(Vector3 at, Sprite sprite, Material mat, int order)
        {
            var go = new GameObject("XpGhost");
            go.transform.position = at;
            var g = go.AddComponent<XpGhost>();
            g.sr = go.AddComponent<SpriteRenderer>();
            g.sr.sprite = sprite;
            g.sr.sharedMaterial = mat;
            g.sr.sortingOrder = order;
            g.sr.color = new Color(1f, 1f, 1f, 0.5f);
        }

        private void Update()
        {
            left -= Time.deltaTime;
            if (left <= 0f) { Destroy(gameObject); return; }
            sr.color = new Color(1f, 1f, 1f, 0.5f * left / Life);
        }
    }
}
