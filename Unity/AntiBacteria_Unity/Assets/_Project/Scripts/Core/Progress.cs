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
        public const int Version = 5; // 2: wallet and upgrades; 3: endless-mode best scores; 4: medals, mutations, strains, daily run; 5: levelled upgrades (2013 items refunded)
        public const int LevelCount = 50;
        public const int DifficultyCount = 3;

        [Serializable]
        private class SaveData
        {
            public int version = Version;
            public CampaignData campaign = new CampaignData();
            public WalletData wallet = new WalletData();
            public ModesData modes = new ModesData();
            public ReplayData replay = new ReplayData();
            public int plays; // levels / runs started, ever (first-time hints)
            public bool introSeen; // the opening story has played once
            public bool tutorialDone; // the first-play controls tutorial has been finished
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
            // Version 5: level of each upgrade, index = (int)CharacterUpgrade.
            public int[] upgradeLevels = new int[UpgradeCount];
            // Up to version 4: the 2013 items owned, index = (int)UpgradeItem. Refunded once, then cleared.
            public bool[] owned = new bool[LegacyItemCount];
            public int selectedShot;
            public bool doubleSpeedOn;
            public bool immortalityOn;
        }

        [Serializable]
        private class ReplayData
        {
            public int mutationUnlocked;      // highest roguelike Mutation that can be picked (0 = none yet)
            public int mutationSelected;
            public bool[] strains = new bool[Strains.Count]; // index = (int)StrainId; Penicillin is always open
            public int strainSelected;
            public string dailyDate = "";     // UTC day of the last scored Daily Run attempt
            public int dailyScore;            // that attempt's score
        }

        private static readonly int UpgradeCount = Enum.GetValues(typeof(CharacterUpgrade)).Length;
        private static readonly int LegacyItemCount = Enum.GetValues(typeof(UpgradeItem)).Length;
        private static bool dirty;

        [Serializable]
        private class CampaignData
        {
            // index = (level - 1) * 3 + difficulty
            public bool[] cleared = new bool[LevelCount * DifficultyCount];
            public int[] best = new int[LevelCount * DifficultyCount];
            public int[] medal = new int[LevelCount * DifficultyCount]; // (int)Medal, the best earned
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

        /// <summary>Levels bought of an upgrade (0 = not bought).</summary>
        public static int UpgradeLevel(CharacterUpgrade u)
        {
            int[] l = Data.wallet.upgradeLevels;
            return l != null && (int)u < l.Length ? l[(int)u] : 0;
        }

        /// <summary>Buy the next level if there is one and the wallet can pay. Saves straight away.</summary>
        public static bool TryBuyLevel(CharacterUpgrade u)
        {
            UpgradeDef def = ShopSettings.Current.Get(u);
            int level = UpgradeLevel(u);
            if (def == null || level >= def.MaxLevel) return false;
            int price = def.prices[level];
            if (Data.wallet.vitamins < price) return false;
            Data.wallet.vitamins -= price;
            Data.wallet.upgradeLevels[(int)u] = level + 1;
            Save();
            int bought = 0;
            foreach (int n in Data.wallet.upgradeLevels) bought += n;
            AchievementTracker.UpgradeBought(bought, ShopSettings.Current.TotalLevels);
            return true;
        }

        /// <summary>
        /// Version 5 replaced the 2013 shop (shots, Double Speed, Immortality) with levelled upgrades: whatever an old
        /// save owned is paid back in full, so nobody loses what they earned. Returns the vitamins refunded.
        /// </summary>
        private static int RefundLegacyItems(SaveData d)
        {
            if (d.wallet.owned == null) return 0;
            int[] prices = ShopSettings.Current.legacyPrices;
            int refund = 0;
            for (int i = 0; i < d.wallet.owned.Length; i++)
                if (d.wallet.owned[i]) refund += prices != null && i < prices.Length ? prices[i] : 0;
            d.wallet.vitamins += refund;
            d.wallet.owned = new bool[LegacyItemCount];
            d.wallet.selectedShot = 0;
            d.wallet.doubleSpeedOn = d.wallet.immortalityOn = false;
            return refund;
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

        public static Medal GetMedal(int level, Difficulty d) => InRange(level) ? (Medal)Data.campaign.medal[Index(level, d)] : Medal.None;

        /// <summary>The best medal of a level on any difficulty.</summary>
        public static Medal BestMedal(int level)
        {
            Medal best = Medal.None;
            for (int d = 0; d < DifficultyCount; d++)
            {
                Medal m = GetMedal(level, (Difficulty)d);
                if (m > best) best = m;
            }
            return best;
        }

        /// <summary>Keeps the better medal. True (and saved) when it improved.</summary>
        public static bool RecordMedal(int level, Difficulty d, Medal m)
        {
            if (!InRange(level)) return false;
            int i = Index(level, d);
            if ((int)m <= Data.campaign.medal[i]) return false;
            Data.campaign.medal[i] = (int)m;
            Save();
            return true;
        }

        /// <summary>Gold medals on every level and difficulty (each difficulty counts).</summary>
        public static int GoldMedalCount()
        {
            int n = 0;
            foreach (int m in Data.campaign.medal) if (m == (int)Medal.Gold) n++;
            return n;
        }

        /// <summary>Stars in one world: each level's best medal on any difficulty, 3 for gold (at most 30).</summary>
        public static int WorldStars(int world)
        {
            int n = 0, first = Worlds.FirstLevel(world);
            for (int level = first; level < first + Worlds.LevelsPerWorld; level++) n += Medals.StarCount(BestMedal(level));
            return n;
        }

        /// <summary>Gold medals in one world, all difficulties.</summary>
        public static int GoldMedals(int world)
        {
            int n = 0, first = Worlds.FirstLevel(world);
            for (int level = first; level < first + Worlds.LevelsPerWorld; level++)
                for (int d = 0; d < DifficultyCount; d++)
                    if (GetMedal(level, (Difficulty)d) == Medal.Gold) n++;
            return n;
        }

        // ------------------------------------------------------------------ roguelike mutations and strains, daily run

        public static int MutationUnlocked => Mathf.Clamp(Data.replay.mutationUnlocked, 0, ReplaySettings.MaxMutation);

        public static int SelectedMutation
        {
            get => Mathf.Clamp(Data.replay.mutationSelected, 0, MutationUnlocked);
            set { Data.replay.mutationSelected = Mathf.Clamp(value, 0, MutationUnlocked); Save(); }
        }

        /// <summary>Opens a Mutation level. True (and saved) when it is new.</summary>
        public static bool UnlockMutation(int level)
        {
            level = Mathf.Clamp(level, 0, ReplaySettings.MaxMutation);
            if (level <= Data.replay.mutationUnlocked) return false;
            Data.replay.mutationUnlocked = level;
            Save();
            return true;
        }

        public static bool IsStrainUnlocked(StrainId id) => id == StrainId.Penicillin || Data.replay.strains[(int)id];

        /// <summary>True (and saved) when the strain was still locked.</summary>
        public static bool UnlockStrain(StrainId id)
        {
            if (IsStrainUnlocked(id)) return false;
            Data.replay.strains[(int)id] = true;
            Save();
            return true;
        }

        public static StrainId SelectedStrain
        {
            get
            {
                var id = (StrainId)Mathf.Clamp(Data.replay.strainSelected, 0, Strains.Count - 1);
                return IsStrainUnlocked(id) ? id : StrainId.Penicillin;
            }
            set { Data.replay.strainSelected = (int)value; Save(); }
        }

        /// <summary>UTC day ("2026-11-05") of the last scored Daily Run attempt.</summary>
        public static string DailyDate => Data.replay.dailyDate ?? "";
        public static int DailyScore => Data.replay.dailyScore;

        /// <summary>The scored attempt of the day starts: used up straight away (quitting doesn't give it back).</summary>
        public static void StartDaily(string date)
        {
            Data.replay.dailyDate = date;
            Data.replay.dailyScore = 0;
            Save();
        }

        /// <summary>The scored attempt ended with this score. True when it is above zero (and saved).</summary>
        public static bool RecordDailyScore(int score)
        {
            Data.replay.dailyScore = Mathf.Max(Data.replay.dailyScore, score);
            Save();
            return score > 0;
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

        /// <summary>The controls tutorial (move, shoot, slow motion) has been finished once.</summary>
        public static bool TutorialDone => Data.tutorialDone;

        public static void MarkTutorialDone()
        {
            if (Data.tutorialDone) return;
            Data.tutorialDone = true;
            Save();
        }

        /// <summary>Editor / debug: the tutorial shows again in the next level.</summary>
        public static void ResetTutorial()
        {
            Data.tutorialDone = false;
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
            if (data.campaign.medal == null) data.campaign.medal = new int[LevelCount * DifficultyCount]; // version 3 had no medals
            Array.Resize(ref data.campaign.medal, LevelCount * DifficultyCount);
            if (data.replay == null) data.replay = new ReplayData();
            if (data.replay.strains == null) data.replay.strains = new bool[Strains.Count];
            Array.Resize(ref data.replay.strains, Strains.Count);
            if (data.replay.dailyDate == null) data.replay.dailyDate = "";
            if (data.wallet == null) data.wallet = new WalletData(); // version 1 saves had no wallet
            if (data.wallet.owned == null) data.wallet.owned = new bool[LegacyItemCount];
            Array.Resize(ref data.wallet.owned, LegacyItemCount);
            if (data.wallet.upgradeLevels == null) data.wallet.upgradeLevels = new int[UpgradeCount];
            Array.Resize(ref data.wallet.upgradeLevels, UpgradeCount);
            if (data.version < 5)
            {
                int refund = RefundLegacyItems(data);
                if (refund > 0) { dirty = true; Debug.Log("Progress: the 2013 upgrades were refunded (" + refund + " vitamins)."); }
            }
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
