using UnityEngine;

namespace AntiBacteria.Gameplay.Effects
{
    /// <summary>
    /// Every "juice" number for shooting, hits, kills and the camera, in one asset.
    /// Edit it in Project Settings > AntiBacteria > Shooting Feel (or AntiBacteria > Settings > Shooting Feel).
    /// Lives in Resources so builds read the same values; changes apply immediately, also in play mode.
    /// Distances are in game pixels, speeds in pixels per second.
    /// </summary>
    [CreateAssetMenu(menuName = "AntiBacteria/Shooting Feel", fileName = "ShotFeel")]
    public class ShotFeel : ScriptableObject
    {
        public const string ResourcePath = "ShotFeel";

        [Header("Firing")]
        [Tooltip("Each shot leaves at a random angle within +- this many degrees of the aim.")]
        [Range(0f, 15f)] public float spreadDegrees = 4f;
        [Tooltip("Each shot's speed varies by up to +- this fraction.")]
        [Range(0f, 0.3f)] public float speedJitter = 0.06f;
        [Tooltip("Shots alternate between two barrels this far left and right of the aim line.")]
        [Range(0f, 4f)] public float barrelOffsetPixels = 1.5f;
        public bool muzzleFlash = true;
        [Tooltip("How far from the cell's centre the muzzle flash appears.")]
        [Range(4f, 16f)] public float muzzleDistancePixels = 11f;
        [Range(10f, 60f)] public float muzzleFlashFps = 30f;

        [Header("Recoil")]
        [Tooltip("The cell's sprite kicks back this far on every shot (purely visual).")]
        [Range(0f, 3f)] public float recoilPixels = 1f;
        [Range(0f, 0.2f)] public float recoilSeconds = 0.05f;
        [Tooltip("The camera is pushed away from the aim on every shot, then springs back.")]
        [Range(0f, 4f)] public float cameraKickPixels = 1.5f;
        [Tooltip("How fast the camera springs back (higher = snappier).")]
        [Range(5f, 60f)] public float cameraKickReturn = 25f;
        [Tooltip("Screen shake on every shot (0 = none; the kick is usually enough).")]
        [Range(0f, 3f)] public float fireShakePixels = 0f;

        [Header("Bullet")]
        [Tooltip("A new bullet shows as a round puff for this long before stretching into its pill shape.")]
        [Range(0f, 0.15f)] public float birthSeconds = 0.04f;
        [Tooltip("A faded streak behind each bullet.")]
        public bool trail = true;
        [Range(0f, 1f)] public float trailAlpha = 0.45f;

        [Header("Hits")]
        [Tooltip("Enemies are pushed back along the bullet's path (bosses and stationary enemies don't move).")]
        [Range(0f, 200f)] public float knockbackPixelsPerSecond = 30f;
        [Tooltip("How quickly the push fades (higher = shorter).")]
        [Range(2f, 40f)] public float knockbackDecay = 15f;
        [Range(0, 16)] public int enemyHitSparks = 4;
        [Range(0, 24)] public int wallHitSparks = 7;
        [Tooltip("Sparks when a shot cancels an enemy bullet.")]
        [Range(0, 16)] public int bulletCancelSparks = 4;

        [Header("Kills")]
        [Range(0, 40)] public int killSparks = 10;
        [Range(0f, 6f)] public float killShakePixels = 1.5f;
        [Range(0f, 0.5f)] public float killShakeSeconds = 0.12f;
        [Range(0, 120)] public int bossKillSparks = 48;
        [Range(0f, 10f)] public float bossKillShakePixels = 4f;
        [Range(0f, 1.5f)] public float bossKillShakeSeconds = 0.5f;

        [Header("Sparks (particles)")]
        [Range(10f, 400f)] public float sparkSpeedMin = 60f;
        [Range(10f, 400f)] public float sparkSpeedMax = 170f;
        [Tooltip("How quickly sparks slow down.")]
        [Range(0f, 20f)] public float sparkDrag = 8f;
        [Range(0.05f, 1f)] public float sparkLifeMin = 0.15f;
        [Range(0.05f, 1f)] public float sparkLifeMax = 0.35f;
        [Tooltip("Sparks blink for the last part of their life instead of fading (keeps the 3 colours pure).")]
        [Range(0f, 1f)] public float sparkBlinkFrom = 0.35f;

        [Header("Screen shake")]
        [Tooltip("Multiplies every kick and shake. 0 turns them all off.")]
        [Range(0f, 2f)] public float screenShakeScale = 1f;

        [Header("Art (filled in automatically)")]
        public Material material;
        public Sprite shotBirth;
        public Sprite shotTrail;
        public Sprite[] muzzleFrames = new Sprite[0];
        public Sprite[] hitFrames = new Sprite[0];
        public Sprite[] wallFrames = new Sprite[0];
        public Sprite sparkForeground;
        public Sprite sparkAccent;
        public Sprite sparkBig;

        private static ShotFeel current;

        public static ShotFeel Current
        {
            get
            {
                if (current != null) return current;
                current = Resources.Load<ShotFeel>(ResourcePath);
                if (current == null)
                {
                    current = CreateInstance<ShotFeel>();
                    current.hideFlags = HideFlags.HideAndDontSave;
                }
                return current;
            }
        }

        public static void Use(ShotFeel feel) => current = feel;

        private void OnValidate()
        {
            if (sparkSpeedMax < sparkSpeedMin) sparkSpeedMax = sparkSpeedMin;
            if (sparkLifeMax < sparkLifeMin) sparkLifeMax = sparkLifeMin;
        }
    }
}
