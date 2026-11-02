using System.Collections.Generic;
using AntiBacteria.Core;
using AntiBacteria.Gameplay.Enemies;
using AntiBacteria.Gameplay.Player;
using AntiBacteria.Rendering;
using UnityEngine;

namespace AntiBacteria.Gameplay.Ambience
{
    /// <summary>
    /// Makes the field feel alive: specks drifting on a slow current (two depth layers), faint microbes swimming
    /// behind the action, pulse rings, and cilia waving along the walls. Everything gets out of the player's way
    /// and is blown outwards by kills. Added by Arena; tuned in AmbienceSettings.
    /// Works in game pixels relative to the arena centre and snaps every sprite to the pixel grid.
    /// </summary>
    public class ArenaAmbience : MonoBehaviour
    {
        // Sorting: floor -100 < far specks < microbes < pulses < near specks < cilia < everything in play (0+).
        private const int FarOrder = -95, MicrobeOrder = -90, PulseOrder = -88, NearOrder = -85, CiliaOrder = -80;
        private const float Px = 1f / Legacy.PixelsPerUnit;

        private class Speck
        {
            public SpriteRenderer sr;
            public Vector2 pos, push;
            public float parallax, alpha, blink, seed;
        }

        private class Microbe
        {
            public SpriteRenderer sr;
            public Sprite[] frames;
            public float fps, speed, seed, frameClock;
            public Vector2 pos, target, push;
            public bool turns; // rods face where they swim (90-degree steps, flipped left / right)
        }

        private class Hair
        {
            public SpriteRenderer sr;
            public float along; // distance along the walls, for the wave
            public int length = -1;
        }

        private readonly List<Speck> specks = new List<Speck>();
        private readonly List<Microbe> microbes = new List<Microbe>();
        private readonly List<Hair> hairs = new List<Hair>();
        private SpriteRenderer pulse;
        private Sprite[] pulseFrames;
        private float pulseAge = -1f, pulseTimer = 2f;

        private Arena arena;
        private AmbienceSettings s;
        private Material mat;
        private Vector2 half; // arena half size in pixels
        private Transform root;
        private bool rebuild;
        private SpriteRenderer floor, floorDetails;

        private void Start()
        {
            arena = GetComponent<Arena>();
            s = AmbienceSettings.Current;
            mat = s.material;
            if (arena == null || mat == null) { enabled = false; return; }
            half = arena.Bounds.size * Legacy.PixelsPerUnit * 0.5f;
            SplitFloor();
            Build();
            Enemy.Killed += OnKilled;
            AmbienceSettings.Changed += OnSettingsChanged;
        }

        private void OnSettingsChanged() => rebuild = true; // applied in Update: OnValidate can't create objects

        /// <summary>(Re)creates everything from the current settings.</summary>
        private void Build()
        {
            if (root != null) Destroy(root.gameObject);
            specks.Clear();
            microbes.Clear();
            hairs.Clear();
            pulseAge = -1f;
            mat = s.material;
            root = new GameObject("Ambience").transform;
            root.SetParent(transform, false);
            BuildSpecks(root);
            BuildMicrobes(root);
            pulseFrames = new Sprite[Mathf.Max(2, s.pulseMaxRadius / 3)];
            for (int i = 0; i < pulseFrames.Length; i++) pulseFrames[i] = PixelShapes.Ring(3 + i * 3, 3);
            pulse = MakeRenderer(root, "Pulse", pulseFrames[0], PulseOrder, 0f);
            pulse.enabled = false;
            if (s.cilia) BuildCilia(root);
            if (floorDetails != null) floorDetails.color = new Color(1f, 1f, 1f, s.floorDetailAlpha);
        }

