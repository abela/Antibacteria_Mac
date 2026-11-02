using UnityEngine;

namespace AntiBacteria.Gameplay.Ambience
{
    /// <summary>
    /// The life in the background: drifting specks, microbes swimming behind the action, pulse rings, the cilia on
    /// the walls, and the main menu backdrop. Pure decoration, no gameplay effect.
    /// Edit in Project Settings > AntiBacteria > Ambience. Distances are in game pixels, times in seconds.
    /// </summary>
    [CreateAssetMenu(menuName = "AntiBacteria/Ambience Settings", fileName = "AmbienceSettings")]
    public class AmbienceSettings : ScriptableObject
    {
        public const string ResourcePath = "AmbienceSettings";

        [Header("Arena: floor")]
        [Tooltip("Opacity of the dots and dotted circles drawn into the floor image (the walls stay solid).")]
        [Range(0f, 1f)] public float floorDetailAlpha = 0.25f;

        [Header("Arena: drifting specks")]
        public int specks = 160;
        [Tooltip("Share of the specks in the far layer: dimmer, and they lag behind the camera for depth.")]
        [Range(0f, 1f)] public float farShare = 0.55f;
        [Range(0f, 1f)] public float nearAlpha = 0.55f;
        [Range(0f, 1f)] public float farAlpha = 0.22f;
        [Tooltip("How much the far layer follows the camera (0 = fixed to the arena, like the near layer).")]
        [Range(0f, 0.9f)] public float farParallax = 0.35f;
        [Tooltip("Speed of the slow current that carries the specks.")]
        public float flowSpeed = 3f;
        [Tooltip("Chance per second that a speck blinks (twinkle).")]
        public float twinkleRate = 0.15f;

        [Header("Arena: reactions")]
        [Tooltip("Specks and microbes move out of the player's way within this radius.")]
        public float playerRepelRadius = 14f;
        public float playerRepelSpeed = 40f;
        [Tooltip("A kill pushes everything nearby outwards.")]
        public float killPushRadius = 48f;
        public float killPushSpeed = 70f;
        [Tooltip("How fast pushes die down (per second).")]
        public float damping = 3f;

        [Header("Arena: microbes behind the action")]
        public int microbes = 10;
        [Range(0f, 1f)] public float microbeAlpha = 0.28f;
        public float microbeSpeedMin = 3f;
        public float microbeSpeedMax = 7f;

        [Header("Arena: pulse rings")]
        public float pulseEveryMin = 3.5f;
        public float pulseEveryMax = 7f;
        public float pulseSeconds = 1.6f;
        public int pulseMaxRadius = 36;
        [Range(0f, 1f)] public float pulseAlpha = 0.35f;

        [Header("Arena: cilia on the walls")]
        public bool cilia = true;
        [Tooltip("Pixels between hairs.")]
        [Min(2)] public int ciliaSpacing = 2;
        [Tooltip("How fast the wave runs along the walls (radians per second).")]
        public float ciliaWaveSpeed = 3f;
        [Tooltip("Wave length along the wall, in pixels.")]
        public float ciliaWaveLength = 18f;
        [Range(1, 4)] public int ciliaMaxLength = 3;

        [Header("Main menu backdrop")]
        public int menuBacteria = 14;
        public int menuDust = 120;
        public int menuGhosts = 9;
        [Range(0f, 1f)] public float menuGhostAlpha = 0.3f;
        [Tooltip("A worm crawls across the menu every this many seconds (0 = never).")]
        public float menuCrawlerEvery = 14f;
        public float menuPulseEveryMin = 2.5f;
        public float menuPulseEveryMax = 5f;

        [Header("Art (filled in automatically)")]
        public Material material;

        private static AmbienceSettings current;

        public static AmbienceSettings Current
        {
            get
            {
                if (current != null) return current;
                current = Resources.Load<AmbienceSettings>(ResourcePath);
                if (current == null)
                {
                    current = CreateInstance<AmbienceSettings>();
                    current.hideFlags = HideFlags.HideAndDontSave;
                }
                return current;
            }
        }

        public static void Use(AmbienceSettings settings) => current = settings;

        /// <summary>Raised when a value is edited (editor), so the arena and the menu rebuild while playing.</summary>
        public static event System.Action Changed;

        private void OnValidate() => Changed?.Invoke();
    }
}
