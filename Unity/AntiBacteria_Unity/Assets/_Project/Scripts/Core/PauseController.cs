using UnityEngine;

namespace AntiBacteria.Core
{
    /// <summary>
    /// Pause state (time scale 0). The pause menu (GameplayMenuController) decides when to pause and resume;
    /// this only owns the state so gameplay code can check it.
    /// </summary>
    public class PauseController : MonoBehaviour
    {
        public static bool IsPaused { get; private set; }

        /// <summary>Set when the level is over, so the end screen can't be paused (2013 bug: Esc there resumed the game).</summary>
        public static bool Locked { get; set; }

        /// <summary>
        /// True on the frame the game resumes and the next one. The key or click that chose "Resume" is still down,
        /// so gameplay ignores it (Space is both Confirm and Slow Motion, the mouse button is both Click and Fire).
        /// </summary>
        public static bool JustResumed => Time.frameCount - lastResumeFrame <= 1;

        private static float scaleBeforePause = 1f;
        private static int lastResumeFrame = -10;

        public static void Pause()
        {
            if (IsPaused) return;
            scaleBeforePause = Time.timeScale;
            Time.timeScale = 0f;
            IsPaused = true;
        }

        public static void Resume()
        {
            if (!IsPaused) return;
            Time.timeScale = scaleBeforePause;
            IsPaused = false;
            lastResumeFrame = Time.frameCount;
        }

        private void OnDestroy()
        {
            if (IsPaused) Time.timeScale = scaleBeforePause;
            IsPaused = false;
            Locked = false;
        }
    }
}
