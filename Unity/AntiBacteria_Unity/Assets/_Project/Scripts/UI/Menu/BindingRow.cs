using System;
using AntiBacteria.Rendering;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AntiBacteria.UI.Menu
{
    /// <summary>
    /// "Move up ........ W" on the Controls screen. Confirm starts listening; the value blinks "Press a key 4"
    /// until a key is pressed, Esc is pressed or time runs out.
    /// </summary>
    public class BindingRow : MenuItem
    {
        private const float LabelX = 48f, RowRight = 292f, ValueRight = 272f;

        private TextMeshProUGUI label, value;
        private Image selector;
        private Func<string> labelText, valueText;
        private Func<bool> listening;
        private Action start;

        public static BindingRow Create(UISkin skin, Transform parent, string name, float y, Func<string> label,
            Func<string> value, Func<bool> isListening, Action startListening)
        {
            RectTransform row = UIFactory.Rect(parent, name, LabelX - 12f, y, RowRight - (LabelX - 12f), skin.rowHeight, new Vector2(0f, 1f));
            var b = row.gameObject.AddComponent<BindingRow>();
            b.Hit = row;
            b.labelText = label;
            b.valueText = value;
            b.listening = isListening;
            b.start = startListening;
            b.selector = UIFactory.Image(row, "Selector", skin.arrow, 3f, 5f, 3f, 5f, PaletteRole.Accent);
            b.label = UIFactory.Text(skin, row, "Label", 12f, 0f, HorizontalAlignmentOptions.Left);
            b.value = UIFactory.Text(skin, row, "Value", ValueRight - (LabelX - 12f), 0f, HorizontalAlignmentOptions.Right);
            b.Refresh();
            return b;
        }

        public override void Submit()
        {
            if (Interactable && !listening()) start?.Invoke();
        }

        private void Update()
        {
            if (listening != null && listening()) Refresh(); // blink and count down
        }

        public override void Refresh()
        {
            if (label == null) return;
            bool active = Selected && Interactable;
            bool waiting = listening != null && listening();
            label.text = labelText != null ? labelText() : "";
            value.text = valueText != null ? valueText() : "";
            label.GetComponent<PaletteGraphic>().Set(active ? PaletteRole.Accent : PaletteRole.Foreground, !Interactable);
            value.GetComponent<PaletteGraphic>().Set(active || waiting ? PaletteRole.Accent : PaletteRole.Foreground, !Interactable);
            value.enabled = !waiting || Mathf.FloorToInt(Time.unscaledTime / 0.3f) % 2 == 0;
            selector.enabled = active;
        }
    }
}
