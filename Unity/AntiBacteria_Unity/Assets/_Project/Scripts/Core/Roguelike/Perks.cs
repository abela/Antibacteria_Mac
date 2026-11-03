namespace AntiBacteria.Core
{
    /// <summary>Roguelike perks. Appended only: ids are kept in RoguelikeRun's rank table.</summary>
    public enum PerkId
    {
        RapidFire, ExtraBarrel, HeavyShots, Piercing, QuickFeet,
        Vitality, FirstAid, Shield, SecondWind, Nova,
        Magnet, Wisdom, GoldenTouch, BonusLover, TimeBender,
        // v2
        CriticalHit, BigShots, Reroll, Ricochet, FrostShots,
        Lucky, ChainZap, Orbiter, Vampire, LastStand, Overdrive,
        // the companion and its upgrades (offered only once Germ Buddy is taken)
        GermBuddy, BuddyTrigger, BuddyPunch, Fetch, DoubleBarrel, Sharpshooter, Bodyguard, Copycat,
    }

    /// <summary>How rare a perk is. Deeper rooms (and harder difficulties, and the Lucky perk) roll rarer ones.</summary>
    public enum PerkRarity { Common = 0, Rare = 1, Epic = 2, Legendary = 3 }

    public class PerkInfo
    {
        public PerkId id;
        public int maxRank;
        public PerkRarity rarity;
        /// <summary>Only offered once this perk is owned (buddy upgrades need Germ Buddy). Null: always.</summary>
        public PerkId? requires;
        public WordIDs name, description;
        public string[] icon; // 9x9: '#' foreground, 'R' accent

        /// <summary>The value shown in the description's {0} (percent, count...), from RoguelikeSettings.</summary>
        public System.Func<RoguelikeSettings, string> value;

        public bool IsBuddyPerk => requires == PerkId.GermBuddy;
    }

    /// <summary>The perk table: what each one is called, how rare, how far it stacks, and its 9x9 icon.</summary>
    public static class Perks
    {
        private const PerkRarity C = PerkRarity.Common, R = PerkRarity.Rare, E = PerkRarity.Epic, L = PerkRarity.Legendary;

        public static readonly PerkInfo[] All =
        {
            P(PerkId.RapidFire, 3, C, WordIDs.PerkRapidFire, WordIDs.PerkRapidFireDesc, s => s.rapidFirePercent.ToString(), new[]
            {
                ".........", "..R...R..", ".RRR.RRR.", "..R...R..", "..#...#..", "..#...#..", "..#...#..", "..#...#..", ".........",
            }),
            // Extra Bullet: one more parallel bullet line per rank (1 -> 4 lines).
            P(PerkId.ExtraBarrel, 3, R, WordIDs.PerkExtraBarrel, WordIDs.PerkExtraBarrelDesc, s => "1", new[]
            {
                ".........", ".R.R.R.R.", ".R.R.R.R.", ".#.#.#.#.", ".#.#.#.#.", ".#.#.#.#.", ".#.#.#.#.", ".........", ".........",
            }),
            P(PerkId.HeavyShots, 2, E, WordIDs.PerkHeavyShots, WordIDs.PerkHeavyShotsDesc, s => "1", new[]
            {
                "...RRR...", "..RRRRR..", "..RRRRR..", "..#####..", "..#...#..", "..#...#..", "..#...#..", "..#####..", ".........",
            }),
            P(PerkId.Piercing, 2, R, WordIDs.PerkPiercing, WordIDs.PerkPiercingDesc, s => "1", new[]
            {
                ".........", "...###...", "..#...#..", ".#.....#.", "RRRRRRRRR", ".#.....#.", "..#...#..", "...###...", ".........",
            }),
            P(PerkId.QuickFeet, 4, C, WordIDs.PerkQuickFeet, WordIDs.PerkQuickFeetDesc, s => s.quickFeetPercent.ToString(), new[]
            {
                ".........", "#...R....", "....RR...", "##..RRR..", "....RRRR.", "##..RRR..", "....RR...", "#...R....", ".........",
            }),
            P(PerkId.Vitality, 2, C, WordIDs.PerkVitality, WordIDs.PerkVitalityDesc, s => "1", new[]
            {
                ".........", ".RR.RR...", "RRRRRRR..", "RRRRRRR..", ".RRRRR...", "..RRR..#.", "...R..###", ".......#.", ".........",
            }),
            P(PerkId.FirstAid, 1, R, WordIDs.PerkFirstAid, WordIDs.PerkFirstAidDesc, s => s.firstAidEveryRooms.ToString(), new[]
            {
                ".........", "#########", "#...R...#", "#...R...#", "#.RRRRR.#", "#...R...#", "#...R...#", "#########", ".........",
            }),
            P(PerkId.Shield, 1, R, WordIDs.PerkShield, WordIDs.PerkShieldDesc, s => "1", new[]
            {
                "#########", "#.......#", "#..RRR..#", "#..RRR..#", ".#.RRR.#.", "..#...#..", "...#.#...", "....#....", ".........",
            }),
            P(PerkId.SecondWind, 1, E, WordIDs.PerkSecondWind, WordIDs.PerkSecondWindDesc, s => "1", new[]
            {
                "...RRR...", "..R...R..", "..R...R..", "...RRR...", "....#....", "..#####..", "....#....", "....#....", "....#....",
            }),
            P(PerkId.Nova, 1, E, WordIDs.PerkNova, WordIDs.PerkNovaDesc, s => "1", new[]
            {
                "#...#...#", ".#..#..#.", "..#.R.#..", "...RRR...", "###RRR###", "...RRR...", "..#.R.#..", ".#..#..#.", "#...#...#",
            }),
            P(PerkId.Magnet, 3, C, WordIDs.PerkMagnet, WordIDs.PerkMagnetDesc, s => s.magnetPercent.ToString(), new[]
            {
                ".........", ".RR...RR.", ".##...##.", ".##...##.", ".##...##.", ".##...##.", "..##.##..", "...###...", ".........",
            }),
            P(PerkId.Wisdom, 3, C, WordIDs.PerkWisdom, WordIDs.PerkWisdomDesc, s => s.wisdomPercent.ToString(), new[]
            {
                "....R....", "...RRR...", "RRRRRRRRR", ".RRRRRRR.", "..RRRRR..", ".RRR.RRR.", "RR.....RR", ".........", ".........",
            }),
            P(PerkId.GoldenTouch, 3, C, WordIDs.PerkGoldenTouch, WordIDs.PerkGoldenTouchDesc, s => s.goldenTouchPercent.ToString(), new[]
            {
                "..RRR....", ".RRRRR...", "RR#RRRR..", "RRRRRRR..", ".RRRRR.#.", "..RRR.###", ".......#.", ".........", ".........",
            }),
            P(PerkId.BonusLover, 3, C, WordIDs.PerkBonusLover, WordIDs.PerkBonusLoverDesc, s => s.bonusLoverPercent.ToString(), new[]
            {
                "#########", "#.RRRRR.#", "#..RRR..#", "#...R...#", "#..R.R..#", "#.RRRRR.#", "#########", ".........", ".........",
            }),
            P(PerkId.TimeBender, 2, C, WordIDs.PerkTimeBender, WordIDs.PerkTimeBenderDesc, s => s.timeBenderPercent.ToString(), new[]
            {
                "..#####..", ".#.....#.", "#...R...#", "#...R...#", "#...RRR.#", "#.......#", ".#.....#.", "..#####..", ".........",
            }),

            // ---------------------------------------------------------------- v2
            P(PerkId.CriticalHit, 3, C, WordIDs.PerkCriticalHit, WordIDs.PerkCriticalHitDesc, s => s.criticalChancePercent.ToString(), new[]
            {
                "....#....", "..#####..", ".#..#..#.", ".#.....#.", "###.R.###", ".#.....#.", ".#..#..#.", "..#####..", "....#....",
            }),
            P(PerkId.BigShots, 1, C, WordIDs.PerkBigShots, WordIDs.PerkBigShotsDesc, s => "2", new[]
            {
                ".........", "...###...", "..#RRR#..", ".#RRRRR#.", ".#RRRRR#.", ".#RRRRR#.", "..#RRR#..", "...###...", ".........",
            }),
            P(PerkId.Reroll, 3, C, WordIDs.PerkReroll, WordIDs.PerkRerollDesc, s => "1", new[]
            {
                "#########", "#.......#", "#.R...R.#", "#.......#", "#...R...#", "#.......#", "#.R...R.#", "#.......#", "#########",
            }),
            P(PerkId.Ricochet, 2, R, WordIDs.PerkRicochet, WordIDs.PerkRicochetDesc, s => "1", new[]
            {
                "#........", "#.....R..", "#....R...", "#...R....", "#..R.....", "#...R....", "#....R...", "#.....R..", "#........",
            }),
            P(PerkId.FrostShots, 2, R, WordIDs.PerkFrostShots, WordIDs.PerkFrostShotsDesc, s => s.frostChancePercent.ToString(), new[]
            {
                "..#.#.#..", "...###...", "#..#.#..#", ".##.R.##.", "..#RRR#..", ".##.R.##.", "#..#.#..#", "...###...", "..#.#.#..",
            }),
            P(PerkId.Lucky, 2, R, WordIDs.PerkLucky, WordIDs.PerkLuckyDesc, s => s.luckyRooms.ToString(), new[]
            {
                "..RR.RR..", ".RRRRRRR.", ".RRRRRRR.", "..RR#RR..", ".RRR#RRR.", ".RRRRRRR.", "..RR.#...", "......#..", ".........",
            }),
            P(PerkId.ChainZap, 2, E, WordIDs.PerkChainZap, WordIDs.PerkChainZapDesc, s => "1", new[]
            {
                ".....RRR.", "....RR...", "...RR....", "..RRRRR..", "....RR...", "...RR....", "..RR.....", ".#.......", "#........",
            }),
            P(PerkId.Orbiter, 2, E, WordIDs.PerkOrbiter, WordIDs.PerkOrbiterDesc, s => "1", new[]
            {
                "......R..", "..###RRR.", ".#....R#.", "#..###..#", "#..#R#..#", "#..###..#", ".#.....#.", "..#####..", ".........",
            }),
            P(PerkId.Vampire, 1, E, WordIDs.PerkVampire, WordIDs.PerkVampireDesc, s => s.vampireKills.ToString(), new[]
            {
                ".........", "#########", "#.......#", ".#R...R#.", "..R...R..", "..R...R..", ".........", "....R....", "...RRR...",
            }),
            P(PerkId.LastStand, 1, L, WordIDs.PerkLastStand, WordIDs.PerkLastStandDesc, s => s.lastStandFireMultiplier.ToString("0.#"), new[]
            {
                ".RRR.RRR.", "RRRRRRRRR", "RRRR#RRRR", "RRRR#RRRR", ".RRR#RRR.", "..RRRRR..", "...R#R...", "....R....", ".........",
            }),
            P(PerkId.Overdrive, 1, L, WordIDs.PerkOverdrive, WordIDs.PerkOverdriveDesc, s => s.overdriveEvery.ToString(), new[]
            {
                "...#.....", "....#....", "RRRRRR#..", "RRRRRRR#.", "RRRRRRRR#", "RRRRRRR#.", "RRRRRR#..", "....#....", "...#.....",
            }),

            // ---------------------------------------------------------------- the companion
            P(PerkId.GermBuddy, 2, R, WordIDs.PerkGermBuddy, WordIDs.PerkGermBuddyDesc, s => "1", new[]
            {
                "..#####..", ".#.....#.", "#..R.R..#", "#.......#", "#..RRR..#", "#.......#", ".#.....#.", "..#####..", ".#.....#.",
            }),
            B(PerkId.BuddyTrigger, 2, C, WordIDs.PerkBuddyTrigger, WordIDs.PerkBuddyTriggerDesc, s => s.buddyTriggerPercent.ToString(),
                "..R.R", "..R.R", "..#.#", "..#.#", "....."),
            B(PerkId.BuddyPunch, 2, C, WordIDs.PerkBuddyPunch, WordIDs.PerkBuddyPunchDesc, s => s.buddyPunchPercent.ToString(),
                "..R..", ".RRR.", "..R..", ".....", "....."),
            B(PerkId.Fetch, 1, C, WordIDs.PerkFetch, WordIDs.PerkFetchDesc, s => "1",
                "..#..", ".#R#.", "#RRR#", ".#R#.", "..#.."),
            B(PerkId.DoubleBarrel, 1, R, WordIDs.PerkDoubleBarrel, WordIDs.PerkDoubleBarrelDesc, s => "1",
                ".R.R.", ".#.#.", ".#.#.", ".#.#.", "....."),
            B(PerkId.Sharpshooter, 2, R, WordIDs.PerkSharpshooter, WordIDs.PerkSharpshooterDesc, s => "1",
                ".....", "..#..", "RRRRR", "..#..", "....."),
            B(PerkId.Bodyguard, 1, E, WordIDs.PerkBodyguard, WordIDs.PerkBodyguardDesc, s => "1",
                "#####", "#RRR#", "#RRR#", ".#R#.", "..#.."),
            B(PerkId.Copycat, 1, E, WordIDs.PerkCopycat, WordIDs.PerkCopycatDesc, s => "1",
                "###..", "#.###", "###R#", "..#R#", "..###"),
        };

        public static int Count => All.Length;

        public static PerkInfo Get(PerkId id) => All[(int)id];

        public static string Name(PerkId id) => Loc.Get(Get(id).name);

        /// <summary>"Shoot 15% faster", with the number from RoguelikeSettings.</summary>
        public static string Description(PerkId id)
        {
            PerkInfo p = Get(id);
            string text = Loc.Get(p.description);
            return p.value != null ? text.Replace("{0}", p.value(RoguelikeSettings.Current)) : text;
        }

        public static string RarityName(PerkRarity r)
        {
            switch (r)
            {
                case PerkRarity.Rare: return Loc.Get(WordIDs.RarityRare);
                case PerkRarity.Epic: return Loc.Get(WordIDs.RarityEpic);
                case PerkRarity.Legendary: return Loc.Get(WordIDs.RarityLegendary);
                default: return Loc.Get(WordIDs.RarityCommon);
            }
        }

        private static PerkInfo P(PerkId id, int max, PerkRarity rarity, WordIDs name, WordIDs desc,
            System.Func<RoguelikeSettings, string> value, string[] icon) =>
            new PerkInfo { id = id, maxRank = max, rarity = rarity, name = name, description = desc, value = value, icon = icon };

        /// <summary>A buddy upgrade: the mini buddy bottom left, its own 5x5 symbol top right; needs Germ Buddy.</summary>
        private static PerkInfo B(PerkId id, int max, PerkRarity rarity, WordIDs name, WordIDs desc,
            System.Func<RoguelikeSettings, string> value, params string[] symbol)
        {
            string[] buddy = { ".###.", "#R.R#", "#...#", "#.R.#", ".###." };
            var grid = new char[9, 9];
            for (int y = 0; y < 9; y++) for (int x = 0; x < 9; x++) grid[y, x] = '.';
            for (int y = 0; y < 5; y++) for (int x = 0; x < 5; x++) grid[y + 4, x] = buddy[y][x];
            for (int y = 0; y < 5; y++) for (int x = 0; x < 5; x++) if (symbol[y][x] != '.') grid[y, x + 4] = symbol[y][x];
            var rows = new string[9];
            for (int y = 0; y < 9; y++)
            {
                var line = new char[9];
                for (int x = 0; x < 9; x++) line[x] = grid[y, x];
                rows[y] = new string(line);
            }
            PerkInfo p = P(id, max, rarity, name, desc, value, rows);
            p.requires = PerkId.GermBuddy;
            return p;
        }
    }
}
