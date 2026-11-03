#if UNITY_EDITOR
using AntiBacteria.Gameplay.Enemies;
using AntiBacteria.Gameplay.Player;
using AntiBacteria.Input;
using AntiBacteria.UI.Menu;
using UnityEngine;

namespace AntiBacteria.Dev
{
    /// <summary>
    /// Editor only: plays the game hands-free for store screenshots. Aims (mouse and crosshair) at the nearest enemy,
    /// keeps the player alive, and mutes Steam so nothing reaches achievements, leaderboards or the cloud save.
    /// Start with ScreenshotDirector.Begin(), stop with End().
    /// </summary>
    [DefaultExecutionOrder(-500)] // before the player reads the mouse
    public class ScreenshotDirector : MonoBehaviour
    {
        private static ScreenshotDirector instance;

        /// <summary>Lead the target by this fraction of the way from the player (0 = dead on).</summary>
        public static float Lead = 0f;
        /// <summary>If set, aim here (world position) instead of at the nearest enemy.</summary>
        public static Vector2? AimAt;

        public static void Begin()
        {
            if (instance != null) return;
            var go = new GameObject("ScreenshotDirector");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<ScreenshotDirector>();
            PlayerController.EditorInvincible = true;
            AntiBacteria.Platform.PlatformServices.EditorMuted = true;
        }

        public static void End()
        {
            if (instance != null) Destroy(instance.gameObject);
            instance = null;
            GameInput.EditorMouseOverride = null;
            PixelCursor.EditorTestPosition = null;
            PlayerController.EditorInvincible = false;
            AntiBacteria.Platform.PlatformServices.EditorMuted = false;
            AimAt = null;
        }

        private void Update()
        {
            var player = FindFirstObjectByType<PlayerController>();
            Camera cam = Camera.main;
            if (player == null || cam == null) { GameInput.EditorMouseOverride = null; PixelCursor.EditorTestPosition = null; return; }
            Vector2 from = player.transform.position;
            Vector2 target = from + Vector2.up;
            if (AimAt is Vector2 fixedAim) target = fixedAim;
            else
            {
                float best = float.MaxValue;
                foreach (Enemy e in FindObjectsByType<Enemy>(FindObjectsSortMode.None))
                {
                    if (!e.isActiveAndEnabled) continue;
                    float d = ((Vector2)e.transform.position - from).sqrMagnitude;
                    if (d < best) { best = d; target = e.transform.position; }
                }
            }
            Vector2 screen = cam.WorldToScreenPoint(Vector2.Lerp(target, from, -Lead));
            GameInput.EditorMouseOverride = screen;
            PixelCursor.EditorTestPosition = screen;
        }
    }
}
#endif
