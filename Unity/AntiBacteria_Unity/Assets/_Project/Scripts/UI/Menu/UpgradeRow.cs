using System;
using AntiBacteria.Rendering;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AntiBacteria.UI.Menu
{
    /// <summary>
    /// An Upgrade Character row: the name on the left, one box per level (filled = bought) and the next level's price
    /// on the right, or "MAX". Confirm / click buys the next level.
    /// </summary>
    public class UpgradeRow : MenuItem
    {
        private const float Pip = 5f, PipGap = 2f;

        private TextMeshProUGUI label, value;
        private Image selector;
        private Image[] pips;
        private PaletteGraphic[][] pipFrames;
        private Func<string> labelText, valueText;
        private Func<int> level;
        private Func<bool> valueDim;
        private Action onSubmit;

        /// <summary>Raised when this row becomes the selected one (the description line listens).</summary>
        public Action OnSelected;

        public static UpgradeRow Create(UISkin skin, Transform parent, string name, float x, float y, float width, int maxLevel,
            Func<string> label, Func<int> level, Func<string> value, Func<bool> valueDim, Action onSubmit)
        {
            RectTransform row = UIFactory.Rect(parent, name, x, y, width, skin.rowHeight, new Vector2(0f, 1f));
            var r = row.gameObject.AddComponent<UpgradeRow>();
            r.Hit = row;
            r.labelText = label;
            r.level = level;
            r.valueText = value;
            r.valueDim = valueDim;
            r.onSubmit = onSubmit;
            r.selector = UIFactory.Image(row, "Selector", skin.arrow, 3f, 5f, 3f, 5f, PaletteRole.Accent);
            r.label = UIFactory.Text(skin, row, "Label", 12f, 0f, HorizontalAlignmentOptions.Left);
            r.value = UIFactory.Text(skin, row, "Value", width - 2f, 0f, HorizontalAlignmentOptions.Right);

            // Level boxes in a column of their own, left of the price.
            r.pips = new Image[maxLevel];
            r.pipFrames = new PaletteGraphic[maxLevel][];
            float px = width - 58f - maxLevel * (Pip + PipGap);
            for (int i = 0; i < maxLevel; i++)
            {
                float x0 = px + i * (Pip + PipGap);
                var holder = UIFactory.Rect(row, "Level" + (i + 1), 0f, 0f, 0f, 0f, new Vector2(0f, 1f));
                UIFactory.Frame(skin, holder, x0, 4f, x0 + Pip - 1f, 4f + Pip - 1f, PaletteRole.Foreground, true);
                r.pipFrames[i] = holder.GetComponentsInChildren<PaletteGraphic>();
                r.pips[i] = UIFactory.Image(holder, "Fill", skin.pixel, x0 + 1f, 5f, Pip - 2f, Pip - 2f, PaletteRole.Accent);
            }
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
            if (active) OnSelected?.Invoke();
            label.text = labelText != null ? labelText() : "";
            label.GetComponent<PaletteGraphic>().Set(active ? PaletteRole.Accent : PaletteRole.Foreground, !Interactable);
            selector.enabled = active;
            value.text = valueText != null ? valueText() : "";
            value.GetComponent<PaletteGraphic>().Set(PaletteRole.Foreground, valueDim != null && valueDim());
            int l = level != null ? level() : 0;
            for (int i = 0; i < pips.Length; i++)
            {
                bool owned = i < l;
                pips[i].enabled = owned;
                foreach (PaletteGraphic f in pipFrames[i]) f.Set(owned ? PaletteRole.Accent : PaletteRole.Foreground, !owned);
            }
        }
    }
}
