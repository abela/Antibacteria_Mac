using System;
using UnityEngine;

namespace AntiBacteria.Core
{
    /// <summary>
    /// Player options, saved in PlayerPrefs. (Language is saved by Gley Localization itself, see Loc.)
    /// Moves into the versioned save file when the save system is built.
    /// </summary>
    public static class GameSettings
    {
        private const string Prefix = "ab.settings.";

        public static Difficulty Difficulty { get; private set; } = Difficulty.Normal;
        public static bool AutoShoot { get; private set; }
        public static int PaletteIndex { get; private set; }
        public static bool Fullscreen { get; private set; } = true;
        /// <summary>0..10</summary>
        public static int SfxVolume { get; private set; } = 8;
        /// <summary>0..10</summary>
        public static int MusicVolume { get; private set; } = 6;

        public static event Action Changed;

        /// <summary>The Display option only exists where windowed mode makes sense.</summary>
        public static bool HasDisplayOption =>
            Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.OSXPlayer ||
            Application.platform == RuntimePlatform.LinuxPlayer || Application.isEditor;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Boot()
        {
            Load();
            ApplyDisplay();
        }

        public static void Load()
        {
            Difficulty = (Difficulty)Mathf.Clamp(PlayerPrefs.GetInt(Prefix + "difficulty", 0), 0, 2);
            AutoShoot = PlayerPrefs.GetInt(Prefix + "autoShoot", 0) == 1;
            PaletteIndex = PlayerPrefs.GetInt(Prefix + "palette", 0);
            Fullscreen = PlayerPrefs.GetInt(Prefix + "fullscreen", 1) == 1;
            SfxVolume = Mathf.Clamp(PlayerPrefs.GetInt(Prefix + "sfx", 8), 0, 10);
            MusicVolume = Mathf.Clamp(PlayerPrefs.GetInt(Prefix + "music", 6), 0, 10);
            GameSession.Difficulty = Difficulty;
        }

        public static void SetDifficulty(Difficulty d)
        {
            Difficulty = d;
            GameSession.Difficulty = d;
            Save("difficulty", (int)d);
        }

        public static void SetAutoShoot(bool on) { AutoShoot = on; Save("autoShoot", on ? 1 : 0); }

        public static void SetPaletteIndex(int index) { PaletteIndex = index; Save("palette", index); }

        public static void SetFullscreen(bool on)
        {
            Fullscreen = on;
            Save("fullscreen", on ? 1 : 0);
            ApplyDisplay();
        }

        public static void SetSfxVolume(int v) { SfxVolume = Mathf.Clamp(v, 0, 10); Save("sfx", SfxVolume); }

        public static void SetMusicVolume(int v) { MusicVolume = Mathf.Clamp(v, 0, 10); Save("music", MusicVolume); }

        private static void Save(string key, int value)
        {
            PlayerPrefs.SetInt(Prefix + key, value);
            PlayerPrefs.Save();
            Changed?.Invoke();
        }

        private static void ApplyDisplay()
        {
            if (Application.isEditor || !HasDisplayOption) return;
            if (Fullscreen)
            {
                Resolution r = Screen.currentResolution;
                Screen.SetResolution(r.width, r.height, FullScreenMode.FullScreenWindow);
            }
            else
            {
                Screen.SetResolution(1280, 720, FullScreenMode.Windowed); // 4x the 320x180 canvas
            }
        }
    }
}
