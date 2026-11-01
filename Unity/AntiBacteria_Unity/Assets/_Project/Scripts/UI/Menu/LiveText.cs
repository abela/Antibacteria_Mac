using System;
using AntiBacteria.Rendering;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AntiBacteria.UI.Menu
{
    /// <summary>
    /// A non-selectable label whose text comes from a function (localised titles, level names).
    /// Titles drawn at 2x drop to 1x when a translation would not fit maxWidth.
    /// </summary>
    public class LiveText : MonoBehaviour, IRefreshable
    {
        private TextMeshProUGUI text;
        private Func<string> source;
        private int baseSize;
        private float maxWidth;

        public TextMeshProUGUI Text => text;

        public static LiveText Create(UISkin skin, Transform parent, string name, float x, float y, Func<string> source,
            HorizontalAlignmentOptions align = HorizontalAlignmentOptions.Center, int scale = 1,
            PaletteRole role = PaletteRole.Foreground, bool dim = false, float maxWidth = 0f)
        {
            TextMeshProUGUI t = UIFactory.Text(skin, parent, name, x, y, align, scale, role, dim);
            var lt = t.gameObject.AddComponent<LiveText>();
            lt.text = t;
            lt.source = source;
            lt.baseSize = skin.fontSize * scale;
            lt.maxWidth = maxWidth;
            lt.Refresh();
            return lt;
        }

        public void Refresh()
        {
            if (text == null) return;
            string s = source != null ? source() : "";
            text.fontSize = baseSize;
            text.text = s;
            // Shrink by whole steps of 16 px so the font stays pixel-exact.
            while (maxWidth > 0f && text.fontSize > 16f && UIFactory.Width(text, s) > maxWidth) text.fontSize -= 16f;
        }
    }
}