        /// <summary>
        /// The floor image holds both the walls and the dots / dotted circles inside the field. Split it once:
        /// every solid pixel connected to the image border (8-way, to follow the dithered edge) is wall and stays
        /// solid; the rest goes to a second sprite whose opacity is floorDetailAlpha.
        /// </summary>
        private void SplitFloor()
        {
            foreach (SpriteRenderer sr in GetComponentsInChildren<SpriteRenderer>(true))
                if (sr.name == "Floor") floor = sr;
            if (floor == null || floor.sprite == null || !floor.sprite.texture.isReadable) return;
            Sprite src = floor.sprite;
            Rect r = src.rect;
            int w = (int)r.width, h = (int)r.height;
            Color32[] px = src.texture.GetPixels32();
            int texW = src.texture.width, ox = (int)r.x, oy = (int)r.y;
            bool Solid(int x, int y) => px[(oy + y) * texW + ox + x].a > 127;
            var wall = new bool[w, h];
            var stack = new Stack<Vector2Int>();
            for (int x = 0; x < w; x++) { stack.Push(new Vector2Int(x, 0)); stack.Push(new Vector2Int(x, h - 1)); }
            for (int y = 0; y < h; y++) { stack.Push(new Vector2Int(0, y)); stack.Push(new Vector2Int(w - 1, y)); }
            while (stack.Count > 0)
            {
                Vector2Int p = stack.Pop();
                if (p.x < 0 || p.y < 0 || p.x >= w || p.y >= h || wall[p.x, p.y] || !Solid(p.x, p.y)) continue;
                wall[p.x, p.y] = true;
                for (int dx = -1; dx <= 1; dx++)
                    for (int dy = -1; dy <= 1; dy++)
                        if (dx != 0 || dy != 0) stack.Push(new Vector2Int(p.x + dx, p.y + dy));
            }
            var walls = new Color32[w * h];
            var details = new Color32[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    Color32 c = px[(oy + y) * texW + ox + x];
                    if (wall[x, y]) walls[y * w + x] = c;
                    else if (c.a > 127) details[y * w + x] = c;
                }
            Vector2 pivot = new Vector2(src.pivot.x / w, src.pivot.y / h);
            floor.sprite = MakeSprite(walls, w, h, pivot, src.pixelsPerUnit, "arena_walls");
            var go = new GameObject("FloorDetails");
            go.transform.SetParent(floor.transform, false);
            floorDetails = go.AddComponent<SpriteRenderer>();
            floorDetails.sprite = MakeSprite(details, w, h, pivot, src.pixelsPerUnit, "arena_floor_details");
            floorDetails.sharedMaterial = floor.sharedMaterial;
            floorDetails.sortingLayerID = floor.sortingLayerID;
            floorDetails.sortingOrder = floor.sortingOrder + 1;
        }

