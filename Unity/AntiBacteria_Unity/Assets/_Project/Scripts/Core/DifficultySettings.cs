using UnityEngine;

namespace AntiBacteria.Core
{
    /// <summary>
    /// What each difficulty gives back for the extra danger, everywhere (campaign and endless modes):
    /// a score multiplier and more vitamins from every drop. Shown to the player whenever they pick a difficulty.
    /// Edit in Project Settings > AntiBacteria > Difficulty. (How much harder it gets lives with the levels and
    /// enemies: faster waves, faster and more aggressive enemies.)
    /// </summary>
    [CreateAssetMenu(menuName = "AntiBacteria/Difficulty Settings", fileName = "DifficultySettings")]
    public class DifficultySettings : ScriptableObject
    {
        public const string ResourcePath = "DifficultySettings";

        [Tooltip("Points are multiplied by this (Normal, Hard, Impossible).")]
        public float[] scoreMultiplier = { 1f, 2f, 3f };
        [Tooltip("Vitamins dropped are multiplied by this (Normal, Hard, Impossible); fractions round up by chance.")]
        public float[] vitaminMultiplier = { 1f, 1.5f, 2f };

        public float Score(Difficulty d) => At(scoreMultiplier, d);
        public float Vitamins(Difficulty d) => At(vitaminMultiplier, d);

        private static float At(float[] a, Difficulty d) => a != null && a.Length > (int)d ? Mathf.Max(0f, a[(int)d]) : 1f;

        /// <summary>"x2", "x1.5".</summary>
        public static string Format(float m) => "x" + (Mathf.Approximately(m, Mathf.Round(m)) ? Mathf.RoundToInt(m).ToString() : m.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture));

        /// <summary>"Faster waves, quicker enemies" for the chosen difficulty, in the current language.</summary>
        public static string Description(Difficulty d) =>
            Loc.Get(d == Difficulty.Hard ? WordIDs.DiffHardDesc : d == Difficulty.Impossible ? WordIDs.DiffImpossibleDesc : WordIDs.DiffNormalDesc);

        /// <summary>"Score x2   Vitamins x1.5".</summary>
        public static string Rewards(Difficulty d) =>
            Loc.Get(WordIDs.Score) + " " + Format(Current.Score(d)) + "   " + Loc.Get(WordIDs.Vitamins) + " " + Format(Current.Vitamins(d));

        private static DifficultySettings current;

        public static DifficultySettings Current
        {
            get
            {
                if (current != null) return current;
                current = Resources.Load<DifficultySettings>(ResourcePath);
                if (current == null)
                {
                    current = CreateInstance<DifficultySettings>();
                    current.hideFlags = HideFlags.HideAndDontSave;
                }
                return current;
            }
        }

        public static void Use(DifficultySettings settings) => current = settings;
    }
}
