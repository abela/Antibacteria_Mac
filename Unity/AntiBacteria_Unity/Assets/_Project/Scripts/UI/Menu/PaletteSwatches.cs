using System;
using AntiBacteria.Rendering;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AntiBacteria.UI.Menu
{
    /// <summary>
    /// The Options row for the colour palette: label on the left, one swatch per palette on the right
    /// (its background with a foreground and an accent stripe, in that palette's real colours), and a frame
    /// around the one in use. Left/right moves along the row and applies the palette at once; Confirm steps
    /// to the next one; a click picks the swatch under the pointer.
    /// </summary>
    public class PaletteSwatches : MenuItem
    {
        public override bool ChangesValue => true;

        private const float RowRight = 292f; // same clickable width as MenuOption rows
        private const float Pitch = 11f;     // 10 px swatch (1 px border + 8 px colours) and a 1 px gap
        private const float Top = 2f;        // swatch top inside the row

        private UISkin skin;
        private TextMeshProUGUI label;
        private Image selector;
        private Func<string> labelText;
        private Func<int> count;
        private Func<int, Palette> palette;
        private Func<int> getIndex;
        private Action<int> setIndex;

        private Image[] backgrounds, foregrounds, accents;
        private RectTransform cursor;
        private PaletteGraphic[] cursorParts;
        private float x0;

        public static PaletteSwatches Create(UISkin skin, Transform parent, string name, float y, Func<string> label,
            Func<int> count, Func<int, Palette> palette, Func<int> get, Action<int> set)
        {
            const float left = MenuOption.LabelX - 12f;
            RectTransform row = UIFactory.Rect(parent, name, left, y, RowRight - left, skin.rowHeight, new Vector2(0f, 1f));
            var o = row.gameObject.AddComponent<PaletteSwatches>();
            o.Hit = row;
            o.skin = skin;
            o.labelText = label;
            o.count = count;
            o.palette = palette;
            o.getIndex = get;
            o.setIndex = set;
            o.selector = UIFactory.Image(row, "Selector", skin.arrow, 3f, 5f, 3f, 5f, PaletteRole.Accent);
            o.label = UIFactory.Text(skin, row, "Label", 12f, 0f, HorizontalAlignmentOptions.Left);
            o.Build();
            o.Refresh();
            return o;
        }

        private int Count => count != null ? count() : 0;

        /// <summary>Swatches are built once the palette list is known (it lives on the scene's PaletteManager).</summary>
        private void Build()
        {
            int n = Count;
            if (n <= 0 || backgrounds != null) return;
            Transform row = transform;
            // Centred on the value column of the other option rows.
            float centre = MenuOption.ValueX - MenuOption.LabelX + 12f;
            x0 = Mathf.Floor(centre - (n * Pitch - 1f) / 2f);
            backgrounds = new Image[n];
            foregrounds = new Image[n];
            accents = new Image[n];
            for (int i = 0; i < n; i++)
            {
                float x = x0 + i * Pitch;
                // Dim border in the current palette, so a swatch shows even when its background matches the menu's.
                UIFactory.Frame(skin, row, x, Top, x + 9f, Top + 9f, PaletteRole.Foreground, true);
                backgrounds[i] = Raw(row, "Swatch" + i, x + 1f, Top + 1f, 8f, 8f);
                foregrounds[i] = Raw(row, "SwatchFg" + i, x + 2f, Top + 2f, 3f, 6f);
                accents[i] = Raw(row, "SwatchAccent" + i, x + 5f, Top + 2f, 3f, 6f);
            }
            cursor = UIFactory.Rect(row, "Cursor", x0 - 1f, Top - 1f, 12f, 12f, new Vector2(0f, 1f));
            UIFactory.Frame(skin, cursor, 0f, 0f, 11f, 11f, PaletteRole.Accent, false);
            cursorParts = cursor.GetComponentsInChildren<PaletteGraphic>();
        }

        /// <summary>An image in a fixed colour (not tied to a palette role).</summary>
        private Image Raw(Transform parent, string name, float x, float y, float w, float h)
        {
            RectTransform rt = UIFactory.Rect(parent, name, x, y, w, h, new Vector2(0f, 1f));
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = skin.pixel;
            img.raycastTarget = false;
            return img;
        }

        private void Pick(int index)
        {
            int n = Count;
            if (n <= 0) return;
            setIndex?.Invoke(((index % n) + n) % n);
            Refresh();
        }

        public override void Submit()
        {
            if (!Interactable) return;
            Pick(getIndex() + 1);
        }

        public override bool Step(int direction)
        {
            if (!Interactable || direction == 0) return false;
            Pick(getIndex() + direction);
            return true;
        }

        public override void Click(Vector2 screenPosition)
        {
            if (!Interactable) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(Hit, screenPosition, null, out Vector2 local);
            // Hit's pivot is its top-left corner, so local.x counts from the row's left edge.
            int i = Mathf.FloorToInt((local.x - x0) / Pitch);
            if (local.x < x0 - 1f) { Submit(); return; } // on the label
            if (i >= 0 && i < Count) Pick(i);
        }

        public override void Refresh()
        {
            if (label == null) return;
            Build();
            bool active = Selected && Interactable;
            label.text = labelText != null ? labelText() : "";
            label.GetComponent<PaletteGraphic>().Set(active ? PaletteRole.Accent : PaletteRole.Foreground, !Interactable);
            selector.enabled = active;
            if (backgrounds == null) return;

            for (int i = 0; i < backgrounds.Length; i++)
            {
                Palette p = palette?.Invoke(i);
                if (p == null) continue;
                backgrounds[i].color = p.background;
                foregrounds[i].color = p.foreground;
                accents[i].color = p.accent;
            }
            int current = Mathf.Clamp(getIndex != null ? getIndex() : 0, 0, backgrounds.Length - 1);
            cursor.anchoredPosition = new Vector2(x0 + current * Pitch - 1f, -(Top - 1f));
            foreach (PaletteGraphic g in cursorParts) g.Set(active ? PaletteRole.Accent : PaletteRole.Foreground, false);
        }
    }
}
