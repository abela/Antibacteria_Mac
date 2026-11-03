using System;
using System.IO;
using AntiBacteria.Gameplay.Weapons;
using AntiBacteria.Platform;
using UnityEngine;

namespace AntiBacteria.Core
{
    /// <summary>
    /// Campaign progress, saved as versioned JSON in Application.persistentDataPath/save.json
    /// (Steam Cloud can sync that file as-is). Replaces the 2013 machine-locked encrypted defaults.
    /// Rules (2013 bug fixed): level 1 is open; level N+1 opens when N is won on any difficulty.
    /// Best score and "cleared" are kept per difficulty.
    /// Also the vitamin wallet and the upgrade shop (2013 bug 47: a cached balance refunded spending and a store
    /// purchase replaced the balance; here there is one wallet, saved with everything else).
    /// </summary>
    public static class Progress
    {
        public const int Version = 3; // 2: wallet and upgrades; 3: endless-mode best scores
        public const int LevelCount = 50;
        public const int DifficultyCount = 3;

        [Serializable]
        private class SaveData
        {
            public int version = Version;
            public CampaignData campaign = new CampaignData();
            public WalletData wallet = new WalletData();
            public ModesData modes = new ModesData();
            public int plays; // levels / runs started, ever (first-time hints)
            public bool introSeen; // the opening story has played once
        }

        private const int EndlessModeCount = 4; // Capture the Flag, Rush, Peace Walker, Roguelike

        [Serializable]
        private class ModesData
        {
            // index = ((int)GameMode - 1) * 3 + difficulty
            public int[] best = new int[EndlessModeCount * DifficultyCount];
            public int[] roguelikeRoom = new int[DifficultyCount]; // furthest room reached per difficulty
        }

        [Serializable]
        private class WalletData
        {
            public int vitamins;
            // index = (int)UpgradeItem (2013 bug 48: explicit keys instead of an empty-string "Normal" row)
            public bool[] owned = new bool[UpgradeCount];
            public int selectedShot; // (int)ShotKind
            public bool doubleSpeedOn;
            public bool immortalityOn;
        }

        private static readonly int UpgradeCount = Enum.GetValues(typeof(UpgradeItem)).Length;
        private static bool dirty;

        [Serializable]
        private class CampaignData
        {
            // index = (level - 1) * 3 + difficulty
            public bool[] cleared = new bool[LevelCount * DifficultyCount];
            public int[] best = new int[LevelCount * DifficultyCount];
        }

        private static SaveData data;

        public static event Action Changed;

        private const string CloudFile = "save.json";

        public static string FilePath => Path.Combine(Application.persistentDataPath, "save.json");

        private static SaveData Data
        {
            get
            {
                if (data == null) Load();
                return data;
            }
        }

        // ------------------------------------------------------------------ wallet & upgrades

        public static int Vitamins => Data.wallet.vitamins;

        /// <summary>Vitamins picked up during play. Written to disk at the end of the level, on scene change and on quit.</summary>
        public static void AddVitamins(int amount)
        {
            Data.wallet.vitamins = Mathf.Max(0, Data.wallet.vitamins + amount);
            dirty = true;
            AchievementTracker.WalletChanged(Data.wallet.vitamins);
            Changed?.Invoke();
        }

        public static bool Owns(UpgradeItem item) => Data.wallet.owned[(int)item];

        /// <summary>Buy an upgrade if it isn't owned yet and the wallet can pay. Saves straight away.</summary>
        public static bool TryBuy(UpgradeItem item, int price)
        {
            if (Owns(item) || Data.wallet.vitamins < price) return false;
            Data.wallet.vitamins -= price;
            Data.wallet.owned[(int)item] = true;
            Save();
            int owned = 0;
            foreach (bool o in Data.wallet.owned) if (o) owned++;
            AchievementTracker.UpgradeBought(owned, UpgradeCount);
            return true;
        }

        /// <summary>The player's own shot. Falls back to Normal if the saved one isn't owned.</summary>
        public static ShotKind SelectedShot
        {
            get
            {
                var shot = (ShotKind)Data.wallet.selectedShot;
                UpgradeItem? item = ShopSettings.ItemFor(shot);
                return item.HasValue && !Owns(item.Value) ? ShotKind.Normal : shot;
            }
        }

        public static void SelectShot(ShotKind shot)
        {
            Data.wallet.selectedShot = (int)shot;
            Save();
        }

        public static bool DoubleSpeedOn => Owns(UpgradeItem.DoubleSpeed) && Data.wallet.doubleSpeedOn;
        public static bool ImmortalityOn => Owns(UpgradeItem.Immortality) && Data.wallet.immortalityOn;

        public static void SetDoubleSpeed(bool on)
        {
            Data.wallet.doubleSpeedOn = on;
            Save();
        }