        private static Sprite MakeSprite(Color32[] pixels, int w, int h, Vector2 pivot, float ppu, string name)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, name = name };
            tex.SetPixels32(pixels);
            tex.Apply(false, false); // stays readable: the cilia scan the walls
            var sprite = Sprite.Create(tex, new Rect(0, 0, w, h), pivot, ppu, 0, SpriteMeshType.FullRect);
            sprite.name = name;
            return sprite;
        }

        private void OnDestroy()
        {
            Enemy.Killed -= OnKilled;
            AmbienceSettings.Changed -= OnSettingsChanged;
        }

        private SpriteRenderer MakeRenderer(Transform parent, string name, Sprite sprite, int order, float alpha)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sharedMaterial = mat;
            sr.sortingOrder = order;
            sr.color = new Color(1f, 1f, 1f, alpha);
            return sr;
        }

        private Vector2 RandomInside(float margin) =>
            new Vector2(Random.Range(-half.x + margin, half.x - margin), Random.Range(-half.y + margin, half.y - margin));

        // ------------------------------------------------------------------ build

        private void BuildSpecks(Transform root)
        {
            for (int i = 0; i < s.specks; i++)
            {
                bool far = Random.value < s.farShare;
                bool big = !far && Random.value < 0.12f;
                bool accent = Random.value < 0.04f;
                var sp = new Speck
                {
                    parallax = far ? s.farParallax : 0f,
                    alpha = far ? s.farAlpha : s.nearAlpha,
                    pos = RandomInside(2f) * (far ? 1f + s.farParallax : 1f),
                    seed = Random.value * 100f,
                };
                sp.sr = MakeRenderer(root, "Speck", PixelShapes.Dot(big ? 2 : 1, accent), far ? FarOrder : NearOrder, sp.alpha);
                specks.Add(sp);
            }
        }

        private void BuildMicrobes(Transform root)
        {
            for (int i = 0; i < s.microbes; i++)
            {
                var m = new Microbe { seed = Random.value * 100f, speed = Random.Range(s.microbeSpeedMin, s.microbeSpeedMax) };
                switch (i % 3)
                {
                    case 0: m.frames = PixelShapes.Blob(Random.Range(4, 8), Random.Range(2, 4), 1.2f, 6, i); m.fps = 4f; break;
                    case 1: m.frames = PixelShapes.Rod(Random.Range(7, 11), Random.Range(5, 8), 4); m.fps = 10f; m.turns = true; m.speed *= 1.6f; break;
                    default:
                        int r = Random.Range(5, 10);
                        m.frames = new[] { PixelShapes.Ring(r), PixelShapes.Ring(r + 1), PixelShapes.Ring(r), PixelShapes.Ring(r - 1) };
                        m.fps = 1.5f; m.speed *= 0.6f; // ghost cells breathe slowly
                        break;
                }
                m.pos = RandomInside(12f);
                m.target = RandomInside(12f);
                m.frameClock = Random.value * m.frames.Length;
                m.sr = MakeRenderer(root, "Microbe", m.frames[0], MicrobeOrder, s.microbeAlpha);
                microbes.Add(m);
            }
        }

        /// <summary>Hairs along the inner edge of the walls, found by reading the floor sprite's pixels.</summary>
        private void BuildCilia(Transform root)
        {
            if (floor == null || floor.sprite == null || !floor.sprite.texture.isReadable) return;
            Texture2D tex = floor.sprite.texture;
            Rect r = floor.sprite.rect;
            int w = (int)r.width, h = (int)r.height, ox = (int)r.x, oy = (int)r.y;
            Vector2 centre = new Vector2(w * 0.5f, h * 0.5f); // floor is centred on the arena
            bool Solid(int x, int y) => x >= 0 && y >= 0 && x < w && y < h && tex.GetPixel(ox + x, oy + y).a > 0.5f;
            const int MaxDepth = 28, CornerSkip = 10;
            float along = 0f;
            void Add(int x, int y, float angle)
            {
                var hair = new Hair { along = along };
                hair.sr = MakeRenderer(root, "Cilium", PixelShapes.Hair(1), CiliaOrder, 1f);
                hair.sr.transform.localPosition = new Vector3((x - centre.x) * Px, (y - centre.y) * Px, 0f);
                hair.sr.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
                hairs.Add(hair);
            }
            // Walk each wall from outside in. The wall's edge is dithered, so the inner edge is the first pixel
            // followed by two more clear ones. Angles point the hair into the field.
            int Depth(System.Func<int, bool> solidAt)
            {
                for (int d = 1; d < MaxDepth; d++) if (!solidAt(d) && !solidAt(d + 1) && !solidAt(d + 2)) return d;
                return -1;
            }
            int k;
            for (int x = CornerSkip; x < w - CornerSkip; x += s.ciliaSpacing, along += s.ciliaSpacing)
                if ((k = Depth(d => Solid(x, h - 1 - d))) > 0) Add(x + 1, h - k, 180f);
            for (int y = h - CornerSkip; y > CornerSkip; y -= s.ciliaSpacing, along += s.ciliaSpacing)
                if ((k = Depth(d => Solid(w - 1 - d, y))) > 0) Add(w - k, y, 90f);
            for (int x = w - CornerSkip; x > CornerSkip; x -= s.ciliaSpacing, along += s.ciliaSpacing)
                if ((k = Depth(d => Solid(x, d))) > 0) Add(x, k, 0f);
            for (int y = CornerSkip; y < h - CornerSkip; y += s.ciliaSpacing, along += s.ciliaSpacing)
                if ((k = Depth(d => Solid(d, y))) > 0) Add(k, y + 1, -90f);
        }

        // ------------------------------------------------------------------ update

        private void OnKilled(Enemy e)
        {
            if (e == null) return;
            Vector2 at = ((Vector2)e.transform.position - (Vector2)transform.position) * Legacy.PixelsPerUnit;
            Push(at, s.killPushRadius, s.killPushSpeed);
        }

        private void Push(Vector2 at, float radius, float speed)
        {
            foreach (Speck sp in specks) sp.push += Away(sp.pos, at, radius, speed);
            foreach (Microbe m in microbes) m.push += Away(m.pos, at, radius, speed * 0.5f);
        }

        private static Vector2 Away(Vector2 p, Vector2 from, float radius, float speed)
        {
            Vector2 d = p - from;
            float dist = d.magnitude;
            if (dist >= radius || dist < 0.01f) return Vector2.zero;
            return d / dist * speed * (1f - dist / radius);
        }

        private void Update()
        {
            if (rebuild) { rebuild = false; Build(); }
            float dt = Time.deltaTime;
            if (dt <= 0f) return; // paused: the field freezes with the game
            float t = Time.time;
            Vector2 arenaPos = transform.position;
            Camera cam = Camera.main;
            Vector2 camPx = cam != null ? ((Vector2)cam.transform.position - arenaPos) * Legacy.PixelsPerUnit : Vector2.zero;
            PlayerController player = PlayerController.Current;
            bool hasPlayer = player != null && player.IsAlive;
            Vector2 playerPx = hasPlayer ? ((Vector2)player.transform.position - arenaPos) * Legacy.PixelsPerUnit : Vector2.zero;
            float decay = Mathf.Exp(-s.damping * dt);

            foreach (Speck sp in specks)
            {
                // A slow current from a few sines: smooth, never repeats exactly, costs nothing.
                Vector2 flow = new Vector2(Mathf.Sin(sp.pos.y * 0.045f + t * 0.23f + sp.seed),
                                           Mathf.Cos(sp.pos.x * 0.038f - t * 0.19f + sp.seed * 0.5f)) * s.flowSpeed;
                Vector2 view = sp.pos + camPx * sp.parallax; // where it is drawn
                if (hasPlayer && sp.parallax == 0f) sp.push += Away(view, playerPx, s.playerRepelRadius, s.playerRepelSpeed) * dt * 8f;
                sp.pos += (flow + sp.push) * dt;
                sp.push *= decay;
                Wrap(ref sp.pos, half * (1f + sp.parallax));
                view = sp.pos + camPx * sp.parallax;
                bool inside = Mathf.Abs(view.x) < half.x - 1f && Mathf.Abs(view.y) < half.y - 1f;
                if (sp.blink > 0f) sp.blink -= dt;
                else if (Random.value < s.twinkleRate * dt) sp.blink = Random.Range(0.08f, 0.3f);
                sp.sr.enabled = inside && sp.blink <= 0f;
                sp.sr.transform.localPosition = Snap(view);
            }

            foreach (Microbe m in microbes)
            {
                Vector2 to = m.target - m.pos;
                if (to.magnitude < 4f) m.target = RandomInside(12f);
                Vector2 wiggle = new Vector2(-to.y, to.x).normalized * Mathf.Sin(t * 2.1f + m.seed) * 0.5f;
                Vector2 vel = (to.normalized + wiggle) * m.speed;
                if (hasPlayer) m.push += Away(m.pos, playerPx, s.playerRepelRadius * 1.6f, s.playerRepelSpeed * 0.5f) * dt * 6f;
                m.pos += (vel + m.push) * dt;
                m.push *= decay;
                m.pos = new Vector2(Mathf.Clamp(m.pos.x, -half.x + 8f, half.x - 8f), Mathf.Clamp(m.pos.y, -half.y + 8f, half.y - 8f));
                m.frameClock += dt * m.fps;
                m.sr.sprite = m.frames[(int)m.frameClock % m.frames.Length];
                if (m.turns)
                {
                    // Face the way it swims in 90-degree steps (no off-grid rotation of pixel art).
                    bool horizontal = Mathf.Abs(vel.x) >= Mathf.Abs(vel.y);
                    m.sr.flipX = horizontal ? vel.x < 0f : false;
                    m.sr.transform.localRotation = Quaternion.Euler(0f, 0f, horizontal ? 0f : (vel.y > 0f ? 90f : -90f));
                }
                m.sr.transform.localPosition = Snap(m.pos);
            }

            UpdatePulse(dt);

            if (hairs.Count > 0)
            {
                float k = Mathf.PI * 2f / Mathf.Max(1f, s.ciliaWaveLength);
                foreach (Hair hair in hairs)
                {
                    float wave = Mathf.Sin(hair.along * k - t * s.ciliaWaveSpeed);
                    int len = Mathf.Clamp(Mathf.RoundToInt((wave * 0.5f + 0.5f) * s.ciliaMaxLength), 0, s.ciliaMaxLength);
                    if (len == hair.length) continue;
                    hair.length = len;
                    hair.sr.enabled = len > 0;
                    if (len > 0) hair.sr.sprite = PixelShapes.Hair(len);
                }
            }
        }

        private void UpdatePulse(float dt)
        {
            if (pulseAge < 0f)
            {
                pulseTimer -= dt;
                if (pulseTimer > 0f) return;
                pulseAge = 0f;
                pulse.transform.localPosition = Snap(RandomInside(s.pulseMaxRadius * 0.5f));
                pulse.enabled = true;
            }
            pulseAge += dt;
            float k = pulseAge / Mathf.Max(0.1f, s.pulseSeconds);
            if (k >= 1f)
            {
                pulse.enabled = false;
                pulseAge = -1f;
                pulseTimer = Random.Range(s.pulseEveryMin, s.pulseEveryMax);
                return;
            }
            float eased = 1f - (1f - k) * (1f - k); // fast out, slow at the end
            pulse.sprite = pulseFrames[Mathf.Min(pulseFrames.Length - 1, (int)(eased * pulseFrames.Length))];
            pulse.color = new Color(1f, 1f, 1f, s.pulseAlpha * (1f - k));
        }

        private static void Wrap(ref Vector2 p, Vector2 h)
        {
            if (p.x < -h.x) p.x += 2f * h.x; else if (p.x > h.x) p.x -= 2f * h.x;
            if (p.y < -h.y) p.y += 2f * h.y; else if (p.y > h.y) p.y -= 2f * h.y;
        }

        private static Vector3 Snap(Vector2 px) => new Vector3(Mathf.Round(px.x) * Px, Mathf.Round(px.y) * Px, 0f);
    }
}
