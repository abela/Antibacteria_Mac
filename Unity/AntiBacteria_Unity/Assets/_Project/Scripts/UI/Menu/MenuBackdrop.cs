using System.Collections.Generic;
using AntiBacteria.Gameplay.Ambience;
using AntiBacteria.Rendering;
using UnityEngine;
using UnityEngine.UI;

namespace AntiBacteria.UI.Menu
{
    /// <summary>
    /// The living backdrop of the main menu (tuned in AmbienceSettings, "Main menu backdrop"):
    /// - dust drifting on a slow current and twinkling, spilling past the 320x180 layout at any aspect ratio;
    /// - faint ghost cells and wobbling blobs floating anywhere, behind the text;
    /// - bacteria in the left and right margins (outside the 80..240 px text column), drifting, bobbing, spinning
    ///   in 90-degree steps like the original BlueCorosia;
    /// - pulse rings in the margins, and now and then a worm crawling along the top or bottom edge.
    /// Runs on unscaled time (menus are open while the game is paused).
    /// </summary>
    public class MenuBackdrop : MonoBehaviour
    {
        private class Mote
        {
            public RectTransform rt;
            public Image img;
            public Vector2 pos, vel;
            public float spin, seed, blink, frameClock, fps;
            public Sprite[] frames;
            public bool margin; // bacteria: kept to the side columns
        }

        private readonly List<Mote> dust = new List<Mote>();
        private readonly List<Mote> ghosts = new List<Mote>();
        private readonly List<Mote> bacteria = new List<Mote>();
        private readonly List<RectTransform> worm = new List<RectTransform>();
        private readonly List<Vector2> wormTrail = new List<Vector2>();
        private Image pulse;
        private Sprite[] pulseFrames;
        private float pulseAge = -1f, pulseTimer = 1.5f, wormTimer, wormDir;
        private float wormX, wormY, wormPhase;
        private bool wormActive, showBacteria = true;
        private AmbienceSettings s;
        private System.Random rng;
        private UISkin skin;
        private int seed;
        private bool rebuild;

        private const float SegmentGap = 5f;

        /// <summary>Hide the creatures on busy screens (Options uses the full width); the dust stays.</summary>
        public void ShowBacteria(bool show)
        {
            showBacteria = show;
            foreach (Mote m in bacteria) m.rt.gameObject.SetActive(show);
            foreach (Mote m in ghosts) m.rt.gameObject.SetActive(show);
            if (pulse != null) pulse.gameObject.SetActive(show);
            foreach (RectTransform w in worm) w.gameObject.SetActive(show && wormActive);
        }

        public static MenuBackdrop Create(UISkin skin, Transform parent, int seed = 3)
        {
            RectTransform root = UIFactory.Stretch(parent, "Backdrop");
            var b = root.gameObject.AddComponent<MenuBackdrop>();
            b.s = AmbienceSettings.Current;
            b.skin = skin;
            b.seed = seed;
            b.Build();
            AmbienceSettings.Changed += b.OnSettingsChanged;
            return b;
        }

        private void OnSettingsChanged() => rebuild = true; // applied in Update: OnValidate can't create objects

        private void OnDestroy() => AmbienceSettings.Changed -= OnSettingsChanged;

        /// <summary>(Re)creates everything from the current settings.</summary>
        private void Build()
        {
            for (int i = transform.childCount - 1; i >= 0; i--) Destroy(transform.GetChild(i).gameObject);
            dust.Clear(); ghosts.Clear(); bacteria.Clear(); worm.Clear(); wormTrail.Clear();
            wormActive = false;
            pulseAge = -1f;
            rng = new System.Random(seed);
            Build(skin, (RectTransform)transform);
            ShowBacteria(showBacteria);
        }

        private float R(float a, float c) => a + (float)rng.NextDouble() * (c - a);

