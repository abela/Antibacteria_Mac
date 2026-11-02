using AntiBacteria.Core;
using AntiBacteria.Input;
using AntiBacteria.Rendering;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AntiBacteria.UI.Menu
{
    /// <summary>
    /// "[A] Select  [<>] Change  [B] Back" along the bottom edge of the screen while a menu is open. Shows only what
    /// the current screen allows, with the glyphs of the device in use (Xbox, PlayStation, Switch or keyboard),
    /// and redraws when the device, the language or the screen changes. One per menu canvas.
    /// </summary>
    public class PromptBar : MonoBehaviour
    {
        private const float GlyphGap = 3f, PromptGap = 10f, RowHeight = 16f;

        private class Slot
        {
            public Image glyph;
            public TextMeshProUGUI label;
        }

        private MenuNavigator navigator;
        private UISkin skin;
        private RectTransform bar;
        private readonly Slot[] slots = new Slot[3];
        private string shownKey;

        public static PromptBar Create(UISkin skin, RectTransform root, MenuNavigator navigator)
        {
            RectTransform bar = UIFactory.Rect(root, "Prompts", 160f, 0f, 320f, RowHeight, new Vector2(0.5f, 0f));
            var p = bar.gameObject.AddComponent<PromptBar>();
            p.navigator = navigator;
            p.skin = skin;
            p.bar = bar;
            for (int i = 0; i < p.slots.Length; i++)
            {
                p.slots[i] = new Slot
                {
                    glyph = UIFactory.Image(bar, "Glyph" + i, null, 0f, 2f, 11f, 11f, PaletteRole.Foreground),
                    label = UIFactory.Text(skin, bar, "Label" + i, 0f, 0f, HorizontalAlignmentOptions.Left, 1, PaletteRole.Foreground, true),
                };
            }
            // Bottom centre of the whole screen, whatever the aspect ratio.
            PinToScreenCorner.Pin(bar, new Vector2(0.5f, 0f), new Vector2(0f, 0f));
            bar.SetAsLastSibling();
            PromptSettings.Changed += p.ForceRedraw;
            Loc.Changed += p.ForceRedraw;
            p.Apply();
            return p;
        }

        private void OnDestroy()
        {
            PromptSettings.Changed -= ForceRedraw;
            Loc.Changed -= ForceRedraw;
        }

        private void ForceRedraw() => shownKey = null;

        private void LateUpdate() => Apply();

        private void Apply()
        {
            PromptSettings s = PromptSettings.Current;
            MenuScreen top = navigator != null ? navigator.Top : null;
            bool visible = s.show && top != null;
            MenuItem item = visible ? top.SelectedItem : null;
            bool confirm = item != null && item.Interactable;
            bool change = confirm && item.ChangesValue;
            bool back = visible && top.CanGoBack && (top.OnBack != null || navigator.Depth > 1);
            PromptFamily family = ControllerPrompts.Shown;
            if (Remapping.IsListening)
            {
                // Controls screen waiting for a key: only Esc does something (pads just time out).
                confirm = change = false;
                back = visible && family == PromptFamily.Keyboard;
            }

            string key = visible ? $"{family}|{confirm}|{change}|{back}" : "hidden";
            if (key == shownKey) return;
            shownKey = key;

            int n = 0;
            if (visible)
            {
                if (confirm) Set(n++, ConfirmGlyph(s, family), Loc.Get(WordIDs.Select));
                if (change) Set(n++, family == PromptFamily.Keyboard ? s.keyLeftRight : s.padLeftRight, Loc.Get(WordIDs.Change));
                if (back) Set(n++, BackGlyph(s, family), Loc.Get(WordIDs.Back));
            }
            for (int i = n; i < slots.Length; i++) { slots[i].glyph.enabled = false; slots[i].label.text = ""; }

            // Centre the row of prompts in the bar, on whole pixels.
            float total = 0f;
            for (int i = 0; i < n; i++) total += Width(i) + (i > 0 ? PromptGap : 0f);
            float x = Mathf.Floor(bar.rect.width * 0.5f - total * 0.5f);
            for (int i = 0; i < n; i++)
            {
                Slot sl = slots[i];
                float gw = sl.glyph.sprite != null ? sl.glyph.sprite.rect.width : 0f;
                sl.glyph.rectTransform.anchoredPosition = new Vector2(x, -2f); // centred on the capitals
                sl.glyph.rectTransform.sizeDelta = new Vector2(gw, 11f);
                sl.label.rectTransform.anchoredPosition = new Vector2(x + gw + GlyphGap, 0f);
                x += Width(i) + PromptGap;
            }
        }

        private void Set(int i, Sprite glyph, string text)
        {
            slots[i].glyph.sprite = glyph;
            slots[i].glyph.enabled = glyph != null;
            slots[i].label.text = text;
        }

        private float Width(int i)
        {
            Slot sl = slots[i];
            float gw = sl.glyph.sprite != null ? sl.glyph.sprite.rect.width + GlyphGap : 0f;
            return gw + UIFactory.Width(sl.label, sl.label.text);
        }

        private static Sprite ConfirmGlyph(PromptSettings s, PromptFamily f)
        {
            switch (f)
            {
                case PromptFamily.Xbox: return s.xboxA;
                case PromptFamily.PlayStation: return s.psCross;
                case PromptFamily.Switch: return s.nintendoConfirmOnRight ? s.switchA : s.switchB;
                default: return s.keyEnter;
            }
        }

        private static Sprite BackGlyph(PromptSettings s, PromptFamily f)
        {
            switch (f)
            {
                case PromptFamily.Xbox: return s.xboxB;
                case PromptFamily.PlayStation: return s.psCircle;
                case PromptFamily.Switch: return s.nintendoConfirmOnRight ? s.switchB : s.switchA;
                default: return s.keyEsc;
            }
        }
    }
}
