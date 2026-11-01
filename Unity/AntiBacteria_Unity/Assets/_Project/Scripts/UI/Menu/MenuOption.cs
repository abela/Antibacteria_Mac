using System;
using AntiBacteria.Rendering;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AntiBacteria.UI.Menu
{
    /// <summary>
    /// An options row: label on the left, value on the right. Three kinds:
    /// Choice ("&lt; English &gt;"), Toggle (box) and Bar (10 blocks, for volumes).
    /// Left/right changes the value; Confirm and clicks change it too.
    /// </summary>
    public class MenuOption : MenuItem
    {
        public enum Kind { Choice, Toggle, Bar }

        public const float LabelX = 48f;
        public const float ValueX = 230f;    // centre of the value column
        private const float ChoiceHalf = 54f; // arrow distance from the value centre (fits "Полный экран")
        private const float RowRight = 292f;  // right end of the clickable row
        private const int BarBlocks = 10;

        private Kind kind;
        private TextMeshProUGUI label, value;
        private Image selector, arrowL, arrowR, boxFill;
        private Image[] boxFrame;
        private Image[] blocks;
        private UISkin skin;
        private Func<string> labelText;
        private Func<string> choiceText;
        private Action<int> stepChoice;
        private Func<bool> getToggle;
        private Action<bool> setToggle;
        private Func<int> getBar;
        private Action<int> setBar;

        public static MenuOption Choice(UISkin skin, Transform parent, string name, float y, Func<string> label,
            Func<string> value, Action<int> step)
        {
            MenuOption o = CreateRow(skin, parent, name, y, label, Kind.Choice);
            o.choiceText = value;
            o.stepChoice = step;
            Transform row = o.transform;
            o.value = UIFactory.Text(skin, row, "Value", ValueX - LabelX + 12f, 0f, HorizontalAlignmentOptions.Center);
            o.arrowL = UIFactory.Image(row, "ArrowL", skin.arrow, ValueX - LabelX + 12f - ChoiceHalf, 5f, 3f, 5f);
            o.arrowL.rectTransform.localScale = new Vector3(-1f, 1f, 1f);
            o.arrowR = UIFactory.Image(row, "ArrowR", skin.arrow, ValueX - LabelX + 12f + ChoiceHalf - 3f, 5f, 3f, 5f);
            o.Refresh();
            return o;
        }

        public static MenuOption Toggle(UISkin skin, Transform parent, string name, float y, Func<string> label,
            Func<bool> get, Action<bool> set)
        {
            MenuOption o = CreateRow(skin, parent, name, y, label, Kind.Toggle);
            o.getToggle = get;
            o.setToggle = set;
            float bx = ValueX - LabelX + 12f - 4f;
            o.boxFrame = new Image[4];
            var holder = UIFactory.Rect(o.transform, "Box", 0f, 0f, 0f, 0f, new Vector2(0f, 1f));
            UIFactory.Frame(skin, holder, bx, 3f, bx + 8f, 11f, PaletteRole.Foreground, false);
            o.boxFrame = holder.GetComponentsInChildren<Image>();
            o.boxFill = UIFactory.Image(o.transform, "Fill", skin.pixel, bx + 2f, 5f, 5f, 5f, PaletteRole.Accent);
            o.Refresh();
            return o;
        }

        /// <summary>
        /// Small toggle for corners: box first, then the label ("[x] Unlock all levels"). x is the left edge
        /// of the selection arrow; width is the clickable width.
        /// </summary>
        public static MenuOption CompactToggle(UISkin skin, Transform parent, string name, float x, float y, float width,
            Func<string> label, Func<bool> get, Action<bool> set)
        {
            MenuOption o = CreateRow(skin, parent, name, y, label, Kind.Toggle, x, width);
            o.getToggle = get;
            o.setToggle = set;
            const float bx = 9f;
            var holder = UIFactory.Rect(o.transform, "Box", 0f, 0f, 0f, 0f, new Vector2(0f, 1f));
            UIFactory.Frame(skin, holder, bx, 3f, bx + 8f, 11f, PaletteRole.Foreground, false);
            o.boxFrame = holder.GetComponentsInChildren<Image>();
            o.boxFill = UIFactory.Image(o.transform, "Fill", skin.pixel, bx + 2f, 5f, 5f, 5f, PaletteRole.Accent);
            o.label.rectTransform.anchoredPosition = new Vector2(bx + 12f, o.label.rectTransform.anchoredPosition.y);
            o.Refresh();
            return o;
        }

        public static MenuOption Bar(UISkin skin, Transform parent, string name, float y, Func<string> label,
            Func<int> get, Action<int> set)
        {
            MenuOption o = CreateRow(skin, parent, name, y, label, Kind.Bar);
            o.getBar = get;
            o.setBar = set;
            o.blocks = new Image[BarBlocks];
            float x0 = ValueX - LabelX + 12f - 34f;
            for (int i = 0; i < BarBlocks; i++)
                o.blocks[i] = UIFactory.Image(o.transform, "Block" + i, skin.pixel, x0 + i * 7f, 4f, 5f, 7f);
            o.Refresh();
            return o;
        }

        private static MenuOption CreateRow(UISkin skin, Transform parent, string name, float y, Func<string> label, Kind kind)
            => CreateRow(skin, parent, name, y, label, kind, LabelX - 12f, RowRight - (LabelX - 12f));

        private static MenuOption CreateRow(UISkin skin, Transform parent, string name, float y, Func<string> label, Kind kind,
            float x, float width)
        {
            // Row starts 12 px left of the label so the selection arrow is inside the clickable area.
            RectTransform row = UIFactory.Rect(parent, name, x, y, width, skin.rowHeight, new Vector2(0f, 1f));
            var o = row.gameObject.AddComponent<MenuOption>();
            o.Hit = row;
            o.skin = skin;
            o.kind = kind;
            o.labelText = label;
            o.selector = UIFactory.Image(row, "Selector", skin.arrow, 3f, 5f, 3f, 5f, PaletteRole.Accent);
            o.label = UIFactory.Text(skin, row, "Label", 12f, 0f, HorizontalAlignmentOptions.Left);
            return o;
        }

        public override void Submit()
        {
            if (!Interactable) return;
            switch (kind)
            {
                case Kind.Choice: stepChoice?.Invoke(1); break;
                case Kind.Toggle: setToggle?.Invoke(!getToggle()); break;
                case Kind.Bar: setBar?.Invoke((getBar() + 1) % (BarBlocks + 1)); break;
            }
            Refresh();
        }

        public override bool Step(int direction)
        {
            if (!Interactable || direction == 0) return false;
            switch (kind)
            {
                case Kind.Choice: stepChoice?.Invoke(direction); break;
                case Kind.Toggle: setToggle?.Invoke(!getToggle()); break;
                case Kind.Bar: setBar?.Invoke(Mathf.Clamp(getBar() + direction, 0, BarBlocks)); break;
            }
            Refresh();
            return true;
        }

        public override void Click(Vector2 screenPosition)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(Hit, screenPosition, null, out Vector2 local);
            float valueCentre = ValueX - LabelX + 12f;
            if (local.x < valueCentre - ChoiceHalf - 6f) { Submit(); return; } // on the label

            switch (kind)
            {
                case Kind.Choice: Step(local.x < valueCentre ? -1 : 1); break;
                case Kind.Toggle: Submit(); break;
                case Kind.Bar:
                    float x0 = valueCentre - 34f;
                    int v = Mathf.Clamp(Mathf.FloorToInt((local.x - x0) / 7f) + 1, 0, BarBlocks);
                    setBar?.Invoke(v);
                    Refresh();
                    break;
            }
        }

        public override void Refresh()
        {
            if (label == null) return;
            bool active = Selected && Interactable;
            label.text = labelText != null ? labelText() : "";
            label.GetComponent<PaletteGraphic>().Set(active ? PaletteRole.Accent : PaletteRole.Foreground, !Interactable);
            selector.enabled = active;

            switch (kind)
            {
                case Kind.Choice:
                    value.text = choiceText != null ? choiceText() : "";
                    value.GetComponent<PaletteGraphic>().Set(active ? PaletteRole.Accent : PaletteRole.Foreground, false);
                    arrowL.GetComponent<PaletteGraphic>().Set(active ? PaletteRole.Accent : PaletteRole.Foreground, !active);
                    arrowR.GetComponent<PaletteGraphic>().Set(active ? PaletteRole.Accent : PaletteRole.Foreground, !active);
                    break;
                case Kind.Toggle:
                    boxFill.enabled = getToggle != null && getToggle();
                    foreach (Image f in boxFrame) f.GetComponent<PaletteGraphic>().Set(active ? PaletteRole.Accent : PaletteRole.Foreground, false);
                    break;
                case Kind.Bar:
                    int v = getBar != null ? getBar() : 0;
                    for (int i = 0; i < blocks.Length; i++)
                    {
                        bool on = i < v;
                        blocks[i].sprite = on ? skin.pixel : skin.blockEmpty;
                        blocks[i].GetComponent<PaletteGraphic>().Set(on && active ? PaletteRole.Accent : PaletteRole.Foreground, !on);
                    }
                    break;
            }
        }
    }
}
