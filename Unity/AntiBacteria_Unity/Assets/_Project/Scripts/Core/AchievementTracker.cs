using AntiBacteria.Platform;

namespace AntiBacteria.Core
{
    /// <summary>
    /// Turns game events into achievements and leaderboard scores. Store-agnostic: PlatformServices does nothing
    /// when no store is running. API names and thresholds: Project Settings > AntiBacteria > Platform (Steam).
    /// </summary>
    public static class AchievementTracker
    {
        public static void LevelWon(int level, Difficulty d, bool bossLevel)
        {
            if (bossLevel)
            {
                switch (level)
                {
                    case 10: PlatformServices.Unlock(Achievement.Boss10); break;
                    case 20: PlatformServices.Unlock(Achievement.Boss20); break;
                    case 30: PlatformServices.Unlock(Achievement.Boss30); break;
                    case 40: PlatformServices.Unlock(Achievement.Boss40); break;
                }
            }
            if (level >= SceneFlow.LevelCount) PlatformServices.Unlock(Achievement.FinishGame);
            if (d == Difficulty.Hard) PlatformServices.Unlock(Achievement.ClearHard);
            if (d == Difficulty.Impossible) PlatformServices.Unlock(Achievement.ClearImpossible);
            PlatformServices.SubmitScore(Board.Campaign, Progress.CampaignTotal());
        }

        /// <summary>An endless run is over; completed = rings filled (Capture The Flag) or flags reached (Peace Walker).</summary>
        public static void RunEnded(GameMode mode, int score, int completed)
        {
            PlatformSettings s = PlatformSettings.Current;
            switch (mode)
            {
                // Capture The Flag plays the Rush rules (Rush left the menu): one board, and the two ring achievements
                // are now two steps of the same count (zoneRunnerCount, then flagRunnerCount).
                case GameMode.CaptureFlag:
                case GameMode.Rush:
                    PlatformServices.SubmitScore(Board.CaptureFlag, score);
                    if (completed >= s.zoneRunnerCount) PlatformServices.Unlock(Achievement.ZoneRunner);
                    if (completed >= s.flagRunnerCount) PlatformServices.Unlock(Achievement.FlagRunner);
                    break;
                case GameMode.PeaceWalker:
                    PlatformServices.SubmitScore(Board.PeaceWalker, score);
                    if (completed >= s.pacifistCount) PlatformServices.Unlock(Achievement.Pacifist);
                    break;
                case GameMode.Roguelike:
                    // Daily Run: only the day's scored attempt, on that day's board.
                    if (RoguelikeRun.IsDaily)
                    {
                        if (RoguelikeRun.DailyScored) PlatformServices.SubmitScoreTo(PlatformSettings.DailyBoardName(RoguelikeRun.DailyDate), score);
                    }
                    else PlatformServices.SubmitScore(Board.Roguelike, score);
                    break;
            }
        }

        /// <summary>A roguelike room is cleared (room 50: the run is won).</summary>
        public static void RoomCleared(int room, Difficulty d)
        {
            if (room >= 10) PlatformServices.Unlock(Achievement.RogueRoom10);
            if (room >= 25) PlatformServices.Unlock(Achievement.RogueRoom25);
            if (room >= RoguelikeRun.Rooms) PlatformServices.Unlock(Achievement.RogueComplete);
            if (d == Difficulty.Impossible && room >= PlatformSettings.Current.roguelikeImpossibleRoom)
                PlatformServices.Unlock(Achievement.RogueImpossible);
        }

        public static void PerkTaken(PerkId p, int rank)
        {
            if (p == PerkId.GermBuddy) PlatformServices.Unlock(Achievement.GermBuddy);
            if (Perks.Get(p).rarity == PerkRarity.Legendary) PlatformServices.Unlock(Achievement.Legendary);
            if (p == PerkId.ExtraBarrel && rank >= Perks.Get(p).maxRank) PlatformServices.Unlock(Achievement.FourLines);
        }

        public static void UpgradeBought(int owned, int total)
        {
            PlatformServices.Unlock(Achievement.FirstUpgrade);
            if (owned >= total) PlatformServices.Unlock(Achievement.AllUpgrades);
        }

        public static void WalletChanged(int vitamins)
        {
            if (vitamins >= PlatformSettings.Current.vitaminHoard) PlatformServices.Unlock(Achievement.VitaminHoarder);
        }
    }
}
