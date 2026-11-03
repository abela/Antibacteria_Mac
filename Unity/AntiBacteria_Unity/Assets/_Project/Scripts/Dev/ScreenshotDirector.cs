#if UNITY_EDITOR
using AntiBacteria.Gameplay;
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
        /// <summary>Move the player on its own: circle the arena and keep away from enemies and their bullets.</summary>
        public static bool AutoMove;
        /// <summary>Auto-move: how far from the arena centre to circle (0..1 of the smaller half-size).</summary>
        public static float OrbitRadius = 0.45f;
        /// <summary>Auto-move: keep at least this far (world units) from enemies and bullets.</summary>
        public static float Personal = 40f;

        private int orbitDir = 1;

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
            GameInput.EditorMoveOverride = null;
            PixelCursor.EditorTestPosition = null;
            PlayerController.EditorInvincible = false;
            AutoMove = false;
            AntiBacteria.Platform.PlatformServices.EditorMuted = false;
            AimAt = null;
        }

        private void Update()
        {
            var player = FindFirstObjectByType<PlayerController>();
            Camera cam = Camera.main;
            if (player == null || cam == null) { GameInput.EditorMouseOverride = null; GameInput.EditorMoveOverride = null; PixelCursor.EditorTestPosition = null; return; }
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
            GameInput.EditorMoveOverride = AutoMove ? Dodge(from) : (Vector2?)null;
        }

        /// <summary>Circle the arena centre, pushed away from anything dangerous nearby.</summary>
        private Vector2 Dodge(Vector2 from)
        {
            Arena arena = Arena.Current;
            Rect r = arena != null ? arena.Bounds : new Rect(from - Vector2.one * 100f, Vector2.one * 200f);
            Vector2 c = r.center;
            float radius = Mathf.Min(r.width, r.height) * 0.5f * OrbitRadius;
            Vector2 rel = from - c;
            float ang = Mathf.Atan2(rel.y, rel.x);
            float ahead = ang + orbitDir * 0.6f;
            Vector2 goal = c + new Vector2(Mathf.Cos(ahead), Mathf.Sin(ahead)) * radius;
            Vector2 move = (goal - from).normalized;

            Vector2 push = Vector2.zero;
            foreach (Enemy e in FindObjectsByType<Enemy>(FindObjectsSortMode.None))
                push += Away(from, e.transform.position, Personal);
            foreach (EnemyBullet b in FindObjectsByType<EnemyBullet>(FindObjectsSortMode.None))
                push += Away(from, b.transform.position, Personal * 0.6f) * 1.5f;
            if (push.sqrMagnitude > 4f && Vector2.Dot(push, move) < -0.5f * push.magnitude) orbitDir = -orbitDir;
            return Vector2.ClampMagnitude(move + push, 1f);
        }

        private static Vector2 Away(Vector2 from, Vector2 other, float range)
        {
            Vector2 d = from - other;
            float m = d.magnitude;
            if (m >= range || m < 0.001f) return Vector2.zero;
            return d / m * (1f - m / range) * 2f;
        }
    }
}
#endif
