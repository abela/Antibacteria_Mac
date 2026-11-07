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

        [Header("Tougher, more aggressive enemies (campaign by world 1..5, roguelike by room)")]
        [Tooltip("Fewest hits any enemy can take, per difficulty (Normal, Hard, Impossible): 2 = nothing dies to one shot.")]
        public int[] minHits = { 1, 2, 2 };
        [Tooltip("Campaign: extra hits for every non-boss enemy, by world 1..5, on Normal.")]
        public int[] campaignExtraHitsNormal = { 0, 0, 1, 1, 2 };
        [Tooltip("Campaign: extra hits by world on Hard.")]
        public int[] campaignExtraHitsHard = { 1, 1, 2, 3, 3 };
        [Tooltip("Campaign: extra hits by world on Impossible.")]
        public int[] campaignExtraHitsImpossible = { 1, 2, 3, 4, 4 };
        [Tooltip("Campaign: enemies move and shoot this much faster, by world, on Normal.")]
        public float[] campaignAggressionNormal = { 1f, 1f, 1.05f, 1.1f, 1.15f };
        public float[] campaignAggressionHard = { 1.1f, 1.12f, 1.15f, 1.2f, 1.25f };
        public float[] campaignAggressionImpossible = { 1.15f, 1.2f, 1.25f, 1.3f, 1.35f };
        [Tooltip("Roguelike: extra hits at room 1 (Normal, Hard, Impossible), and one more every this many rooms.")]
        public int[] roguelikeExtraHits = { 0, 1, 1 };
        [Min(1)] public int roguelikeExtraHitEveryRooms = 15;
        [Tooltip("Roguelike: enemies move and shoot faster by this share per room (0.005 = +25% by room 50), times the difficulty's.")]
        public float roguelikeAggressionPerRoom = 0.005f;
        public float[] roguelikeAggression = { 1f, 1.1f, 1.15f };

        public int MinHits(Difficulty d) => minHits != null && minHits.Length > (int)d ? Mathf.Max(1, minHits[(int)d]) : 1;

        public int CampaignExtraHits(Difficulty d, int world) =>
            Mathf.Max(0, ByWorld(d == Difficulty.Hard ? campaignExtraHitsHard : d == Difficulty.Impossible ? campaignExtraHitsImpossible : campaignExtraHitsNormal, world, 0));

        public float CampaignAggression(Difficulty d, int world) =>
            Mathf.Max(0.1f, ByWorld(d == Difficulty.Hard ? campaignAggressionHard : d == Difficulty.Impossible ? campaignAggressionImpossible : campaignAggressionNormal, world, 1f));

        public int RoguelikeExtraHits(Difficulty d, int room) =>
            Mathf.Max(0, (roguelikeExtraHits != null && roguelikeExtraHits.Length > (int)d ? roguelikeExtraHits[(int)d] : 0) +
                         (room - 1) / Mathf.Max(1, roguelikeExtraHitEveryRooms));

        public float RoguelikeAggression(Difficulty d, int room) => (1f + roguelikeAggressionPerRoom * (room - 1)) * At(roguelikeAggression, d);

        private static T ByWorld<T>(T[] a, int world, T fallback) =>
            a != null && a.Length > 0 ? a[Mathf.Clamp(world - 1, 0, a.Length - 1)] : fallback;

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
