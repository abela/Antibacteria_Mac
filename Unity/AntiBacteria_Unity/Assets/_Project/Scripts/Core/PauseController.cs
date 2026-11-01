using AntiBacteria.Input;
using UnityEngine;

namespace AntiBacteria.Core
{
    /// <summary>Esc / P / Start toggles pause (time scale 0). A pause menu comes with the UI work.</summary>
    public class PauseController : MonoBehaviour
    {
        public static bool IsPaused { get; private set; }

        private float scaleBeforePause = 1f;

        private void Update()
        {
            if (!GameInput.PausePressed) return;
            if (IsPaused)
            {
                Time.timeScale = scaleBeforePause;
                IsPaused = false;
            }
            else
            {
                scaleBeforePause = Time.timeScale;
                Time.timeScale = 0f;
                IsPaused = true;
            }
        }

        private void OnDestroy()
        {
            if (IsPaused) Time.timeScale = scaleBeforePause;
            IsPaused = false;
        }
    }
}
