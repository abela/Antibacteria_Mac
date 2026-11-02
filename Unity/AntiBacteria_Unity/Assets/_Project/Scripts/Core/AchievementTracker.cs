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

        /// <summary>An endless run is over; completed = flags taken or zones filled.</summary>
        public static void RunEnded(GameMode mode, int score, int completed)
        {
            PlatformSettings s = PlatformSettings.Current;
            switch (mode)
            {
                case GameMode.CaptureFlag:
                    PlatformServices.SubmitScore(Board.CaptureFlag, score);
                    if (completed >= s.flagRunnerCount) PlatformServices.Unlock(Achievement.FlagRunner);
                    break;
                case GameMode.Rush:
                    PlatformServices.SubmitScore(Board.Rush, score);
                    if (completed >= s.zoneRunnerCount) PlatformServices.Unlock(Achievement.ZoneRunner);
                    break;
                case GameMode.PeaceWalker:
                    PlatformServices.SubmitScore(Board.PeaceWalker, score);
                    if (completed >= s.pacifistCount) PlatformServices.Unlock(Achievement.Pacifist);
                    break;
            }
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
