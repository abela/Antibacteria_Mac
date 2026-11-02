namespace AntiBacteria.Core
{
    /// <summary>
    /// The campaign's 5 worlds of 10 levels; the 10th level of each is its boss.
    /// Names are localisation words (World1..World5 in the Gley localisation file), boss names are proper nouns.
    /// </summary>
    public static class Worlds
    {
        public const int Count = 5;
        public const int LevelsPerWorld = 10;

        private static readonly WordIDs[] Names = { WordIDs.World1, WordIDs.World2, WordIDs.World3, WordIDs.World4, WordIDs.World5 };
        private static readonly string[] Bosses = { "Big Boss", "Inferno", "Big Red Noise", "Anakonda", "Frunctus" };

        public static int Of(int level) => (level - 1) / LevelsPerWorld + 1;
        public static int IndexInWorld(int level) => (level - 1) % LevelsPerWorld + 1;
        public static int FirstLevel(int world) => (world - 1) * LevelsPerWorld + 1;
        public static bool IsBossLevel(int level) => IndexInWorld(level) == LevelsPerWorld;

        /// <summary>"Agar Plains", in the current language.</summary>
        public static string Name(int world) => Loc.Get(Names[UnityEngine.Mathf.Clamp(world - 1, 0, Count - 1)]);

        public static string BossName(int world) => Bosses[UnityEngine.Mathf.Clamp(world - 1, 0, Count - 1)];

        /// <summary>A world opens with its first level (beat the previous boss).</summary>
        public static bool IsOpen(int world) => Progress.IsUnlocked(FirstLevel(world));

        /// <summary>Levels of the world cleared on any difficulty.</summary>
        public static int ClearedOnAny(int world)
        {
            int n = 0;
            for (int i = 0; i < LevelsPerWorld; i++) if (Progress.IsClearedOnAny(FirstLevel(world) + i)) n++;
            return n;
        }

        /// <summary>Levels of the world cleared on the given difficulty.</summary>
        public static int Cleared(int world, Difficulty d)
        {
            int n = 0;
            for (int i = 0; i < LevelsPerWorld; i++) if (Progress.IsCleared(FirstLevel(world) + i, d)) n++;
            return n;
        }
    }
}
