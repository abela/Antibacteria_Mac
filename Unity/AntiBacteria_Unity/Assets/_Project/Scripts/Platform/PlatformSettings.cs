using System;
using UnityEngine;

namespace AntiBacteria.Platform
{
    /// <summary>Achievements the game can unlock. API names are set per store in PlatformSettings.</summary>
    public enum Achievement
    {
        Boss10, Boss20, Boss30, Boss40, FinishGame,
        ClearHard, ClearImpossible,
        FirstUpgrade, AllUpgrades, VitaminHoarder,
        FlagRunner, ZoneRunner, Pacifist,
    }

    /// <summary>One leaderboard for the campaign (sum of best scores) and one per endless mode.</summary>
    public enum Board { Campaign, CaptureFlag, Rush, PeaceWalker }

    [Serializable]
    public class AchievementEntry
    {
        public Achievement id;
        [Tooltip("The API name set up in the Steamworks partner site (Stats & Achievements).")]
        public string steamApiName;
        [Tooltip("Shown here only, to remember what unlocks it.")]
        public string note;
    }

    [Serializable]
    public class BoardEntry
    {
        public Board id;
        [Tooltip("Leaderboard name on Steam. Created on first use if it doesn't exist (sorted high to low).")]
        public string steamName;
    }

    /// <summary>
    /// Store settings: Steam App ID, Steam Cloud, achievement and leaderboard names, and the thresholds of the
    /// run-based achievements. Edit in Project Settings > AntiBacteria > Platform (Steam).
    /// </summary>
    [CreateAssetMenu(menuName = "AntiBacteria/Platform Settings", fileName = "PlatformSettings")]
    public class PlatformSettings : ScriptableObject
    {
        public const string ResourcePath = "PlatformSettings";

        [Header("Steam")]
        [Tooltip("480 is Valve's test app (Spacewar). Replace with the game's App ID once it exists.")]
        public uint steamAppId = 480;
        [Tooltip("Builds started outside Steam relaunch through Steam. Leave off while testing with App ID 480.")]
        public bool restartThroughSteam;
        [Tooltip("Keep save.json in Steam Cloud too; the newer copy wins when the game starts. " +
                 "Don't also turn on Steam Auto-Cloud for the same file.")]
        public bool useSteamCloud = true;

        [Header("Achievements")]
        public AchievementEntry[] achievements =
        {
            new AchievementEntry { id = Achievement.Boss10, steamApiName = "ACH_BOSS_10", note = "Beat the level 10 boss" },
            new AchievementEntry { id = Achievement.Boss20, steamApiName = "ACH_BOSS_20", note = "Beat the level 20 boss" },
            new AchievementEntry { id = Achievement.Boss30, steamApiName = "ACH_BOSS_30", note = "Beat the level 30 boss" },
            new AchievementEntry { id = Achievement.Boss40, steamApiName = "ACH_BOSS_40", note = "Beat the level 40 boss" },
            new AchievementEntry { id = Achievement.FinishGame, steamApiName = "ACH_FINISH", note = "Beat level 50" },
            new AchievementEntry { id = Achievement.ClearHard, steamApiName = "ACH_HARD", note = "Clear any level on Hard" },
            new AchievementEntry { id = Achievement.ClearImpossible, steamApiName = "ACH_IMPOSSIBLE", note = "Clear any level on Impossible" },
            new AchievementEntry { id = Achievement.FirstUpgrade, steamApiName = "ACH_FIRST_UPGRADE", note = "Buy an upgrade" },
            new AchievementEntry { id = Achievement.AllUpgrades, steamApiName = "ACH_ALL_UPGRADES", note = "Own every upgrade" },
            new AchievementEntry { id = Achievement.VitaminHoarder, steamApiName = "ACH_VITAMINS", note = "Hold vitaminHoard vitamins" },
            new AchievementEntry { id = Achievement.FlagRunner, steamApiName = "ACH_FLAGS", note = "flagRunnerCount flags in one Capture the Flag run" },
            new AchievementEntry { id = Achievement.ZoneRunner, steamApiName = "ACH_ZONES", note = "zoneRunnerCount zones in one Rush run" },
            new AchievementEntry { id = Achievement.Pacifist, steamApiName = "ACH_PEACE", note = "pacifistCount flags in one Peace Walker run" },
        };

        [Header("Achievement thresholds")]
        [Min(1)] public int vitaminHoard = 1000;
        [Min(1)] public int flagRunnerCount = 25;
        [Min(1)] public int zoneRunnerCount = 10;
        [Min(1)] public int pacifistCount = 10;

        [Header("Leaderboards")]
        public BoardEntry[] boards =
        {
            new BoardEntry { id = Board.Campaign, steamName = "Campaign" },
            new BoardEntry { id = Board.CaptureFlag, steamName = "Capture the Flag" },
            new BoardEntry { id = Board.Rush, steamName = "Rush" },
            new BoardEntry { id = Board.PeaceWalker, steamName = "Peace Walker" },
        };

        public string ApiName(Achievement a)
        {
            foreach (AchievementEntry e in achievements) if (e != null && e.id == a) return e.steamApiName;
            return null;
        }

        public string BoardName(Board b)
        {
            foreach (BoardEntry e in boards) if (e != null && e.id == b) return e.steamName;
            return null;
        }

        private static PlatformSettings current;

        public static PlatformSettings Current
        {
            get
            {
                if (current != null) return current;
                current = Resources.Load<PlatformSettings>(ResourcePath);
                if (current == null)
                {
                    current = CreateInstance<PlatformSettings>();
                    current.hideFlags = HideFlags.HideAndDontSave;
                }
                return current;
            }
        }

        public static void Use(PlatformSettings settings) => current = settings;
    }
}
