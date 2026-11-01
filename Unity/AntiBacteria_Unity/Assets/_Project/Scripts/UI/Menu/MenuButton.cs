using System;
using AntiBacteria.Rendering;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AntiBacteria.UI.Menu
{
    /// <summary>Centred text button. Selected: accent colour with an arrow on each side. Disabled: dim.</summary>
    public class MenuButton : MenuItem
    {
        private const float ArrowGap = 6f;
        private const float ArrowW = 3f;

        private TextMeshProUGUI label;
        private Image leftArrow, rightArrow;
        private Func<string> text;
        private Action onSubmit;

        public static MenuButton Create(UISkin skin, Transform parent, string name, float centreX, float y,
            Func<string> text, Action onSubmit)
        {
            RectTransform row = UIFactory.Rect(parent, name, centreX, y, 100f, skin.rowHeight, new Vector2(0.5f, 1f));
            var b = row.gameObject.AddComponent<MenuButton>();
            b.Hit = row;
            b.text = text;
            b.onSubmit = onSubmit;
            b.label = UIFactory.Text(skin, row, "Label", 50f, 0f, HorizontalAlignmentOptions.Center);
            b.leftArrow = UIFactory.Image(row, "ArrowL", skin.arrow, 0f, 5f, ArrowW, 5f, PaletteRole.Accent);
            b.rightArrow = UIFactory.Image(row, "ArrowR", skin.arrow, 0f, 5f, ArrowW, 5f, PaletteRole.Accent);
            b.rightArrow.rectTransform.localScale = new Vector3(-1f, 1f, 1f); // points left
            b.Refresh();
            return b;
        }

        public override void Submit()
        {
            if (Interactable) onSubmit?.Invoke();
        }

        public override void Refresh()
        {
            if (label == null) return;
            string s = text != null ? text() : "";
            label.text = s;
            bool active = Selected && Interactable;
            label.GetComponent<PaletteGraphic>().Set(active ? PaletteRole.Accent : PaletteRole.Foreground, !Interactable);

            // Row = arrow, gap, text, gap, arrow. Kept an even width so the centre lands on a whole pixel.
            float w = UIFactory.Width(label, s);
            float rowW = w + 2f * (ArrowGap + ArrowW);
            if (rowW % 2f > 0.01f) rowW += 1f;
            Hit.sizeDelta = new Vector2(rowW, Hit.sizeDelta.y);
            label.rectTransform.anchoredPosition = new Vector2(rowW / 2f, 0f);
            leftArrow.rectTransform.anchoredPosition = new Vector2(0f, -5f);
            rightArrow.rectTransform.anchoredPosition = new Vector2(rowW, -5f); // mirrored: draws from rowW-3 to rowW
            leftArrow.enabled = rightArrow.enabled = active;
        }
    }
}
