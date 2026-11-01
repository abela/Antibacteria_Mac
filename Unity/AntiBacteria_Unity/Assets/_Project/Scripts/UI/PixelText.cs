using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace AntiBacteria.UI
{
    /// <summary>
    /// Draws short strings (numbers, "x", "-", ":") from 3x5 glyph sprites, pixel-exact.
    /// Placeholder for HUD numbers until the TextMeshPro pixel font is in.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class PixelText : MonoBehaviour
    {
        public enum Align { Left, Center, Right }

        [SerializeField] private string glyphChars = "0123456789-x:";
        [SerializeField] private Sprite[] glyphs = new Sprite[0];
        [SerializeField] private Material material;
        [SerializeField] private int spacingPixels = 1;
        [SerializeField] private Align align = Align.Left;

        private readonly List<Image> pool = new List<Image>();
        private string current;

        public void SetText(string text)
        {
            if (text == current) return;
            current = text;
            int x = 0, used = 0;
            var placed = new List<KeyValuePair<Image, int>>();
            foreach (char c in text)
            {
                int gi = glyphChars.IndexOf(c);
                if (gi < 0 || gi >= glyphs.Length || glyphs[gi] == null) { x += 3 + spacingPixels; continue; } // space / unknown
                Image img = GetImage(used++);
                Sprite s = glyphs[gi];
                img.sprite = s;
                img.rectTransform.sizeDelta = new Vector2(s.rect.width, s.rect.height);
                placed.Add(new KeyValuePair<Image, int>(img, x));
                x += (int)s.rect.width + spacingPixels;
            }
            int width = Mathf.Max(0, x - spacingPixels);
            int start = align == Align.Left ? 0 : align == Align.Right ? -width : -width / 2;
            foreach (var p in placed) p.Key.rectTransform.anchoredPosition = new Vector2(start + p.Value, 0f);
            for (int i = 0; i < pool.Count; i++) pool[i].enabled = i < used;
        }

        private Image GetImage(int i)
        {
            while (pool.Count <= i)
            {
                var go = new GameObject("Glyph", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(transform, false);
                var img = go.GetComponent<Image>();
                img.material = material;
                img.raycastTarget = false;
                var rt = img.rectTransform;
                rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot = new Vector2(0f, 1f);
                pool.Add(img);
            }
            pool[i].enabled = true;
            return pool[i];
        }
    }
}
