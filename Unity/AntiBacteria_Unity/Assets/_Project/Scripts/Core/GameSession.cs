using System;

namespace AntiBacteria.Core
{
    public enum Difficulty { Normal = 0, Hard = 1, Impossible = 2 }

    /// <summary>Score and vitamins earned in the current run. Persistence comes later with the save system.</summary>
    public static class GameSession
    {
        public static int Score { get; private set; }
        public static int Vitamins { get; private set; }
        public static Difficulty Difficulty { get; set; } = Difficulty.Normal;

        public static event Action Changed;

        public static void ResetRun()
        {
            Score = 0;
            Vitamins = 0;
            Changed?.Invoke();
        }

        public static void AddScore(int points)
        {
            Score += points;
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
