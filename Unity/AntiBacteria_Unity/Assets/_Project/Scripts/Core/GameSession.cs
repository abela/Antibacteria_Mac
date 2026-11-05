using System;
using UnityEngine;

namespace AntiBacteria.Core
{
    public enum Difficulty { Normal = 0, Hard = 1, Impossible = 2 }

    /// <summary>What the Gameplay scene runs: a campaign level or one of the three endless modes.</summary>
    public enum GameMode { Campaign = 0, CaptureFlag = 1, Rush = 2, PeaceWalker = 3, Roguelike = 4 }

    /// <summary>The current level attempt: which level, and what was earned in it. Persistence comes with the save system.</summary>
    public static class GameSession
    {
        /// <summary>Campaign level being played, 1..50.</summary>
        public static int CurrentLevel { get; set; } = 1;

        public static int Score { get; private set; }
        public static int Vitamins { get; private set; }
        public static int Kills { get; private set; }
        public static Difficulty Difficulty { get; set; } = Difficulty.Normal;
        public static GameMode Mode { get; set; } = GameMode.Campaign;
        public static bool IsEndless => Mode != GameMode.Campaign;

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
            // Harder difficulties pay more, in the campaign and the endless modes (DifficultySettings; 2013 modes: x1/x2/x3).
            // Roguelike Mutations pay more on top (RunModifiers.ScoreMultiplier is 1 everywhere else).
            Score += Mathf.RoundToInt(points * DifficultySettings.Current.Score(Difficulty) * RunModifiers.ScoreMultiplier);
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
            Progress.AddVitamins(amount); // the wallet the upgrade shop spends
            Changed?.Invoke();
        }
    }
}
