using System;
using AntiBacteria.Rendering;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AntiBacteria.UI.Menu
{
    /// <summary>"<  WORLD 2  >" at the top of level select: left / right (or a click on either half) changes world.</summary>
    public class WorldPicker : MenuItem
    {
        private const float Width = 160f, ArrowGap = 6f;

        private TextMeshProUGUI label;
        private Image left, right;
        private Func<string> text;
        private Func<int, bool> step; // returns false at the first / last world

        public override bool ChangesValue => true;

        public static WorldPicker Create(UISkin skin, Transform parent, float y, Func<string> text, Func<int, bool> step)
        {
            RectTransform row = UIFactory.Rect(parent, "WorldPicker", 160f, y, Width, skin.rowHeight, new Vector2(0.5f, 1f));
            var p = row.gameObject.AddComponent<WorldPicker>();
            p.Hit = row;
            p.text = text;
            p.step = step;
            p.label = UIFactory.Text(skin, row, "Label", Width / 2f, 0f, HorizontalAlignmentOptions.Center);
            p.left = UIFactory.Image(row, "ArrowL", skin.arrow, 0f, 5f, 3f, 5f, PaletteRole.Accent);
            p.left.rectTransform.localScale = new Vector3(-1f, 1f, 1f);
            p.right = UIFactory.Image(row, "ArrowR", skin.arrow, 0f, 5f, 3f, 5f, PaletteRole.Accent);
            p.Refresh();
            return p;
        }

        public override bool Step(int direction) => step != null && step(direction);

        public override void Click(Vector2 screenPosition)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(Hit, screenPosition, null, out Vector2 local);
            Step(local.x < 0f ? -1 : 1);
            Refresh();
        }

        public override void Refresh()
        {
            if (label == null) return;
            string s = text != null ? text() : "";
            label.text = s;
            bool active = Selected && Interactable;
            label.GetComponent<PaletteGraphic>().Set(active ? PaletteRole.Accent : PaletteRole.Foreground, !active);
            float half = Mathf.Ceil(UIFactory.Width(label, s) / 2f);
            left.rectTransform.anchoredPosition = new Vector2(Width / 2f - half - ArrowGap, -5f);
            right.rectTransform.anchoredPosition = new Vector2(Width / 2f + half + ArrowGap - 3f, -5f);
            left.GetComponent<PaletteGraphic>().Set(active ? PaletteRole.Accent : PaletteRole.Foreground, !active);
            right.GetComponent<PaletteGraphic>().Set(active ? PaletteRole.Accent : PaletteRole.Foreground, !active);
        }
    }
}
