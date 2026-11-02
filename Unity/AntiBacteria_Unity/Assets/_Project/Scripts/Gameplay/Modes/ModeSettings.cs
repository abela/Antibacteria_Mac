using UnityEngine;

namespace AntiBacteria.Gameplay.Modes
{
    /// <summary>
    /// Numbers for the three endless modes (Docs/DesignReference/04, sections 2 and 4-7). Edit in
    /// Project Settings > AntiBacteria > Game Modes. 2013 counted in frames ("units" of 100 frames = 1.67 s at 60 fps);
    /// here everything is seconds of game time. Distances in points (80 pt = 16 game pixels).
    /// </summary>
    [CreateAssetMenu(menuName = "AntiBacteria/Mode Settings", fileName = "ModeSettings")]
    public class ModeSettings : ScriptableObject
    {
        public const string ResourcePath = "ModeSettings";

        [Header("Capture the Flag and Rush: enemies")]
        [Tooltip("Enemy IDs in unlock order (GameMode.mm). Only the first is used at first; every few objectives unlock the next.")]
        public int[] enemyPool = { 0, 1, 2, 11, 9, 5, 6, 8, 7, 0, 13, 18, 20, 18, 21, 10, 14, 15, 19 };
        [Tooltip("Seconds between waves at the start (2013: 1.5 units = 2.5 s).")]
        public float waveIntervalSeconds = 2.5f;
        [Tooltip("Never faster than this (2013: 0.7 units = 1.17 s).")]
        public float minWaveIntervalSeconds = 1.17f;
        [Tooltip("Each speed-up takes this much off the interval (2013: 0.1 units = 0.17 s).")]
        public float waveIntervalStepSeconds = 0.17f;
        [Tooltip("Never more enemies per wave than this (2013 bug 29: no limit).")]
        [Min(1)] public int maxEnemiesPerWave = 6;

        [Header("Capture the Flag and Rush: ramp (counted per objective that appears)")]
        [Tooltip("Every Nth objective unlocks the next enemy in the pool.")]
        [Min(1)] public int unlockEvery = 4;
        [Tooltip("Every Nth objective: waves come faster and bring one more enemy.")]
        [Min(1)] public int speedUpEvery = 6;
        [Tooltip("Every Nth objective: one more bullet per volley (up to the Weapons maximum) and a bonus appears.")]
        [Min(1)] public int levelUpEvery = 8;
        [Tooltip("The next objective appears this long after the last one is done (2013: 1 unit = 1.67 s).")]
        public float objectiveDelaySeconds = 1.67f;

        [Header("Capture the Flag")]
        [Tooltip("Points per flag before the difficulty multiplier (2013: 100).")]
        public int flagPoints = 100;

        [Header("Rush")]
        [Tooltip("Points per completed zone (2013: 250; bug 50 also paid an unfinished zone at death).")]
        public int zonePoints = 250;
        [Tooltip("Zone radius (2013: 160 pt).")]
        public float zoneRadiusPoints = 160f;
        [Tooltip("Seconds inside to fill a zone (2013: 200 frames = 3.33 s). Outside, it drains at the same rate.")]
        public float zoneFillSeconds = 3.33f;
        [Tooltip("Zones appear this far from the centre of the screen (2013: 500-520 pt).")]
        public float zoneDistanceMinPoints = 500f;
        public float zoneDistanceMaxPoints = 520f;

        [Header("Peace Walker (no shooting)")]
        [Tooltip("Seconds to reach each flag (2013 showed 10 but each 'second' was 100 frames, 16.7 s at 60 fps).")]
        public float secondsPerFlag = 15f;
        [Tooltip("Tentacles at the start (2013: 2); one more joins with every flag.")]
        public int startTentacles = 2;
        public int tentacleId = 17;
        [Tooltip("Points per flag (2013: 100).")]
        public int peaceFlagPoints = 100;
        [Tooltip("Vitamins dropped near the centre with every flag after the first (2013: 5).")]
        public int vitaminsPerFlag = 5;
        [Tooltip("Every Nth flag also brings a +1 Life bonus (2013: 8).")]
        [Min(1)] public int lifeEvery = 8;
        [Tooltip("Flags appear this far from the centre of the screen (2013: 500-520 pt).")]
        public float flagDistanceMinPoints = 500f;
        public float flagDistanceMaxPoints = 520f;

        [Header("Art (filled in automatically)")]
        public Material material;
        public Sprite flag;
        public Sprite peaceFlag;
        public Sprite dot;
        public Sprite dotFilled;

        private static ModeSettings current;

        public static ModeSettings Current
        {
            get
            {
                if (current != null) return current;
                current = Resources.Load<ModeSettings>(ResourcePath);
                if (current == null)
                {
                    current = CreateInstance<ModeSettings>();
                    current.hideFlags = HideFlags.HideAndDontSave;
                }
                return current;
            }
        }

        public static void Use(ModeSettings settings) => current = settings;
    }
}
