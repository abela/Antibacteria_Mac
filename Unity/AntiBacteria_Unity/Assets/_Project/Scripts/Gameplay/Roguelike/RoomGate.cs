using AntiBacteria.Audio;
using AntiBacteria.Core;
using AntiBacteria.Gameplay.Effects;
using AntiBacteria.Gameplay.Player;
using AntiBacteria.Rendering;
using UnityEngine;

namespace AntiBacteria.Gameplay.Roguelike
{
    /// <summary>
    /// The way out of a cleared room: an accent doorway on the top wall with chevrons running up through it.
    /// Touching it takes the player to the next room. A blinking arrow at the screen edge points to it.
    /// </summary>
    public class RoomGate : MonoBehaviour
    {
        private SpriteRenderer frame, chevrons;
        private Sprite[] chevronFrames;
        private System.Action onEnter;
        private bool used;
        private float age;

        public static RoomGate Spawn(Material mat, System.Action onEnter)
        {
            Arena arena = Arena.Current;
            if (arena == null) return null;
            RoguelikeSettings s = RoguelikeSettings.Current;
            int w = Mathf.RoundToInt(s.gateWidthPixels);
            Rect b = arena.Bounds;
            var go = new GameObject("RoomGate") { layer = GameLayers.Get(GameLayers.Pickup) };
            // Sits on the inner edge of the top wall.
            go.transform.position = new Vector3(Mathf.Round(b.center.x * Legacy.PixelsPerUnit) / Legacy.PixelsPerUnit,
                                                b.yMax - 7f / Legacy.PixelsPerUnit, 0f);
            var gate = go.AddComponent<RoomGate>();
            gate.onEnter = onEnter;
            var rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            var box = go.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = new Vector2(w, 14f) / Legacy.PixelsPerUnit;

            gate.frame = Make(go.transform, "Frame", Doorway(w), mat, 8);
            gate.chevronFrames = new Sprite[4];
            for (int i = 0; i < gate.chevronFrames.Length; i++) gate.chevronFrames[i] = Chevrons(w - 6, i);
            gate.chevrons = Make(go.transform, "Chevrons", gate.chevronFrames[0], mat, 9);
            ScreenEdgePointer.Attach(go, mat).marginPixels = 30f; // below the HUD's meter and XP bar at the top
            Fx.Sparks(go.transform.position, Vector2.down, 70f, 24, 0.8f);
            Sound.Play(Sfx.BonusAppear);
            return gate;
        }

        private static SpriteRenderer Make(Transform parent, string name, Sprite s, Material mat, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = s;
            sr.sharedMaterial = mat;
            sr.sortingOrder = order;
            return sr;
        }

        /// <summary>Two accent posts and a lintel, w wide and 14 tall.</summary>
        private static Sprite Doorway(int w)
        {
            var rows = new string[14];
            for (int y = 0; y < 14; y++)
            {
                var r = new char[w];
                for (int x = 0; x < w; x++)
                {
                    bool post = x < 2 || x >= w - 2;
                    bool lintel = y < 2;
                    r[x] = post || lintel ? 'R' : '.';
                }
                rows[y] = new string(r);
            }
            return PixelShapes.FromRows("gate_door" + w, rows);
        }

        /// <summary>Upward chevrons, scrolled by 'step' pixels (4 frames make one loop).</summary>
        private static Sprite Chevrons(int w, int step)
        {
            const int h = 12;
            var rows = new string[h];
            for (int y = 0; y < h; y++)
            {
                var r = new char[w];
                for (int x = 0; x < w; x++)
                {
                    int dx = Mathf.Abs(x - w / 2);
                    int k = ((y - dx + step) % 4 + 4) % 4; // a chevron every 4 rows, pointing up, moving up as step grows
                    r[x] = k == 0 && dx <= 5 ? '#' : '.';
                }
                rows[y] = new string(r);
            }
            return PixelShapes.FromRows($"gate_chev{w}_{step}", rows);
        }

        private void Update()
        {
            age += Time.deltaTime;
            if (chevrons != null) chevrons.sprite = chevronFrames[Mathf.FloorToInt(age * 10f) % chevronFrames.Length];
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (used || other.gameObject.layer != GameLayers.Get(GameLayers.Player)) return;
            PlayerController p = other.GetComponentInParent<PlayerController>();
            if (p == null || !p.IsAlive) return;
            used = true;
            Sound.Play(Sfx.UiConfirm);
            onEnter?.Invoke();
        }
    }
}
