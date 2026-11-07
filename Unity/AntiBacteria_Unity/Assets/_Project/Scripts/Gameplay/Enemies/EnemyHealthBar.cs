using AntiBacteria.Core;
using UnityEngine;

namespace AntiBacteria.Gameplay.Enemies
{
    /// <summary>
    /// A small life bar over an enemy that takes more than one hit: a 1 px accent line on a background-coloured box,
    /// 2 px above the sprite, shrinking with each hit. It shows once the enemy has been hit (a calm screen until then).
    /// Bosses have the big bar at the top of the screen instead.
    /// </summary>
    public class EnemyHealthBar : MonoBehaviour
    {
        private const int MinWidth = 6, MaxWidth = 14;

        private static Sprite red, black;
        private Enemy enemy;
        private Transform root;
        private SpriteRenderer back, fill;
        private int width;
        private float topPixels;

        public static void Attach(Enemy e)
        {
            if (e == null || e.GetComponent<EnemyHealthBar>() != null) return;
            e.gameObject.AddComponent<EnemyHealthBar>().enemy = e;
        }

        private static Sprite Pixel(Color c)
        {
            var t = new Texture2D(1, 1, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            t.SetPixel(0, 0, c);
            t.Apply();
            return Sprite.Create(t, new Rect(0, 0, 1, 1), new Vector2(0f, 1f), 1f); // 1 sprite pixel = 1 unit (scaled below)
        }

        private void Start()
        {
            if (enemy == null) enemy = GetComponent<Enemy>();
            if (red == null) red = Pixel(Color.red);     // palette shader: red = accent
            if (black == null) black = Pixel(Color.black); // black = background

            SpriteRenderer body = enemy.Body;
            Material mat = body != null ? body.sharedMaterial : null;
            int order = body != null ? body.sortingOrder + 5 : 50;
            Vector2 size = body != null && body.sprite != null ? body.sprite.rect.size : new Vector2(8f, 8f);
            width = Mathf.Clamp(Mathf.RoundToInt(size.x) - 2, MinWidth, MaxWidth);
            topPixels = Mathf.Ceil(size.y / 2f) + 2f;

            root = new GameObject("HealthBar").transform;
            root.SetParent(transform, false);
            back = Part("Back", black, mat, order, width + 2, 3);
            fill = Part("Fill", red, mat, order + 1, width, 1);
            fill.transform.localPosition = new Vector3(1f / Legacy.PixelsPerUnit, -1f / Legacy.PixelsPerUnit, 0f);
            root.gameObject.SetActive(false);
        }

        private SpriteRenderer Part(string n, Sprite s, Material mat, int order, int w, int h)
        {
            var go = new GameObject(n);
            go.transform.SetParent(root, false);
            go.transform.localScale = new Vector3(w / Legacy.PixelsPerUnit, h / Legacy.PixelsPerUnit, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = s;
            if (mat != null) sr.sharedMaterial = mat;
            sr.sortingOrder = order;
            return sr;
        }

        private void LateUpdate()
        {
            if (enemy == null || root == null) return;
            bool show = enemy.Current == Enemy.State.Alive && enemy.MaxHits > 1 && enemy.HitsLeft < enemy.MaxHits && enemy.HitsLeft > 0
                        && enemy.Body != null && enemy.Body.enabled;
            if (root.gameObject.activeSelf != show) root.gameObject.SetActive(show);
            if (!show) return;

            // Upright and on the pixel grid, whatever the body does.
            float ppu = Legacy.PixelsPerUnit;
            Vector3 p = transform.position;
            float left = Mathf.Round(p.x * ppu - (width + 2) / 2f) / ppu, top = Mathf.Round(p.y * ppu + topPixels + 3f) / ppu;
            root.SetPositionAndRotation(new Vector3(left, top, p.z), Quaternion.identity);
            Vector3 ls = transform.lossyScale;
            root.localScale = new Vector3(ls.x != 0f ? 1f / ls.x : 1f, ls.y != 0f ? 1f / ls.y : 1f, 1f);
            int w = Mathf.Max(1, Mathf.CeilToInt(width * (float)enemy.HitsLeft / enemy.MaxHits));
            fill.transform.localScale = new Vector3(w / ppu, 1f / ppu, 1f);
        }
    }
}
