using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace AntiBacteria.Core
{
    /// <summary>The roguelike characters. Penicillin is always there; the others are unlocked in other modes.</summary>
    public enum StrainId { Penicillin = 0, Phage = 1, Antiviral = 2, Vaccine = 3 }

    /// <summary>Campaign medal for one level on one difficulty (kept as the best earned).</summary>
    public enum Medal { None = 0, Bronze = 1, Silver = 2, Gold = 3 }

    /// <summary>Text for a Loadout: "Start with Germ Buddy, Heavy Shots x2" and "One life fewer, Shoots 20% slower".</summary>
    public static class Loadouts
    {
        public static string PerksLine(Loadout l)
        {
            if (l == null || l.perks == null || l.perks.Length == 0) return "";
            var names = new List<string>();
            foreach (PerkGrant g in l.perks) names.Add(Perks.Name(g.perk) + (g.rank > 1 ? " x" + g.rank : ""));
            return Loc.Get(WordIDs.StartWith).Replace("{0}", string.Join(", ", names));
        }

        public static string DrawbackLine(Loadout l)
        {
            if (l == null) return "";
            var parts = new List<string>();
            if (l.extraLives < 0) parts.Add(Loc.Get(WordIDs.LifeFewer).Replace("{0}", (-l.extraLives).ToString()));
            if (l.fireSlowerPercent > 0) parts.Add(Loc.Get(WordIDs.FireSlower).Replace("{0}", l.fireSlowerPercent.ToString()));
            if (l.moveSlowerPercent > 0) parts.Add(Loc.Get(WordIDs.MoveSlower).Replace("{0}", l.moveSlowerPercent.ToString()));
            if (l.enemiesFasterPercent > 0) parts.Add(Loc.Get(WordIDs.EnemiesFaster).Replace("{0}", l.enemiesFasterPercent.ToString()));
            return string.Join(", ", parts);
        }
    }

    public static class Strains
    {
        public const int Count = 4;

        public static Loadout Get(StrainId id)
        {
            ReplaySettings s = ReplaySettings.Current;
            switch (id)
            {
                case StrainId.Phage: return s.phage;
                case StrainId.Antiviral: return s.antiviral;
                case StrainId.Vaccine: return s.vaccine;
                default: return null; // Penicillin: the classic cure, nothing added
            }
        }

        public static string Name(StrainId id)
        {
            switch (id)
            {
                case StrainId.Phage: return Loc.Get(WordIDs.StrainPhage);
                case StrainId.Antiviral: return Loc.Get(WordIDs.StrainAntiviral);
                case StrainId.Vaccine: return Loc.Get(WordIDs.StrainVaccine);
                default: return Loc.Get(WordIDs.StrainPenicillin);
            }
        }

        public static bool IsUnlocked(StrainId id) => id == StrainId.Penicillin || Progress.IsStrainUnlocked(id);

        /// <summary>What it starts with (Penicillin: "The classic cure").</summary>
        public static string Line1(StrainId id) => id == StrainId.Penicillin ? Loc.Get(WordIDs.StrainClassic) : Loadouts.PerksLine(Get(id));

        /// <summary>Its drawback, or how to unlock it.</summary>
        public static string Line2(StrainId id) => IsUnlocked(id) ? Loadouts.DrawbackLine(Get(id)) : UnlockHint(id);

        public static string UnlockHint(StrainId id)
        {
            ReplaySettings s = ReplaySettings.Current;
            switch (id)
            {
                case StrainId.Phage: return Loc.Get(WordIDs.UnlockPhage).Replace("{0}", s.phageGoldMedals.ToString());
                case StrainId.Antiviral: return Loc.Get(WordIDs.UnlockAntiviral).Replace("{0}", s.antiviralRings.ToString());
                case StrainId.Vaccine: return Loc.Get(WordIDs.UnlockVaccine).Replace("{0}", s.vaccineFlags.ToString());
                default: return "";
            }
        }
    }

    /// <summary>Roguelike Mutations: level N plays with rules 1..N and pays scorePercentPerMutation x N more.</summary>
    public static class Mutations
    {
        public const int Max = ReplaySettings.MaxMutation;

        private static readonly WordIDs[] Rules =
        {
            WordIDs.Mut1, WordIDs.Mut2, WordIDs.Mut3, WordIDs.Mut4, WordIDs.Mut5,
            WordIDs.Mut6, WordIDs.Mut7, WordIDs.Mut8, WordIDs.Mut9, WordIDs.Mut10,
        };

        /// <summary>The rule that level 1..10 adds, with its number.</summary>
        public static string Rule(int level)
        {
            if (level < 1 || level > Max) return "";
            ReplaySettings s = ReplaySettings.Current;
            int[] values =
            {
                s.m1EnemyHealthPercent, s.m2BonusShorterPercent, s.m3MoreEnemiesPercent, s.m4LivesLost, s.m5BulletsFasterPercent,
                s.m6FewerCards, s.m7BossHealthPercent, 0, s.m9MoreXpPercent, s.m10EnemiesFasterPercent,
            };
            return Loc.Get(Rules[level - 1]).Replace("{0}", values[level - 1].ToString());
        }

        public static float ScoreMultiplier(int level) => 1f + Mathf.Max(0, level) * ReplaySettings.Current.scorePercentPerMutation / 100f;

        /// <summary>"Score +30%".</summary>
        public static string ScoreLine(int level) =>
            Loc.Get(WordIDs.MutationScore).Replace("{0}", (Mathf.Max(0, level) * ReplaySettings.Current.scorePercentPerMutation).ToString());
    }

    /// <summary>
    /// The Daily Run: one roguelike run per UTC day, the same for everyone (seeded perk offers and room spawns, one
    /// twist), on Normal with no mutations and Penicillin. The first attempt of the day is scored (Daily leaderboard),
    /// the rest are practice.
    /// </summary>
    public static class DailyRun
    {
        // One name per twist in ReplaySettings.dailyTwists, in the same order (extra twists get "Today's twist 15").
        private static readonly WordIDs[] Names =
        {
            WordIDs.Twist0, WordIDs.Twist1, WordIDs.Twist2, WordIDs.Twist3, WordIDs.Twist4, WordIDs.Twist5, WordIDs.Twist6,
            WordIDs.Twist7, WordIDs.Twist8, WordIDs.Twist9, WordIDs.Twist10, WordIDs.Twist11, WordIDs.Twist12, WordIDs.Twist13,
        };

        /// <summary>"2026-11-05" (UTC, so everyone shares the same day).</summary>
        public static string Today => DateTime.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        /// <summary>A stable number for a date (string.GetHashCode differs between runtimes).</summary>
        public static int SeedFor(string date)
        {
            unchecked
            {
                uint h = 2166136261;
                foreach (char c in date ?? "") { h ^= c; h *= 16777619; }
                return (int)(h & 0x7fffffff);
            }
        }

        public static int TwistCount => Mathf.Max(1, ReplaySettings.Current.dailyTwists != null ? ReplaySettings.Current.dailyTwists.Length : 0);

        /// <summary>
        /// Twists rotate in shuffled cycles: every twist once per cycle (one cycle = TwistCount days), a new order each
        /// cycle, and never the same twist two days in a row (also across cycles).
        /// </summary>
        public static int TwistFor(string date)
        {
            int n = TwistCount;
            if (n <= 1) return 0;
            if (!DateTime.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime d))
                return (int)((uint)SeedFor(date) % (uint)n);
            int day = (int)(d.Date - new DateTime(2026, 1, 1)).TotalDays;
            int cycle = day >= 0 ? day / n : (day - n + 1) / n; // floor, also before 2026
            return Cycle(cycle, n)[day - cycle * n];
        }

        private static int[] Cycle(int cycle, int n)
        {
            int[] order = Shuffled(cycle, n);
            if (n > 2 && order[0] == Shuffled(cycle - 1, n)[n - 1]) { int t = order[0]; order[0] = order[1]; order[1] = t; }
            return order;
        }

        private static int[] Shuffled(int cycle, int n)
        {
            var order = new int[n];
            for (int i = 0; i < n; i++) order[i] = i;
            var rng = new System.Random(SeedFor("cycle " + cycle + " of " + n));
            for (int i = n - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                int t = order[i]; order[i] = order[j]; order[j] = t;
            }
            return order;
        }

        public static Loadout Twist(int index)
        {
            Loadout[] all = ReplaySettings.Current.dailyTwists;
            return all != null && all.Length > 0 ? all[Mathf.Clamp(index, 0, all.Length - 1)] : null;
        }

        public static string TwistName(int index) =>
            index >= 0 && index < Names.Length ? Loc.Get(Names[index]) : Loc.Get(WordIDs.DailyTwist) + " " + (index + 1);

        /// <summary>The first attempt today counts for the leaderboard.</summary>
        public static bool ScoredAttemptAvailable => Progress.DailyDate != Today;
    }

    public static class Medals
    {
        /// <summary>Bronze for a clear, silver for at most silverMaxLivesLost lives lost, gold for none.</summary>
        public static Medal For(int livesLost, bool immortalityOn)
        {
            ReplaySettings s = ReplaySettings.Current;
            if (immortalityOn && s.immortalityCapsAtBronze) return Medal.Bronze;
            if (livesLost <= 0) return Medal.Gold;
            return livesLost <= s.silverMaxLivesLost ? Medal.Silver : Medal.Bronze;
        }

        /// <summary>Medal stars, drawn by the m6x11_extras font: bronze 1, silver 2, gold 3.</summary>
        public const string StarFull = "\u2605", StarEmpty = "\u2606";

        public static int StarCount(Medal m) => Mathf.Clamp((int)m, 0, 3);

        /// <summary>"★★☆" for silver; "☆☆☆" for none.</summary>
        public static string Stars(Medal m)
        {
            int n = StarCount(m);
            var sb = new System.Text.StringBuilder(3);
            for (int i = 0; i < 3; i++) sb.Append(i < n ? StarFull : StarEmpty);
            return sb.ToString();
        }

        public static string Name(Medal m)
        {
            switch (m)
            {
                case Medal.Gold: return Loc.Get(WordIDs.MedalGold);
                case Medal.Silver: return Loc.Get(WordIDs.MedalSilver);
                case Medal.Bronze: return Loc.Get(WordIDs.MedalBronze);
                default: return "-";
            }
        }
    }

    /// <summary>
    /// Unlocks that come from playing: strains (from other modes) and the next Mutation. What was unlocked during the
    /// current level or run is kept in Message for the result screen, and cleared when the next one starts.
    /// </summary>
    public static class Unlocks
    {
        public static string Message { get; private set; }

        public static void Clear() => Message = null;

        /// <summary>An endless run ended: Capture The Flag and Peace Walker unlock strains.</summary>
        public static void RunEnded(GameMode mode, int completed)
        {
            ReplaySettings s = ReplaySettings.Current;
            if (mode == GameMode.CaptureFlag && completed >= s.antiviralRings) UnlockStrain(StrainId.Antiviral);
            if (mode == GameMode.PeaceWalker && completed >= s.vaccineFlags) UnlockStrain(StrainId.Vaccine);
        }

        /// <summary>A campaign medal was recorded: enough golds unlock the Phage.</summary>
        public static void MedalsChanged()
        {
            if (Progress.GoldMedalCount() >= ReplaySettings.Current.phageGoldMedals) UnlockStrain(StrainId.Phage);
        }

        /// <summary>All 50 rooms cleared (not a Daily Run): the next Mutation opens.</summary>
        public static void RoguelikeCompleted(int mutation)
        {
            int next = mutation + 1;
            if (next > Mutations.Max || !Progress.UnlockMutation(next)) return;
            Add(Loc.Get(WordIDs.MutationUnlocked).Replace("{0}", next.ToString()));
        }

        private static void UnlockStrain(StrainId id)
        {
            if (!Progress.UnlockStrain(id)) return;
            Add(Loc.Get(WordIDs.StrainUnlocked).Replace("{0}", Strains.Name(id)));
        }

        private static void Add(string text) => Message = string.IsNullOrEmpty(Message) ? text : Message + "  " + text;
    }
}
