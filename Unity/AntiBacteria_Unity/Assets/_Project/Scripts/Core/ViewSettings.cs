using UnityEngine;

namespace AntiBacteria.Core
{
    /// <summary>
    /// How much of the arena the camera shows, in game pixels (the view at the reference zoom).
    /// Edit it in Project Settings > AntiBacteria > View (or AntiBacteria > Settings > View); the asset lives in
    /// Resources so builds read the same values. Changes apply immediately, also in play mode.
    /// </summary>
    [CreateAssetMenu(menuName = "AntiBacteria/View Settings", fileName = "ViewSettings")]
    public class ViewSettings : ScriptableObject
    {
        public const string ResourcePath = "ViewSettings";
        public const int DefaultWidth = 384, DefaultHeight = 216;
        public const int MinWidth = 160, MaxWidth = 640;
        public const int MinHeight = 90, MaxHeight = 360;
        /// <summary>With 16:9 kept, the width moves in 32 px steps so both sides stay whole, even numbers.</summary>
        public const int Step16x9 = 32;

        [Tooltip("Game pixels shown across. Smaller = camera closer, bigger sprites.")]
        [SerializeField] private int viewWidth = DefaultWidth;
        [Tooltip("Game pixels shown top to bottom.")]
        [SerializeField] private int viewHeight = DefaultHeight;
        [Tooltip("Keep the view 16:9 so 1080p and 4K screens show exactly this area.")]
        [SerializeField] private bool keep16by9 = true;

        public int ViewWidth => viewWidth;
        public int ViewHeight => viewHeight;
        public bool Keep16by9 => keep16by9;

        private static ViewSettings current;

        /// <summary>The project's settings asset, or the defaults if it is missing.</summary>
        public static ViewSettings Current
        {
            get
            {
                if (current != null) return current;
                current = Resources.Load<ViewSettings>(ResourcePath);
                if (current == null)
                {
                    current = CreateInstance<ViewSettings>();
                    current.hideFlags = HideFlags.HideAndDontSave;
                }
                return current;
            }
        }

        /// <summary>Editor: point at a newly created asset.</summary>
        public static void Use(ViewSettings settings)
        {
            current = settings;
            PixelScreen.NotifyChanged();
        }

        public void Set(int width, int height, bool lock16by9)
        {
            viewWidth = width;
            viewHeight = height;
            keep16by9 = lock16by9;
            Sanitize();
            PixelScreen.NotifyChanged();
        }

        private void OnValidate()
        {
            Sanitize();
            PixelScreen.NotifyChanged();
        }

        private void Sanitize()
        {
            if (keep16by9)
            {
                viewWidth = Mathf.Clamp(Mathf.RoundToInt(viewWidth / (float)Step16x9) * Step16x9, 192, MaxWidth);
                viewHeight = viewWidth * 9 / 16;
            }
            else
            {
                // Even sizes keep the 320x180 menu layout centred on whole pixels.
                viewWidth = Mathf.Clamp(viewWidth + (viewWidth & 1), MinWidth, MaxWidth);
                viewHeight = Mathf.Clamp(viewHeight + (viewHeight & 1), MinHeight, MaxHeight);
            }
        }
    }
}
