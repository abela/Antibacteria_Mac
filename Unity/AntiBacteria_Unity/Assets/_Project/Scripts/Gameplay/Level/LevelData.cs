using AntiBacteria.Core;
using UnityEngine;

namespace AntiBacteria.Gameplay.Level
{
    /// <summary>
    /// One campaign level, imported from the 2013 plists (AntiBacteria > Data > Import Levels).
    /// Field meanings: Docs/DesignReference/04, section 9.2.
    /// </summary>
    [CreateAssetMenu(menuName = "AntiBacteria/Level", fileName = "Level")]
    public class LevelData : ScriptableObject
    {
        public int world = 1;
        public int levelInWorld = 1;
        [Tooltip("Campaign number 1..50.")]
        public int number = 1;
        [Tooltip("Survive this long to win (ignored on boss levels).")]
        public float timeSeconds = 80f;
        [Tooltip("Seconds between enemy waves (original enemiescreatetime). Hard -0.5, Impossible -1.0.")]
        public float spawnIntervalSeconds = 2f;
        [Tooltip("Enemies per wave start at 1 and grow up to this.")]
        public int maxEnemiesPerWave = 3;
        [Tooltip("Enemies per wave +1 every this many seconds (original enemiesgrowinterval x 1.67 s; 0 = never).")]
        public float growIntervalSeconds = 16.67f;
        [Tooltip("Original enemy type IDs; picked uniformly, duplicates add weight.")]
        public int[] enemyPool = { 0 };

        public bool IsBoss => levelInWorld == 10;

        /// <summary>
        /// Seconds to survive, by world (1..5). The 2013 data says 80 s for every level, which leaves most levels
        /// at full intensity for half a minute or more: these keep each level short and rising, and later worlds a
        /// little longer. timeSeconds (80) is kept as the 2013 reference.
        /// </summary>
        public static readonly float[] WorldSeconds = { 45f, 50f, 55f, 60f, 65f };

        /// <summary>How long this level lasts.</summary>
        public float Seconds => WorldSeconds[Mathf.Clamp(world - 1, 0, WorldSeconds.Length - 1)];

        /// <summary>
        /// Seconds between "one more enemy per wave", squeezed with the level's length so each level still builds up to
        /// its full wave size at the same point of the clock as in 2013.
        /// </summary>
        public float GrowInterval => timeSeconds > 0f ? growIntervalSeconds * Seconds / timeSeconds : growIntervalSeconds;

        public string Label => world + "-" + levelInWorld;

        public float SpawnInterval(Difficulty difficulty)
        {
            float v = spawnIntervalSeconds - (difficulty == Difficulty.Hard ? 0.5f : difficulty == Difficulty.Impossible ? 1f : 0f);
            return Mathf.Max(0.25f, v); // original had no lower clamp (level 41 Impossible = 0.3 s)
        }
    }
}
