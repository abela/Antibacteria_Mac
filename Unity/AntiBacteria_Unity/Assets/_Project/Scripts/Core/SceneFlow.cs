using UnityEngine;
using UnityEngine.SceneManagement;

namespace AntiBacteria.Core
{
    /// <summary>Scene changes in one place, so time scale, pause and physics step are always reset.</summary>
    public static class SceneFlow
    {
        public const string MainMenuScene = "MainMenu";
        public const string GameplayScene = "Gameplay";

        public const int LevelCount = 50;

        public static void LoadMainMenu() => Load(MainMenuScene);

        /// <summary>Starts (or restarts) a campaign level, 1..50.</summary>
        public static void LoadLevel(int number)
        {
            GameSession.Mode = GameMode.Campaign;
            GameSession.CurrentLevel = Mathf.Clamp(number, 1, LevelCount);
            Load(GameplayScene);
        }

        /// <summary>Starts an endless mode at the difficulty chosen in the menu.</summary>
        public static void LoadMode(GameMode mode)
        {
            if (mode == GameMode.Campaign) { LoadLevel(GameSession.CurrentLevel); return; }
            GameSession.Mode = mode;
            GameSession.Difficulty = GameSettings.Difficulty;
            Load(GameplayScene);
        }

        /// <summary>Plays the same level, or the same endless mode, again.</summary>
        public static void RestartLevel()
        {
            if (GameSession.IsEndless) LoadMode(GameSession.Mode);
            else LoadLevel(GameSession.CurrentLevel);
        }

        public static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private static void Load(string scene)
        {
            Progress.SaveIfDirty(); // vitamins picked up this level
            PauseController.Resume();
            Time.timeScale = 1f;
            Time.fixedDeltaTime = 1f / 60f;
            SceneManager.LoadScene(scene);
        }
    }
}
