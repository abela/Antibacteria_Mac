using System;
using GleyLocalization;
using UnityEngine;

namespace AntiBacteria.Core
{
    /// <summary>
    /// Thin wrapper over Gley Localization. Gley's own Next/PreviousLanguage don't save the choice and only
    /// refresh active objects, so the game uses this: it saves, and raises Changed for every label.
    /// </summary>
    public static class Loc
    {
        public static readonly SupportedLanguages[] Languages =
        {
            SupportedLanguages.English, SupportedLanguages.French, SupportedLanguages.German,
            SupportedLanguages.Russian, SupportedLanguages.Spanish,
        };

        public static event Action Changed;

        public static string Get(WordIDs id)
        {
            if (!Application.isPlaying) return id.ToString();
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
            Changed?.Invoke();
        }
    }
}
