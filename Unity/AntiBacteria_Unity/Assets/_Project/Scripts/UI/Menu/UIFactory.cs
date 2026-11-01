using AntiBacteria.Rendering;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AntiBacteria.UI.Menu
{
    /// <summary>
    /// Creates menu elements in game pixels: x, y are measured from the TOP-LEFT of the 320x180 screen,
    /// sizes are in pixels. Colours always come from the palette (PaletteGraphic).
    /// </summary>
    public static class UIFactory
    {
        public const float ScreenW = 320f;
        public const float ScreenH = 180f;

        public static RectTransform Rect(Transform parent, string name, float x, float y, float w, float h, Vector2 pivot)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = parent.gameObject.layer;
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = pivot;
            // Floor(v + 0.5) rather than Mathf.Round: Round sends .5 to the nearest EVEN number, which spaces rows unevenly.
            rt.anchoredPosition = new Vector2(Mathf.Floor(x + 0.5f), -Mathf.Floor(y + 0.5f));
            rt.sizeDelta = new Vector2(w, h);
            return rt;
        }

        public static RectTransform Stretch(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = parent.gameObject.layer;
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            return rt;
        }

        /// <summary>Text whose x is its left edge, centre or right edge depending on alignment; y is the top of the line.</summary>
        public static TextMeshProUGUI Text(UISkin skin, Transform parent, string name, float x, float y,
            HorizontalAlignmentOptions align, int scale = 1, PaletteRole role = PaletteRole.Foreground, bool dim = false)
        {
            float px = align == HorizontalAlignmentOptions.Left ? 0f : align == HorizontalAlignmentOptions.Right ? 1f : 0.5f;
            RectTransform rt = Rect(parent, name, x, y, 300f, skin.rowHeight * scale, new Vector2(px, 1f));
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            t.font = skin.font;
            t.fontSize = skin.fontSize * scale;
            t.horizontalAlignment = align;
            t.verticalAlignment = VerticalAlignmentOptions.Top;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.overflowMode = TextOverflowModes.Overflow;
            t.richText = false;
            t.raycastTarget = false;
            t.extraPadding = false;
            Tint(t.gameObject, role, dim);
            return t;
        }

        public static Image Image(Transform parent, string name, Sprite sprite, float x, float y, float w, float h,
            PaletteRole role = PaletteRole.Foreground, bool dim = false, Vector2? pivot = null)
        {
            RectTransform rt = Rect(parent, name, x, y, w, h, pivot ?? new Vector2(0f, 1f));
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.raycastTarget = false;
            Tint(img.gameObject, role, dim);
            return img;
        }

        /// <summary>A palette-indexed sprite (enemy, heart...) at its pixel size, centred on x, y.</summary>
        public static Image Sprite(UISkin skin, Transform parent, string name, Sprite sprite, float x, float y)
        {
            RectTransform rt = Rect(parent, name, x, y, sprite.rect.width, sprite.rect.height, new Vector2(0.5f, 0.5f));
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.material = skin.paletteMaterial;
            img.raycastTarget = false;
            return img;
        }

        public static PaletteGraphic Tint(GameObject go, PaletteRole role, bool dim)
        {
            var pg = go.GetComponent<PaletteGraphic>() ?? go.AddComponent<PaletteGraphic>();
            pg.Set(role, dim);
            return pg;
        }

        /// <summary>1 px rectangle outline, inclusive pixel coordinates.</summary>
        public static void Frame(UISkin skin, Transform parent, float x0, float y0, float x1, float y1, PaletteRole role, bool dim)
        {
            float w = x1 - x0 + 1f, h = y1 - y0 + 1f;
            Image(parent, "FrameTop", skin.pixel, x0, y0, w, 1f, role, dim);
            Image(parent, "FrameBottom", skin.pixel, x0, y1, w, 1f, role, dim);
            Image(parent, "FrameLeft", skin.pixel, x0, y0, 1f, h, role, dim);
            Image(parent, "FrameRight", skin.pixel, x1, y0, 1f, h, role, dim);
        }

        /// <summary>Solid background box with a foreground frame and a dim inner frame.</summary>
        public static void Panel(UISkin skin, Transform parent, float x0, float y0, float x1, float y1)
        {
            Image(parent, "PanelFill", skin.pixel, x0, y0, x1 - x0 + 1f, y1 - y0 + 1f, PaletteRole.Background);
            Frame(skin, parent, x0, y0, x1, y1, PaletteRole.Foreground, false);
            Frame(skin, parent, x0 + 2f, y0 + 2f, x1 - 2f, y1 - 2f, PaletteRole.Foreground, true);
        }

        public static float Width(TextMeshProUGUI t, string s) => Mathf.Ceil(t.GetPreferredValues(s).x);
    }
}
