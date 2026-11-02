using AntiBacteria.Audio;
using System.Collections.Generic;
using AntiBacteria.Core;
using AntiBacteria.Gameplay.Effects;
using AntiBacteria.Gameplay.Player;
using AntiBacteria.Rendering;
using UnityEngine;

namespace AntiBacteria.Gameplay.Bonuses
{
    /// <summary>
    /// A bonus floating in the arena: a square frame with the type's icon. It wanders between random points,
    /// shows each type in turn (campaign) and gives whichever it shows when touched, blinks, then disappears.
    /// Port of UsableBonus (Docs/DesignReference/01, section 8), with every timer in seconds instead of frames.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class BonusPickup : MonoBehaviour
    {
        private const int SortingOrder = 7; // under enemies (10+), over vitamins (6)
        private const float BlinkPeriod = 0.1f;

        private static readonly List<BonusPickup> Live = new List<BonusPickup>();

        public BonusType Type { get; private set; }

        private Rigidbody2D rb;
        private SpriteRenderer frame, icon, burst, arrow;
        private readonly SpriteRenderer[] corners = new SpriteRenderer[4];
        private Vector2 target;
        private float age, cycleTimer;
        private bool taken, cycles = true;

        public static int LiveCount => Live.Count;
        /// <summary>Every bonus in the arena (the HUD labels them for new players).</summary>
        public static IReadOnlyList<BonusPickup> All => Live;
        /// <summary>False while it blinks off near the end of its life.</summary>
        public bool Visible => frame != null && frame.enabled;

        /// <param name="cycle">Campaign drops change type every few seconds; endless modes drop a fixed type.</param>
        public static BonusPickup Spawn(BonusType type, Vector2 position, bool cycle = true)
        {
            BonusSettings s = BonusSettings.Current;
            var go = new GameObject("Bonus") { layer = GameLayers.Get(GameLayers.Pickup) };
            go.transform.position = position;
            var rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 6.5f / Legacy.PixelsPerUnit; // the 13 px frame
            var p = go.AddComponent<BonusPickup>();
            p.rb = rb;
            p.cycles = cycle;
            p.frame = MakeRenderer(go.transform, "Frame", s.frame, s.material, SortingOrder);
            p.icon = MakeRenderer(go.transform, "Icon", null, s.material, SortingOrder + 1);
            p.SetType(type);
            p.PickTarget();
            p.BuildHighlight(s);
            return p;
        }

        private static SpriteRenderer MakeRenderer(Transform parent, string name, Sprite sprite, Material mat, int order)
        {
            var go = new GameObject(name);
            go.layer = parent.gameObject.layer;
            go.transform.SetParent(parent, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sharedMaterial = mat;
            sr.sortingOrder = order;
            return sr;
        }

        /// <summary>Corner brackets, the arrival burst and the off-screen arrow (BonusSettings, "Highlight").</summary>
        private void BuildHighlight(BonusSettings s)
        {
            if (s.brackets)
                for (int i = 0; i < 4; i++)
                {
                    corners[i] = MakeRenderer(transform, "Corner", PixelShapes.Bracket(3, true), s.material, SortingOrder + 2);
                    corners[i].transform.localRotation = Quaternion.Euler(0f, 0f, -90f * i); // TL, TR, BR, BL
                }
            if (s.spawnFlash)
            {
                burst = MakeRenderer(transform, "Burst", PixelShapes.SquareRing(7, 2), s.material, SortingOrder + 2);
                Fx.Sparks(transform.position, Vector2.zero, 0f, 14, 0.6f);
                Sound.Play(Sfx.BonusAppear);
            }
            if (s.offscreenArrow)
            {
                var go = new GameObject("BonusArrow");
                go.transform.SetParent(transform, false);
                arrow = go.AddComponent<SpriteRenderer>();
                arrow.sharedMaterial = s.material;
                arrow.sortingOrder = 40; // over the action, under the HUD canvas
                arrow.enabled = false;
            }
        }

        private void UpdateHighlight(BonusSettings s, bool visible)
        {
            const float Px = 1f / Legacy.PixelsPerUnit;
            // Brackets breathe between 1 and 3 px outside the 13 px frame.
            int gap = 1 + Mathf.RoundToInt((Mathf.Sin(age * Mathf.PI * 2f / s.bracketPulseSeconds) * 0.5f + 0.5f) * 2f);
            float d = (7 + gap) * Px;
            Vector2[] at = { new Vector2(-d, d), new Vector2(d, d), new Vector2(d, -d), new Vector2(-d, -d) };
            for (int i = 0; i < 4; i++)
                if (corners[i] != null)
                {
                    corners[i].transform.localPosition = at[i];
                    corners[i].enabled = visible;
                }
            if (burst != null)
            {
                const float BurstSeconds = 0.45f;
                if (age >= BurstSeconds) { Destroy(burst.gameObject); burst = null; }
                else
                {
                    float k = age / BurstSeconds;
                    burst.sprite = PixelShapes.SquareRing(7 + Mathf.RoundToInt(k * 14f), 2);
                    burst.color = new Color(1f, 1f, 1f, 1f - k);
                }
            }
            if (arrow != null) UpdateArrow(s);
        }

        /// <summary>When the bonus is off screen, a blinking accent arrow at the screen edge points at it.</summary>
        private void UpdateArrow(BonusSettings s)
        {
            Camera cam = Camera.main;
            if (cam == null || !cam.orthographic) { arrow.enabled = false; return; }
            float halfH = cam.orthographicSize, halfW = halfH * cam.aspect;
            float m = s.arrowMarginPixels / Legacy.PixelsPerUnit;
            Vector2 c = cam.transform.position;
            Vector2 d = (Vector2)transform.position - c;
            bool onScreen = Mathf.Abs(d.x) < halfW - 0.2f && Mathf.Abs(d.y) < halfH - 0.2f;
            if (onScreen) { arrow.enabled = false; return; }
            // Where the line from the screen centre to the bonus meets the edge (inset by the margin).
            float sx = (halfW - m) / Mathf.Max(0.001f, Mathf.Abs(d.x)), sy = (halfH - m) / Mathf.Max(0.001f, Mathf.Abs(d.y));
            Vector2 p = c + d * Mathf.Min(sx, sy);
            arrow.transform.position = new Vector3(Mathf.Round(p.x * Legacy.PixelsPerUnit) / Legacy.PixelsPerUnit,
                                                   Mathf.Round(p.y * Legacy.PixelsPerUnit) / Legacy.PixelsPerUnit, 0f);
            // 8 directions: straight or diagonal sprite, turned in 90-degree steps (pixels stay on the grid).
            int oct = Mathf.RoundToInt(Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg / 45f);
            oct = ((oct % 8) + 8) % 8;
            arrow.sprite = PixelShapes.Arrow(oct % 2 == 1, true);
            arrow.transform.rotation = Quaternion.Euler(0f, 0f, (oct / 2) * 90f);
            arrow.enabled = Mathf.FloorToInt(age / 0.25f) % 2 == 0;
        }

        /// <summary>Remove every pickup (level over).</summary>
        public static void ClearAll()
        {
            for (int i = Live.Count - 1; i >= 0; i--)
                if (Live[i] != null) Destroy(Live[i].gameObject);
            Live.Clear();
        }

        private void OnEnable() => Live.Add(this);
        private void OnDisable() => Live.Remove(this);

        private void SetType(BonusType type)
        {
            Type = type;
            BonusInfo info = BonusSettings.Current.Info(type);
            if (icon != null) icon.sprite = info != null ? info.icon : null;
        }

        private void PickTarget()
        {
            float margin = Legacy.Units(BonusSettings.Current.edgeMarginPoints);
            target = Arena.Current != null ? Arena.Current.RandomPointInside(margin) : rb.position;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return; // paused
            BonusSettings s = BonusSettings.Current;
            age += dt;
            if (age >= s.lifetimeSeconds) { Destroy(gameObject); return; }

            if (cycles && s.cycleTypes)
            {
                cycleTimer += dt;
                if (cycleTimer >= s.cycleSeconds)
                {
                    cycleTimer -= s.cycleSeconds;
                    SetType((BonusType)(((int)Type + 1) % System.Enum.GetValues(typeof(BonusType)).Length));
                }
            }

            float leftSeconds = s.lifetimeSeconds - age;
            bool visible = leftSeconds > s.blinkLastSeconds || Mathf.FloorToInt(leftSeconds / BlinkPeriod) % 2 == 0;
            frame.enabled = icon.enabled = visible;
            UpdateHighlight(s, visible);
        }

        private void FixedUpdate()
        {
            float step = Legacy.Units(BonusSettings.Current.wanderSpeedPoints) * Time.fixedDeltaTime;
            Vector2 to = target - rb.position;
            if (to.magnitude <= Mathf.Max(step, 10f / Legacy.PixelsPerUnit)) { PickTarget(); return; }
            rb.MovePosition(rb.position + to.normalized * step);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (taken || other.gameObject.layer != GameLayers.Get(GameLayers.Player)) return;
            PlayerController player = other.GetComponentInParent<PlayerController>();
            if (player == null || !player.IsAlive) return;
            taken = true;
            player.Buffs.Apply(Type);
            if (Type != BonusType.Boom) Sound.Play(Sfx.BonusPickup); // Boom makes its own noise
            Fx.Sparks(transform.position, Vector2.zero, 0f, 10, 0.5f);
            Destroy(gameObject);
        }
    }
}
