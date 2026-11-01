using System;
using System.IO;
using UnityEngine;

namespace AntiBacteria.Core
{
    /// <summary>
    /// Campaign progress, saved as versioned JSON in Application.persistentDataPath/save.json
    /// (Steam Cloud can sync that file as-is). Replaces the 2013 machine-locked encrypted defaults.
    /// Rules (2013 bug fixed): level 1 is open; level N+1 opens when N is won on any difficulty.
    /// Best score and "cleared" are kept per difficulty.
    /// </summary>
    public static class Progress
    {
        public const int Version = 1;
        public const int LevelCount = 50;
        public const int DifficultyCount = 3;

        [Serializable]
        private class SaveData
        {
            public int version = Version;
            public CampaignData campaign = new CampaignData();
        }

        [Serializable]
        private class CampaignData
        {
            // index = (level - 1) * 3 + difficulty
            public bool[] cleared = new bool[LevelCount * DifficultyCount];
            public int[] best = new int[LevelCount * DifficultyCount];
        }

        private static SaveData data;

        public static event Action Changed;

        public static string FilePath => Path.Combine(Application.persistentDataPath, "save.json");

        private static SaveData Data
        {
            get
            {
                if (data == null) Load();
                return data;
            }
        }

        public static bool IsUnlocked(int level) =>
            level == 1 || (level >= 2 && level <= LevelCount && IsClearedOnAny(level - 1));

        public static bool IsCleared(int level, Difficulty d) => InRange(level) && Data.campaign.cleared[Index(level, d)];

        public static bool IsClearedOnAny(int level)
        {
            for (int d = 0; d < DifficultyCount; d++)
                if (IsCleared(level, (Difficulty)d)) return true;
            return false;
        }

        public static int Best(int level, Difficulty d) => InRange(level) ? Data.campaign.best[Index(level, d)] : 0;

        /// <summary>The level the player most likely wants next: the first unlocked level not yet cleared.</summary>
        public static int NextToPlay()
        {
            for (int n = 1; n <= LevelCount; n++)
                if (IsUnlocked(n) && !IsClearedOnAny(n)) return n;
            return LevelCount;
        }

        /// <summary>Called when a level is won. Returns true if the score is a new best for that difficulty.</summary>
        public static bool RecordWin(int level, Difficulty d, int score)
        {
            if (!InRange(level)) return false;
            int i = Index(level, d);
            bool newBest = score > Data.campaign.best[i];
            Data.campaign.cleared[i] = true;
            if (newBest) Data.campaign.best[i] = score;
            Save();
            return newBest;
        }

        public static void Load()
        {
            data = null;
            try
            {
                if (File.Exists(FilePath)) data = JsonUtility.FromJson<SaveData>(File.ReadAllText(FilePath));
            }
            catch (Exception e)
            {
                Debug.LogWarning("Progress: could not read save, starting fresh. " + e.Message);
            }
            if (data == null) data = new SaveData();
            // Older or damaged files: make sure the arrays have the right size.
            if (data.campaign == null) data.campaign = new CampaignData();
            Array.Resize(ref data.campaign.cleared, LevelCount * DifficultyCount);
            Array.Resize(ref data.campaign.best, LevelCount * DifficultyCount);
            data.version = Version;
        }

        public static void Save()
        {
            try
            {
                string tmp = FilePath + ".tmp";
                File.WriteAllText(tmp, JsonUtility.ToJson(Data, true));
                if (File.Exists(FilePath)) File.Delete(FilePath);
                File.Move(tmp, FilePath); // write-then-rename, so a crash never leaves half a file
            }
            catch (Exception e)
            {
                Debug.LogError("Progress: could not save. " + e.Message);
            }
            Changed?.Invoke();
        }

        /// <summary>Dev / options use: wipe all progress.</summary>
        public static void ResetAll()
        {
            data = new SaveData();
            Save();
        }

        private static bool InRange(int level) => level >= 1 && level <= LevelCount;

        private static int Index(int level, Difficulty d) => (level - 1) * DifficultyCount + Mathf.Clamp((int)d, 0, DifficultyCount - 1);
    }
}
