using AntiBacteria.Core;
using AntiBacteria.Gameplay.Player;
using UnityEngine;

namespace AntiBacteria.Gameplay
{
    /// <summary>
    /// Follows the player, stays inside the arena, and snaps to whole pixels so pixel art never shimmers.
    /// Original: 80 % toward the player every frame and no clamp, so the void beyond the arena was visible
    /// (Docs 01, section 11). Here: time-based smoothing and clamped to the arena.
    /// </summary>
    [DefaultExecutionOrder(100)]
    [RequireComponent(typeof(Camera))]
    public class CameraFollow : MonoBehaviour
    {
        [Tooltip("Higher = tighter follow. 96 would match the original's per-frame lerp at 60 fps.")]
        [SerializeField] private float sharpness = 20f;

        [SerializeField] private bool clampToArena = true;

        private Camera cam;
        private Vector2 smoothed;
        private bool initialised;

        private UnityEngine.Rendering.Universal.PixelPerfectCamera ppc;
        private int viewVersion = -1;

        // Juice (numbers from Effects.ShotFeel): a kick that springs back, and a decaying random shake.
        private const float MaxKickPixels = 4f;
        private static CameraFollow active;
        private Vector2 kick, shakeOffset;
        private float shakePixels, shakeLeft, shakeDuration;

        /// <summary>Push the camera by this many pixels; it springs back (every shot pushes away from the aim).</summary>
        public static void Kick(Vector2 pixels)
        {
            if (active == null) return;
            float scale = Effects.ShotFeel.Current.screenShakeScale;
            active.kick = Vector2.ClampMagnitude(active.kick + pixels * scale / Legacy.PixelsPerUnit,
                MaxKickPixels / Legacy.PixelsPerUnit);
        }

        /// <summary>Shake the screen. A weaker shake never cuts a stronger one short.</summary>
        public static void Shake(float pixels, float seconds)
        {
            if (active == null || seconds <= 0f) return;
            float amp = pixels * Effects.ShotFeel.Current.screenShakeScale;
            float current = active.shakeLeft > 0f ? active.shakePixels * active.shakeLeft / active.shakeDuration : 0f;
            if (amp <= current) return;
            active.shakePixels = amp;
            active.shakeLeft = active.shakeDuration = seconds;
        }

        private void OnEnable() => active = this;
        private void OnDisable() { if (active == this) active = null; }

        private void Awake()
        {
            cam = GetComponent<Camera>();
            ppc = GetComponent<UnityEngine.Rendering.Universal.PixelPerfectCamera>();
            SyncView();
        }

        /// <summary>How much of the arena you see comes from Project Settings > AntiBacteria > View (PixelScreen).</summary>
        private void SyncView()
        {
            viewVersion = PixelScreen.Version;
            if (ppc == null) return;
            ppc.refResolutionX = PixelScreen.ViewWidth;
            ppc.refResolutionY = PixelScreen.ViewHeight;
        }

        private void LateUpdate()
        {
            if (viewVersion != PixelScreen.Version) SyncView(); // the setting was changed while playing
            PlayerController player = PlayerController.Current;
            if (player == null) return;

            Vector2 target = player.transform.position;
            if (!initialised) { smoothed = target; initialised = true; }
            smoothed = Vector2.Lerp(smoothed, target, 1f - Mathf.Exp(-sharpness * Time.unscaledDeltaTime));

            Vector2 pos = smoothed;
            if (clampToArena && Arena.Current != null)
            {
                Rect r = Arena.Current.Bounds;
                float halfH = cam.orthographicSize, halfW = halfH * cam.aspect;
                const float wallMargin = 0.5f; // show a little of the membrane wall
                pos.x = r.width + 2f * wallMargin <= 2f * halfW ? r.center.x : Mathf.Clamp(pos.x, r.xMin - wallMargin + halfW, r.xMax + wallMargin - halfW);
                pos.y = r.height + 2f * wallMargin <= 2f * halfH ? r.center.y : Mathf.Clamp(pos.y, r.yMin - wallMargin + halfH, r.yMax + wallMargin - halfH);
            }

            // juice: kick + shake, applied after the clamp so they show at the walls too
            float dt = Time.deltaTime;
            if (dt > 0f) // frozen while paused
            {
                kick *= Mathf.Exp(-Effects.ShotFeel.Current.cameraKickReturn * dt);
                shakeOffset = Vector2.zero;
                if (shakeLeft > 0f)
                {
                    shakeLeft -= dt;
                    float amp = shakePixels * Mathf.Clamp01(shakeLeft / shakeDuration) / Legacy.PixelsPerUnit;
                    shakeOffset = Random.insideUnitCircle * amp;
                }
            }
            pos += kick + shakeOffset;

            // snap to the pixel grid
            pos.x = Mathf.Round(pos.x * Legacy.PixelsPerUnit) / Legacy.PixelsPerUnit;
            pos.y = Mathf.Round(pos.y * Legacy.PixelsPerUnit) / Legacy.PixelsPerUnit;
            transform.position = new Vector3(pos.x, pos.y, transform.position.z);
        }
    }
}