        public static void SetImmortality(bool on)
        {
            Data.wallet.immortalityOn = on;
            Save();
        }

        public static void SaveIfDirty()
        {
            if (dirty) Save();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void SaveOnQuit() => Application.quitting += SaveIfDirty;

        // ------------------------------------------------------------------ campaign

        public static bool IsUnlocked(int level) =>
            level == 1 || (level >= 2 && level <= LevelCount && (UnlockAll || IsClearedOnAny(level - 1)));

#if UNITY_EDITOR
        private const string UnlockAllKey = "AntiBacteria.UnlockAllLevels";

        /// <summary>
        /// Editor-only testing switch: every level counts as unlocked. Kept in EditorPrefs, never in save.json,
        /// so switching it off returns to the real progress. Always false in builds.
        /// </summary>
        public static bool UnlockAll
        {
            get => UnityEditor.EditorPrefs.GetBool(UnlockAllKey, false);
            set
            {
                if (value == UnlockAll) return;
                UnityEditor.EditorPrefs.SetBool(UnlockAllKey, value);
                Changed?.Invoke();
            }
        }
#else
        public static bool UnlockAll => false;
#endif

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

        /// <summary>The opening story has played (first launch) - it doesn't play by itself again.</summary>
        public static bool IntroSeen => Data.introSeen;

        public static void MarkIntroSeen()
        {
            if (Data.introSeen) return;
            Data.introSeen = true;
            Save();
        }

        /// <summary>Editor / debug: the intro plays again on the next launch.</summary>
        public static void ResetIntro()
        {
            Data.introSeen = false;
            Save();
        }

        /// <summary>How many levels or endless runs have been started, ever (retries count).</summary>
        public static int Plays => Data.plays;

        /// <summary>Called when a level or run starts. Saved with the next save (end of level, scene change, quit).</summary>
        public static void CountPlay()
        {
            Data.plays++;
            dirty = true;
        }

        public static int ModeBest(GameMode mode, Difficulty d) =>
            mode == GameMode.Campaign ? 0 : Data.modes.best[ModeIndex(mode, d)];

        /// <summary>Called when an endless run ends. Returns true (and saves) on a new best for that mode and difficulty.</summary>
        public static bool RecordModeScore(GameMode mode, Difficulty d, int score)
        {
            if (mode == GameMode.Campaign) return false;
            int i = ModeIndex(mode, d);
            if (score <= Data.modes.best[i]) return false;
            Data.modes.best[i] = score;
            Save();
            return true;
        }

        /// <summary>Sum of the best score of every level on every difficulty (the campaign leaderboard).</summary>
        public static int CampaignTotal()
        {
            long total = 0;
            foreach (int b in Data.campaign.best) total += b;
            return (int)Math.Min(total, int.MaxValue);
        }

        public static int RoguelikeBestRoom(Difficulty d) => Data.modes.roguelikeRoom[(int)d];

        /// <summary>A roguelike run ended in this room. True (and saved) when it is the furthest yet.</summary>
        public static bool RecordRoguelikeRoom(Difficulty d, int room)
        {
            if (room <= Data.modes.roguelikeRoom[(int)d]) return false;
            Data.modes.roguelikeRoom[(int)d] = room;
            Save();
            return true;
        }

        private static int ModeIndex(GameMode mode, Difficulty d) => ((int)mode - 1) * DifficultyCount + (int)d;

        public static void Load()
        {
            data = null;
            try
            {
                string json = File.Exists(FilePath) ? File.ReadAllText(FilePath) : null;
                // Steam Cloud: take the cloud copy when there is no local file or the cloud one is newer.
                byte[] cloud = PlatformServices.CloudRead(CloudFile, out long cloudTime);
                if (cloud != null && (json == null || cloudTime > new DateTimeOffset(File.GetLastWriteTimeUtc(FilePath)).ToUnixTimeSeconds()))
                    json = System.Text.Encoding.UTF8.GetString(cloud);
                if (json != null) data = JsonUtility.FromJson<SaveData>(json);
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
            if (data.wallet == null) data.wallet = new WalletData(); // version 1 saves had no wallet
            if (data.wallet.owned == null) data.wallet.owned = new bool[UpgradeCount];
            Array.Resize(ref data.wallet.owned, UpgradeCount);
            if (data.modes == null) data.modes = new ModesData(); // version 2 saves had no mode scores
            if (data.modes.best == null) data.modes.best = new int[EndlessModeCount * DifficultyCount];
            Array.Resize(ref data.modes.best, EndlessModeCount * DifficultyCount);
            if (data.modes.roguelikeRoom == null) data.modes.roguelikeRoom = new int[DifficultyCount];
            Array.Resize(ref data.modes.roguelikeRoom, DifficultyCount);
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
                PlatformServices.CloudWrite(CloudFile, File.ReadAllBytes(FilePath));
                dirty = false;
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
