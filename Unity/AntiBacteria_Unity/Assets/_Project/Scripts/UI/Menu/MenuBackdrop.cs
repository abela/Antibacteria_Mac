using AntiBacteria.Rendering;
using UnityEngine;
using UnityEngine.UI;

namespace AntiBacteria.UI.Menu
{
    /// <summary>
    /// Slowly drifting, spinning bacteria and dust behind the main menu. The bacteria stay in the left and right
    /// margins (outside the 80..240 px column the menu text uses) and drift vertically, wrapping top to bottom.
    /// </summary>
    public class MenuBackdrop : MonoBehaviour
    {
        private struct Mote
        {
            public RectTransform rt;
            public Vector2 pos, vel;
            public float spin;
        }

        private Mote[] motes = new Mote[0];

        /// <summary>Hide the bacteria on busy screens (Options uses the full width); the dust stays.</summary>
        public void ShowBacteria(bool show)
        {
            foreach (Mote m in motes) m.rt.gameObject.SetActive(show);
        }

        public static MenuBackdrop Create(UISkin skin, Transform parent, int bacteria = 7, int dust = 40, int seed = 3)
        {
            RectTransform root = UIFactory.Stretch(parent, "Backdrop");
            var b = root.gameObject.AddComponent<MenuBackdrop>();
            var rng = new System.Random(seed);
            float R(float a, float c) => a + (float)rng.NextDouble() * (c - a);

            for (int i = 0; i < dust; i++)
                UIFactory.Image(root, "Dust", skin.pixel, Mathf.Round(R(0f, 320f)), Mathf.Round(R(0f, 180f)), 1f, 1f,
                    PaletteRole.Foreground, true);

            int n = skin.backdropSprites.Length > 0 ? bacteria : 0;
            b.motes = new Mote[n];
            for (int i = 0; i < n; i++)
            {
                Sprite s = skin.backdropSprites[i % skin.backdropSprites.Length];
                // Alternate left / right margin, spread out vertically.
                bool left = i % 2 == 0;
                var p = new Vector2(left ? R(10f, 62f) : R(258f, 310f), (i + 0.5f) * 180f / n + R(-8f, 8f));
                Image img = UIFactory.Sprite(skin, root, "Bacterium", s, p.x, p.y);
                b.motes[i] = new Mote
                {
                    rt = img.rectTransform,
                    pos = p,
                    vel = new Vector2(R(-0.6f, 0.6f), R(2f, 4f) * (rng.Next(2) == 0 ? -1f : 1f)),
                    spin = R(-25f, 25f),
                };
            }
            return b;
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            for (int i = 0; i < motes.Length; i++)
            {
                Mote m = motes[i];
                m.pos += m.vel * dt;
                if (m.pos.x < 160f) m.pos.x = Mathf.Clamp(m.pos.x, 4f, 66f);
                else m.pos.x = Mathf.Clamp(m.pos.x, 254f, 316f);
                if (m.pos.y < -12f) m.pos.y += 204f; else if (m.pos.y > 192f) m.pos.y -= 204f;
                m.rt.anchoredPosition = new Vector2(Mathf.Round(m.pos.x), -Mathf.Round(m.pos.y));
                // Spin in 90-degree steps like the original BlueCorosia, so pixels stay on the grid.
                m.rt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Round(Time.unscaledTime * m.spin / 90f) * 90f);
                motes[i] = m;
            }
        }
    }
}