        private void Build(UISkin skin, RectTransform root)
        {
            // Dust first (furthest back). The screen is wider than the 320x180 layout, so it spills past it.
            for (int i = 0; i < s.menuDust; i++)
            {
                bool bright = rng.NextDouble() < 0.15;
                Image img = UIFactory.Image(root, "Dust", skin.pixel, 0f, 0f, 1f, 1f, PaletteRole.Foreground, !bright);
                dust.Add(new Mote { rt = img.rectTransform, img = img, pos = new Vector2(R(-80f, 400f), R(-60f, 240f)), seed = R(0f, 100f) });
            }

            // Ghost cells and blobs, faint, anywhere.
            for (int i = 0; i < s.menuGhosts; i++)
            {
                Sprite[] frames;
                float fps;
                if (i % 2 == 0)
                {
                    int r = rng.Next(5, 12);
                    frames = new[] { PixelShapes.Ring(r), PixelShapes.Ring(r + 1), PixelShapes.Ring(r), PixelShapes.Ring(r - 1) };
                    fps = 1.2f;
                }
                else
                {
                    frames = PixelShapes.Blob(rng.Next(4, 9), rng.Next(2, 4), 1.2f, 6, i);
                    fps = 3f;
                }
                Image img = UIFactory.Sprite(skin, root, "Ghost", frames[0], 0f, 0f);
                img.color = new Color(1f, 1f, 1f, s.menuGhostAlpha);
                ghosts.Add(new Mote
                {
                    rt = img.rectTransform, img = img, frames = frames, fps = fps, frameClock = R(0f, 4f),
                    pos = new Vector2(R(-40f, 360f), R(-20f, 200f)),
                    vel = new Vector2(R(-3f, 3f), R(-2.5f, 2.5f)), seed = R(0f, 100f),
                });
            }

            pulseFrames = new Sprite[10];
            for (int i = 0; i < pulseFrames.Length; i++) pulseFrames[i] = PixelShapes.Ring(3 + i * 3, 3);
            pulse = UIFactory.Sprite(skin, root, "Pulse", pulseFrames[0], 0f, 0f);
            pulse.enabled = false;

            // Bacteria in the margins, alternating sides, spread out vertically.
            int n = skin.backdropSprites.Length > 0 ? s.menuBacteria : 0;
            for (int i = 0; i < n; i++)
            {
                Sprite sp = skin.backdropSprites[i % skin.backdropSprites.Length];
                bool left = i % 2 == 0;
                var p = new Vector2(left ? R(-14f, 62f) : R(258f, 334f), (i / 2 + 0.5f) * 268f / ((n + 1) / 2) - 44f + R(-10f, 10f));
                Image img = UIFactory.Sprite(skin, root, "Bacterium", sp, p.x, p.y);
                bacteria.Add(new Mote
                {
                    rt = img.rectTransform, img = img, pos = p, margin = true, seed = R(0f, 100f),
                    vel = new Vector2(R(-0.8f, 0.8f), R(2f, 5f) * (rng.Next(2) == 0 ? -1f : 1f)),
                    spin = R(-30f, 30f),
                });
            }

            // The worm: head plus segments, hidden until it crawls.
            if (skin.crawlerHead != null && skin.crawlerSegment != null)
            {
                for (int i = 0; i < 9; i++)
                {
                    Image img = UIFactory.Sprite(skin, root, i == 0 ? "WormHead" : "WormSegment", i == 0 ? skin.crawlerHead : skin.crawlerSegment, 0f, 0f);
                    img.gameObject.SetActive(false);
                    worm.Add(img.rectTransform);
                }
                worm.Reverse(); // head drawn last (on top)
                wormTimer = s.menuCrawlerEvery * 0.4f;
            }
        }

        private void Update()
        {
            if (rebuild) { rebuild = false; Build(); }
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.1f);
            float t = Time.unscaledTime;

            foreach (Mote m in dust)
            {
                Vector2 flow = new Vector2(Mathf.Sin(m.pos.y * 0.05f + t * 0.21f + m.seed), Mathf.Cos(m.pos.x * 0.04f - t * 0.17f + m.seed * 0.5f)) * 2.2f;
                m.pos += (flow + new Vector2(0f, -1.2f)) * dt; // and a gentle upward drift (y grows downwards)
                if (m.pos.x < -80f) m.pos.x += 480f; else if (m.pos.x > 400f) m.pos.x -= 480f;
                if (m.pos.y < -60f) m.pos.y += 300f; else if (m.pos.y > 240f) m.pos.y -= 300f;
                if (m.blink > 0f) m.blink -= dt;
                else if (Random.value < 0.12f * dt) m.blink = Random.Range(0.08f, 0.35f);
                m.img.enabled = m.blink <= 0f;
                Place(m.rt, m.pos);
            }

