using System;

namespace AntiBacteria.Core
{
    public enum Difficulty { Normal = 0, Hard = 1, Impossible = 2 }

    /// <summary>The current level attempt: which level, and what was earned in it. Persistence comes with the save system.</summary>
    public static class GameSession
    {
        /// <summary>Campaign level being played, 1..50.</summary>
        public static int CurrentLevel { get; set; } = 1;

        public static int Score { get; private set; }
        public static int Vitamins { get; private set; }
        public static int Kills { get; private set; }
        public static Difficulty Difficulty { get; set; } = Difficulty.Normal;

        public static event Action Changed;

        public static void ResetRun()
        {
            Score = 0;
            Vitamins = 0;
            Kills = 0;
            Changed?.Invoke();
        }

        public static void AddScore(int points)
        {
            Score += points;
            Changed?.Invoke();
        }

        public static void AddKill()
        {
            Kills++;
            Changed?.Invoke();
        }

        /// <summary>Vitamins are credited when PICKED UP (the original credited them on drop - Docs QUIRK, fixed).</summary>
        public static void AddVitamins(int amount)
        {
            Vitamins += amount;
            Changed?.Invoke();
        }
    }
}
