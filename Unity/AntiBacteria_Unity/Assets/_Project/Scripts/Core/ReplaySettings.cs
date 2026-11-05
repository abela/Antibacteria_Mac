using System;
using UnityEngine;

namespace AntiBacteria.Core
{
    /// <summary>What a run starts with: free perks plus drawbacks (a strain, or the Daily Run's twist).</summary>
    [Serializable]
    public class Loadout
    {
        [Tooltip("Perks owned from the first room (rank capped at the perk's maximum).")]
        public PerkGrant[] perks = new PerkGrant[0];
        [Tooltip("Added to the maximum lives (negative = fewer). Never below 1 life.")]
        public int extraLives;
        [Tooltip("Percent slower shooting (20 = the player fires 20% less often).")]
        [Range(0, 90)] public int fireSlowerPercent;
        [Tooltip("Percent slower movement.")]
        [Range(0, 90)] public int moveSlowerPercent;
        [Tooltip("Percent faster enemies.")]
        [Range(0, 200)] public int enemiesFasterPercent;
    }

    [Serializable]
    public struct PerkGrant
    {
        public PerkId perk;
        [Min(1)] public int rank;
        public PerkGrant(PerkId perk, int rank) { this.perk = perk; this.rank = rank; }
    }

    /// <summary>
    /// The replay layer: roguelike Mutations, Strains, the Daily Run and campaign medals.
    /// Edit in Project Settings > AntiBacteria > Replay. Lives in Resources so builds read the same values.
    /// </summary>
    [CreateAssetMenu(menuName = "AntiBacteria/Replay Settings", fileName = "ReplaySettings")]
    public class ReplaySettings : ScriptableObject
    {
        public const string ResourcePath = "ReplaySettings";
        public const int MaxMutation = 10;

        [Header("Mutations (roguelike). Level N has rules 1..N. Clearing room 50 at level N unlocks N+1.")]
        [Tooltip("Score bonus per mutation level, in percent.")]
        public int scorePercentPerMutation = 10;
        [Tooltip("1: enemy health, percent more.")]
        public int m1EnemyHealthPercent = 25;
        [Tooltip("2: bonuses last this much shorter, percent.")]
        public int m2BonusShorterPercent = 30;
        [Tooltip("3: enemies per room, percent more.")]
        public int m3MoreEnemiesPercent = 25;
        [Tooltip("4: maximum lives lost.")]
        public int m4LivesLost = 1;
        [Tooltip("5: enemy bullets, percent faster.")]
        public int m5BulletsFasterPercent = 30;
        [Tooltip("6: fewer perk cards per level up (never fewer than 2).")]
        public int m6FewerCards = 1;
        [Tooltip("7: boss health, percent more.")]
        public int m7BossHealthPercent = 50;
        // 8: no healing when a room is cleared (nothing to set).
        [Tooltip("9: XP needed per level, percent more.")]
        public int m9MoreXpPercent = 25;
        [Tooltip("10: enemies move this much faster, percent.")]
        public int m10EnemiesFasterPercent = 20;

        [Header("Strains (roguelike characters)")]
        public Loadout phage = new Loadout { perks = new[] { new PerkGrant(PerkId.GermBuddy, 1) }, fireSlowerPercent = 20 };
        public Loadout antiviral = new Loadout { perks = new[] { new PerkGrant(PerkId.Piercing, 1), new PerkGrant(PerkId.HeavyShots, 1) }, extraLives = -1 };
        public Loadout vaccine = new Loadout { perks = new[] { new PerkGrant(PerkId.Shield, 1), new PerkGrant(PerkId.Vitality, 1) }, moveSlowerPercent = 15 };
        [Tooltip("Phage: campaign gold medals needed (any difficulty counts each).")]
        [Min(1)] public int phageGoldMedals = 10;
        [Tooltip("Antiviral: rings filled in one Capture The Flag run.")]
        [Min(1)] public int antiviralRings = 30;
        [Tooltip("Vaccine: flags reached in one Peace Walker run.")]
        [Min(1)] public int vaccineFlags = 15;

        [Header("Daily Run (Normal difficulty, no mutations, Penicillin). Twist = day's seed modulo the list.")]
        public Loadout[] dailyTwists =
        {
            new Loadout { perks = new[] { new PerkGrant(PerkId.BigShots, 1) } },
            new Loadout { perks = new[] { new PerkGrant(PerkId.GermBuddy, 1) } },
            new Loadout { perks = new[] { new PerkGrant(PerkId.HeavyShots, 2) }, extraLives = -1 },
            new Loadout { perks = new[] { new PerkGrant(PerkId.QuickFeet, 2) }, enemiesFasterPercent = 20 },
            new Loadout { perks = new[] { new PerkGrant(PerkId.Ricochet, 2) } },
            new Loadout { perks = new[] { new PerkGrant(PerkId.FrostShots, 2) } },
        };

        [Header("Campaign medals (per level and difficulty)")]
        [Tooltip("Silver: at most this many lives lost. Gold: none lost. Bronze: cleared.")]
        [Min(1)] public int silverMaxLivesLost = 1;
        [Tooltip("With the bought Immortality upgrade on, hits never cost a life: the medal is capped at bronze.")]
        public bool immortalityCapsAtBronze = true;

        private static ReplaySettings current;

        public static ReplaySettings Current
        {
            get
            {
                if (current != null) return current;
                current = Resources.Load<ReplaySettings>(ResourcePath);
                if (current == null)
                {
                    current = CreateInstance<ReplaySettings>();
                    current.hideFlags = HideFlags.HideAndDontSave;
                }
                return current;
            }
        }

        public static void Use(ReplaySettings settings) => current = settings;
    }
}
