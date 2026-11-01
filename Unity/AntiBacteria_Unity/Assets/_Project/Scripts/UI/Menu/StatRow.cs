using System;
using AntiBacteria.Rendering;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AntiBacteria.UI.Menu
{
    /// <summary>"Score ........ 1250": dim label on the left, value on the right, dotted leader between.</summary>
    public class StatRow : MonoBehaviour, IRefreshable
    {
        private TextMeshProUGUI label, value;
        private Image dots;
        private Func<string> labelText, valueText;
        private float x0, x1;

        public static StatRow Create(UISkin skin, Transform parent, string name, float x0, float x1, float y,
            Func<string> label, Func<string> value)
        {
            RectTransform row = UIFactory.Rect(parent, name, 0f, y, 0f, 0f, new Vector2(0f, 1f));
            var s = row.gameObject.AddComponent<StatRow>();
            s.x0 = x0;
            s.x1 = x1;
            s.labelText = label;
            s.valueText = value;
            s.label = UIFactory.Text(skin, row, "Label", x0, 0f, HorizontalAlignmentOptions.Left, 1, PaletteRole.Foreground, true);
            s.value = value != null ? UIFactory.Text(skin, row, "Value", x1, 0f, HorizontalAlignmentOptions.Right) : null;
            // One long dotted strip, cut to length with a horizontal fill (UI tiling of tiny textures leaves gaps).
            s.dots = UIFactory.Image(row, "Dots", skin.dot, x0, 9f, skin.dot.rect.width, 1f, PaletteRole.Foreground, true);
            s.dots.type = Image.Type.Filled;
            s.dots.fillMethod = Image.FillMethod.Horizontal;
            s.dots.fillOrigin = (int)Image.OriginHorizontal.Left;
            s.Refresh();
            return s;
        }

        public void Refresh()
        {
            if (label == null) return;
            label.text = labelText != null ? labelText() : "";
            float lw = UIFactory.Width(label, label.text);
            float vw = 0f;
            if (value != null)
            {
                value.text = valueText != null ? valueText() : "";
                vw = UIFactory.Width(value, value.text);
            }
            float start = x0 + lw + 4f, end = x1 - vw - 4f;
            dots.enabled = value != null && end > start;
            dots.rectTransform.anchoredPosition = new Vector2(start, -9f);
            float length = Mathf.Max(0f, Mathf.Floor((end - start) / 3f) * 3f);
            dots.fillAmount = Mathf.Clamp01(length / dots.rectTransform.sizeDelta.x);
        }
    }
}
