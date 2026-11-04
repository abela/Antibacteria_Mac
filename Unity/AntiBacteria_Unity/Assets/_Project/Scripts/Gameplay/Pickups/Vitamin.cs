using AntiBacteria.Audio;
using AntiBacteria.Core;
using AntiBacteria.Gameplay.Effects;
using AntiBacteria.Gameplay.Player;
using AntiBacteria.Rendering;
using UnityEngine;

namespace AntiBacteria.Gameplay.Pickups
{
    /// <summary>
    /// The currency drop (vitamin orb). Spills out of the kill, then bobs, glints and sends out a pulsing ring so it
    /// reads at a glance. Homes in once the player is within magnetDistancePoints and is collected on contact
    /// (Docs/DesignReference/01, section 7). Picking several up in a row raises the pitch of the pickup sound.
    /// When a level is won every vitamin left on the floor flies to the player (CollectAll).
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class Vitamin : MonoBehaviour
    {
        [SerializeField] private int value = 1;
        [Tooltip("The player pulls vitamins in from this far (2013: 500 pt).")]
        [SerializeField] private float magnetDistancePoints = 500f;
        [SerializeField] private float homingSpeedPoints = 1280f;

        [Header("Look")]
        [Tooltip("Orb frames: [0] normal, [1] glint flash.")]
        [SerializeField] private Sprite[] frames = new Sprite[0];
        [Tooltip("Pickup radius in pixels (the orb is 7 px).")]
        [SerializeField] private float pickupRadiusPixels = 5f;
        [SerializeField] private float glintEverySeconds = 1.1f;
        [SerializeField] private float ringEverySeconds = 1.3f;
        [Range(0f, 1f)] [SerializeField] private float ringAlpha = 0.7f;
        [Tooltip("Seconds to spill from the kill to where it lands.")]
        [SerializeField] private float spillSeconds = 0.28f;

        private const float BobPeriod = 0.9f, RingSeconds = 0.6f;
        private static float lastPickup = -10f;
        private static int streak;
        private static readonly System.Collections.Generic.List<Vitamin> Live = new System.Collections.Generic.List<Vitamin>();

        /// <summary>Level won: every vitamin, including any still spilling out of the last kill, flies to the player.
        /// Cleared when a level starts (CampaignLevelRunner).</summary>
        public static bool CollectAll { get; set; }

        /// <summary>Vitamins still on the floor.</summary>
        public static int LiveCount => Live.Count;

        /// <summary>Credits whatever is still flying (the result screen is about to open) and removes it.</summary>
        public static void CreditRest()
        {
            int total = 0;
            for (int i = Live.Count - 1; i >= 0; i--)
            {
                Vitamin v = Live[i];
                if (v == null || v.taken) continue;
                v.taken = true;
                total += v.value;
                Destroy(v.gameObject);
            }
            if (total > 0) { GameSession.AddVitamins(total); Sound.Play(Sfx.Vitamin); }
        }

        private Rigidbody2D rb;
        private SpriteRenderer body, ring;
        private Sprite[] ringFrames;
        private Transform visual;
        private bool homing, spilling, taken;
        private Vector2 spillFrom, spillTo;
        private float age, phase, spillT;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            phase = Random.value * 10f;
            body = GetComponentInChildren<SpriteRenderer>();
            if (body != null)
            {
                visual = body.transform;
                if (frames.Length > 0 && frames[0] != null) body.sprite = frames[0];
                ringFrames = new[] { PixelShapes.Ring(4, 2), PixelShapes.Ring(5, 2), PixelShapes.Ring(6, 2), PixelShapes.Ring(7, 2) };
                var go = new GameObject("Ring");
                go.transform.SetParent(transform, false);
                ring = go.AddComponent<SpriteRenderer>();
                ring.sharedMaterial = body.sharedMaterial;
                ring.sortingLayerID = body.sortingLayerID;
                ring.sortingOrder = body.sortingOrder - 1;
                ring.enabled = false;
            }
            if (GetComponent<CircleCollider2D>() is CircleCollider2D c) c.radius = pickupRadiusPixels / Legacy.PixelsPerUnit;
        }

        private void OnEnable() => Live.Add(this);
        private void OnDisable() => Live.Remove(this);

        /// <summary>Fly to the player from anywhere (roguelike room cleared).</summary>
        public void Attract() => homing = true;

        /// <summary>Fly out from where the enemy died to this vitamin's spot (RewardSpawner).</summary>
        public void SpillFrom(Vector2 from)
        {
            spillTo = rb.position;
            spillFrom = from;
            spilling = true;
            spillT = 0f;
            rb.position = from;
            transform.position = from;
        }

        private void Update()
        {
            if (visual == null || Time.deltaTime <= 0f) return;
            age += Time.deltaTime;
            float t = age + phase;
            // 1 px bob, glint flash, and a dotted ring that grows and fades now and then.
            visual.localPosition = new Vector3(0f, Mathf.Round(Mathf.Sin(t * Mathf.PI * 2f / BobPeriod)) / Legacy.PixelsPerUnit, 0f);
            if (frames.Length > 1) body.sprite = frames[(t % glintEverySeconds) < 0.12f ? 1 : 0];
            float r = t % ringEverySeconds;
            ring.enabled = r < RingSeconds;
            if (ring.enabled)
            {
                float k = r / RingSeconds;
                ring.sprite = ringFrames[Mathf.Min(ringFrames.Length - 1, (int)(k * ringFrames.Length))];
                ring.color = new Color(1f, 1f, 1f, ringAlpha * (1f - k));
            }
        }

        private void FixedUpdate()
        {
            if (spilling)
            {
                spillT += Time.fixedDeltaTime / Mathf.Max(0.01f, spillSeconds);
                float e = 1f - (1f - Mathf.Min(1f, spillT)) * (1f - Mathf.Min(1f, spillT)); // ease out
                rb.MovePosition(Vector2.Lerp(spillFrom, spillTo, e));
                if (spillT >= 1f) spilling = false;
                return;
            }
            PlayerController p = PlayerController.Current;
            if (p == null || !p.IsAlive) { rb.linearVelocity = Vector2.zero; return; }
            if (CollectAll) homing = true;
            Vector2 d = (Vector2)p.transform.position - rb.position;
            if (!homing && d.magnitude <= Legacy.Units(magnetDistancePoints) * RunModifiers.MagnetMultiplier) homing = true;
            rb.linearVelocity = homing ? d.normalized * Legacy.Units(homingSpeedPoints) : Vector2.zero;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (taken || other.gameObject.layer != GameLayers.Get(GameLayers.Player)) return;
            taken = true;
            GameSession.AddVitamins(value);
            // A quick run of pickups climbs in pitch (resets after half a second without one).
            streak = Time.time - lastPickup < 0.5f ? Mathf.Min(streak + 1, 12) : 0;
            lastPickup = Time.time;
            Sound.Play(Sfx.Vitamin, 1f, 1f + streak * 0.06f + Random.Range(0f, 0.04f));
            Fx.Sparks(transform.position, Vector2.zero, 0f, 5, 0.8f);
            Destroy(gameObject);
        }
    }
}
