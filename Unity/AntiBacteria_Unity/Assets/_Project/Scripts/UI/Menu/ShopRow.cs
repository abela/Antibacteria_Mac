using System;
using AntiBacteria.Rendering;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AntiBacteria.UI.Menu
{
    /// <summary>
    /// A shop row: name on the left, price or state on the right ("20,000", "Equipped", "On").
    /// Confirm / click runs its action (buy, equip, toggle).
    /// </summary>
    public class ShopRow : MenuItem
    {
        private TextMeshProUGUI label, value;
        private Image selector;
        private Func<string> labelText, valueText;
        private Func<PaletteRole> valueRole;
        private Func<bool> valueDim;
        private Action onSubmit;

        public static ShopRow Create(UISkin skin, Transform parent, string name, float x, float y, float width,
            Func<string> label, Func<string> value, Func<PaletteRole> valueRole, Func<bool> valueDim, Action onSubmit)
        {
            RectTransform row = UIFactory.Rect(parent, name, x, y, width, skin.rowHeight, new Vector2(0f, 1f));
            var r = row.gameObject.AddComponent<ShopRow>();
            r.Hit = row;
            r.labelText = label;
            r.valueText = value;
            r.valueRole = valueRole;
            r.valueDim = valueDim;
            r.onSubmit = onSubmit;
            r.selector = UIFactory.Image(row, "Selector", skin.arrow, 3f, 5f, 3f, 5f, PaletteRole.Accent);
            r.label = UIFactory.Text(skin, row, "Label", 12f, 0f, HorizontalAlignmentOptions.Left);
            r.value = UIFactory.Text(skin, row, "Value", width - 2f, 0f, HorizontalAlignmentOptions.Right);
            r.Refresh();
            return r;
        }

        public override void Submit()
        {
            if (Interactable) onSubmit?.Invoke();
        }

        public override void Refresh()
        {
            if (label == null) return;
            bool active = Selected && Interactable;
            label.text = labelText != null ? labelText() : "";
            label.GetComponent<PaletteGraphic>().Set(active ? PaletteRole.Accent : PaletteRole.Foreground, !Interactable);
            selector.enabled = active;
            value.text = valueText != null ? valueText() : "";
            value.GetComponent<PaletteGraphic>().Set(valueRole != null ? valueRole() : PaletteRole.Foreground,
                valueDim != null && valueDim());
        }
    }
}
