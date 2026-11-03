using System;
using System.Collections.Generic;
using GleyLocalization;
using TMPro;
using UnityEngine;

namespace AntiBacteria.Core
{
    /// <summary>
    /// Thin wrapper over Gley Localization. Gley's own Next/PreviousLanguage don't save the choice and only
    /// refresh active objects, so the game uses this: it saves, and raises Changed for every label.
    /// </summary>
    public static class Loc
    {
        /// <summary>Order of the Options > Language picker.</summary>
        public static readonly SupportedLanguages[] Languages =
        {
            SupportedLanguages.English, SupportedLanguages.French, SupportedLanguages.German, SupportedLanguages.Spanish,
            SupportedLanguages.Italian, SupportedLanguages.Portuguese, SupportedLanguages.Dutch, SupportedLanguages.Polish,
            SupportedLanguages.Czech, SupportedLanguages.Russian, SupportedLanguages.Georgian, SupportedLanguages.Japanese,
            SupportedLanguages.Korean, SupportedLanguages.ChineseSimplified, SupportedLanguages.ChineseTraditional,
        };

        // Japanese and both Chinese scripts share many characters but draw them differently: the current language's
        // pixel font goes first in the fallback list (FontSetup builds the fonts and the list).
        private static readonly Dictionary<SupportedLanguages, string> HanFont = new Dictionary<SupportedLanguages, string>
        {
            { SupportedLanguages.Japanese, "AB_Pixel_JA" },
            { SupportedLanguages.ChineseSimplified, "AB_Pixel_SC" },
            { SupportedLanguages.ChineseTraditional, "AB_Pixel_TC" },
        };
        private static bool fontsApplied;

        public static event Action Changed;

        public static string Get(WordIDs id)
        {
            if (!Application.isPlaying) return id.ToString();
            if (!fontsApplied) ApplyFonts();
            return LocalizationManager.Instance.GetText(id).Trim();
        }

        public static SupportedLanguages Current =>
            Application.isPlaying ? LocalizationManager.Instance.GetCurrentLanguage() : SupportedLanguages.English;

        public static void Step(int direction)
        {
            int i = Array.IndexOf(Languages, Current);
            if (i < 0) i = 0;
            i = ((i + direction) % Languages.Length + Languages.Length) % Languages.Length;
            LocalizationManager.Instance.SetCurrentLanguage(Languages[i]);
            ApplyFonts();
            Changed?.Invoke();
        }

        /// <summary>Put the current language's Han font first among the default font's fallbacks.</summary>
        public static void ApplyFonts()
        {
            fontsApplied = true;
            TMP_FontAsset primary = TMP_Settings.defaultFontAsset;
            List<TMP_FontAsset> list = primary != null ? primary.fallbackFontAssetTable : null;
            if (list == null || !HanFont.TryGetValue(Current, out string wanted)) return;
            int i = list.FindIndex(f => f != null && f.name == wanted);
            if (i <= 0) return;
            TMP_FontAsset font = list[i];
            list.RemoveAt(i);
            list.Insert(0, font);
        }
    }
}