            if (showBacteria)
            {
                foreach (Mote m in ghosts)
                {
                    m.pos += (m.vel + new Vector2(Mathf.Sin(t * 0.5f + m.seed), Mathf.Cos(t * 0.4f + m.seed)) * 1.5f) * dt;
                    if (m.pos.x < -60f) m.pos.x += 440f; else if (m.pos.x > 380f) m.pos.x -= 440f;
                    if (m.pos.y < -50f) m.pos.y += 280f; else if (m.pos.y > 230f) m.pos.y -= 280f;
                    m.frameClock += dt * m.fps;
                    Sprite f = m.frames[(int)m.frameClock % m.frames.Length];
                    if (m.img.sprite != f) { m.img.sprite = f; m.img.SetNativeSize(); }
                    Place(m.rt, m.pos);
                }

                foreach (Mote m in bacteria)
                {
                    m.pos += m.vel * dt;
                    if (m.pos.x < 160f) m.pos.x = Mathf.Clamp(m.pos.x, -20f, 66f);
                    else m.pos.x = Mathf.Clamp(m.pos.x, 254f, 340f);
                    if (m.pos.y < -44f) m.pos.y += 268f; else if (m.pos.y > 224f) m.pos.y -= 268f; // wrap off screen
                    // A 1 px bob, so even the slow ones look alive.
                    Vector2 bob = new Vector2(0f, Mathf.Round(Mathf.Sin(t * 1.7f + m.seed)));
                    Place(m.rt, m.pos + bob);
                    m.rt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Round(t * m.spin / 90f) * 90f);
                }

                UpdatePulse(dt);
                UpdateWorm(dt, t);
            }
        }

        private void UpdatePulse(float dt)
        {
            if (pulseAge < 0f)
            {
                pulseTimer -= dt;
                if (pulseTimer > 0f) return;
                pulseAge = 0f;
                bool left = Random.value < 0.5f;
                Place(pulse.rectTransform, new Vector2(left ? Random.Range(0f, 50f) : Random.Range(270f, 320f), Random.Range(10f, 170f)));
                pulse.enabled = true;
            }
            pulseAge += dt;
            float k = pulseAge / 1.8f;
            if (k >= 1f)
            {
                pulse.enabled = false;
                pulseAge = -1f;
                pulseTimer = Random.Range(s.menuPulseEveryMin, s.menuPulseEveryMax);
                return;
            }
            float eased = 1f - (1f - k) * (1f - k);
            Sprite f = pulseFrames[Mathf.Min(pulseFrames.Length - 1, (int)(eased * pulseFrames.Length))];
            if (pulse.sprite != f) { pulse.sprite = f; pulse.SetNativeSize(); }
            pulse.color = new Color(1f, 1f, 1f, 0.45f * (1f - k));
        }

        private void UpdateWorm(float dt, float t)
        {
            if (worm.Count == 0 || s.menuCrawlerEvery <= 0f) return;
            if (!wormActive)
            {
                wormTimer -= dt;
                if (wormTimer > 0f) return;
                // Crawl along the top or the bottom edge, clear of the menu text.
                wormActive = true;
                wormDir = Random.value < 0.5f ? 1f : -1f;
                wormX = wormDir > 0f ? -60f : 380f;
                wormY = Random.value < 0.5f ? Random.Range(1f, 7f) : Random.Range(166f, 174f); // clear of the title and the buttons
                wormPhase = Random.value * 10f;
                wormTrail.Clear();
                foreach (RectTransform w in worm) w.gameObject.SetActive(true);
            }
            wormX += wormDir * 20f * dt;
            Vector2 head = new Vector2(wormX, wormY + Mathf.Sin(wormX * 0.06f + wormPhase) * 5f);
            if (wormTrail.Count == 0 || Vector2.Distance(wormTrail[0], head) >= 1f) wormTrail.Insert(0, head);
            int needed = (int)(SegmentGap * worm.Count) + 2;
            if (wormTrail.Count > needed) wormTrail.RemoveRange(needed, wormTrail.Count - needed);
            for (int i = 0; i < worm.Count; i++)
            {
                int seg = worm.Count - 1 - i; // worm[] runs tail..head
                int k = Mathf.Min(wormTrail.Count - 1, (int)(seg * SegmentGap));
                Place(worm[i], wormTrail[k]);
            }
            worm[worm.Count - 1].localScale = new Vector3(wormDir < 0f ? -1f : 1f, 1f, 1f);
            if ((wormDir > 0f && wormX > 380f + SegmentGap * worm.Count) || (wormDir < 0f && wormX < -60f - SegmentGap * worm.Count))
            {
                wormActive = false;
                wormTimer = s.menuCrawlerEvery * Random.Range(0.8f, 1.2f);
                foreach (RectTransform w in worm) w.gameObject.SetActive(false);
            }
        }

        private static void Place(RectTransform rt, Vector2 pos) =>
            rt.anchoredPosition = new Vector2(Mathf.Round(pos.x), -Mathf.Round(pos.y));
    }
}
