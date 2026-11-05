using System;
using System.Collections.Generic;
using AntiBacteria.Gameplay.Bonuses;
using AntiBacteria.Gameplay.Player;
using UnityEngine;

namespace AntiBacteria.Core
{
    /// <summary>
    /// One roguelike run across rooms (the Gameplay scene reloads for every room): room number, the run's XP level
    /// and perks, lives carried between rooms, total time. Score, kills and vitamins carry in GameSession.
    /// Started by SceneFlow.LoadMode(Roguelike) (menu, Retry), advanced by NextRoom (the gate).
    /// </summary>
    public static class RoguelikeRun
    {
        public const int Rooms = 50;

        public static bool Active => GameSession.Mode == GameMode.Roguelike;
        public static int Room { get; private set; } = 1;
        public static int Level { get; private set; } = 1;
        public static int Xp { get; private set; }
        public static int XpNeeded => Mathf.Max(1, Mathf.CeilToInt(RoguelikeSettings.Current.XpFor(Level) *
            (RunModifiers.M(9) ? 1f + ReplaySettings.Current.m9MoreXpPercent / 100f : 1f)));
        public static float Xp01 => Mathf.Clamp01((float)Xp / XpNeeded);
        /// <summary>Level ups waiting for a perk choice.</summary>
        public static int PendingLevelUps { get; private set; }
        /// <summary>Lives to start the next room with (-1: full).</summary>
        public static int CarriedLives { get; private set; } = -1;
        /// <summary>Bonuses still running when the player went through the gate: they keep going in the next room.</summary>
        public static PlayerBuffs.State CarriedBuffs { get; private set; }
        /// <summary>The scene was loaded through the gate (don't reset the score; lives carry over).</summary>
        public static bool ContinuingRoom { get; set; }
        public static float SecondsBeforeThisRoom { get; private set; }
        public static bool ReviveUsed { get; set; }
        /// <summary>Hits the Shield perk can still block in this room.</summary>
        public static int ShieldsLeft { get; set; }
        /// <summary>Hits the buddy can still take for the player in this room (Bodyguard).</summary>
        public static int BodyguardLeft { get; set; }
        /// <summary>Card rerolls left for the level up on screen (Reroll perk).</summary>
        public static int RerollsLeft { get; private set; }
        /// <summary>Kills toward the next Vampire heal (kept across rooms).</summary>
        public static int VampireKills { get; set; }

        /// <summary>Mutation level of this run (0 = none). Picked in the menu, never on a Daily Run.</summary>
        public static int Mutation { get; private set; }
        public static StrainId Strain { get; private set; }
        /// <summary>What the run started with: the strain's, or the Daily Run twist's (null: nothing extra).</summary>
        public static Loadout Loadout { get; private set; }

        /// <summary>Set by the menu: the next run started (menu, Retry) is the Daily Run.</summary>
        public static bool NextRunDaily { get; set; }
        public static bool IsDaily { get; private set; }
        /// <summary>The UTC day this Daily Run belongs to (its seed and leaderboard).</summary>
        public static string DailyDate { get; private set; } = "";
        /// <summary>This Daily Run is the day's scored attempt (otherwise practice).</summary>
        public static bool DailyScored { get; private set; }
        public static int DailyTwist { get; private set; }
        // Daily Run: perk offers come from the day's seed, so everyone is offered the same cards for the same picks.
        private static System.Random offerRng;

        private static readonly int[] ranks = new int[Perks.Count];
        private static float xpRemainder;

        public static event Action LevelUpReady;
        public static event Action<PerkId> PerkTaken;
        public static event Action Changed;

        public static int Rank(PerkId p) => ranks[(int)p];
        public static bool Has(PerkId p) => ranks[(int)p] > 0;

        public static void Begin()
        {
            Room = 1;
            Level = 1;
            Xp = 0;
            xpRemainder = 0f;
            PendingLevelUps = 0;
            CarriedLives = -1;
            CarriedBuffs = default;
            ContinuingRoom = false;
            SecondsBeforeThisRoom = 0f;
            ReviveUsed = false;
            RerollsLeft = 0;
            VampireKills = 0;
            Array.Clear(ranks, 0, ranks.Length);
            IsDaily = NextRunDaily;
            if (IsDaily)
            {
                DailyDate = DailyRun.Today;
                DailyScored = DailyRun.ScoredAttemptAvailable;
                if (DailyScored) Progress.StartDaily(DailyDate);
                DailyTwist = DailyRun.TwistFor(DailyDate);
                Mutation = 0;
                Strain = StrainId.Penicillin;
                Loadout = DailyRun.Twist(DailyTwist);
                offerRng = new System.Random(DailyRun.SeedFor(DailyDate));
            }
            else
            {
                DailyScored = false;
                Mutation = Progress.SelectedMutation;
                Strain = Progress.SelectedStrain;
                Loadout = Strains.Get(Strain);
                offerRng = null;
            }
            if (Loadout != null && Loadout.perks != null)
                foreach (PerkGrant g in Loadout.perks)
                    ranks[(int)g.perk] = Mathf.Clamp(Mathf.Max(ranks[(int)g.perk], g.rank), 0, Perks.Get(g.perk).maxRank);
            GameSession.CurrentLevel = Room;
            Changed?.Invoke();
        }

        /// <summary>Through the gate: the next room starts with these lives and these bonuses still running.</summary>
        public static void NextRoom(int lives, float roomSeconds, PlayerBuffs.State buffs = default)
        {
            Room = Mathf.Min(Rooms, Room + 1);
            CarriedLives = lives;
            CarriedBuffs = buffs;
            SecondsBeforeThisRoom += roomSeconds;
            ContinuingRoom = true;
            GameSession.CurrentLevel = Room;
        }

        public static void AddXp(float amount)
        {
            xpRemainder += amount * RunModifiers.XpMultiplier;
            int whole = Mathf.FloorToInt(xpRemainder);
            xpRemainder -= whole;
            Xp += whole;
            bool levelled = false;
            while (Xp >= XpNeeded)
            {
                Xp -= XpNeeded;
                Level++;
                PendingLevelUps++;
                levelled = true;
            }
            Changed?.Invoke();
            if (levelled) LevelUpReady?.Invoke();
        }

        /// <summary>
        /// The room perks are rolled at: the real room, plus luck from the difficulty and the Lucky perk.
        /// Rarer perks get likelier as this grows (RoguelikeSettings curves).
        /// </summary>
        public static float LuckRoom
        {
            get
            {
                RoguelikeSettings s = RoguelikeSettings.Current;
                return Room + s.DifficultyLuck(GameSession.Difficulty) + s.luckyRooms * Rank(PerkId.Lucky);
            }
        }

        /// <summary>Whether a perk can be offered now: below its top rank and its requirement owned.</summary>
        public static bool CanOffer(PerkInfo p) =>
            ranks[(int)p.id] < p.maxRank && (!p.requires.HasValue || Has(p.requires.Value));

        /// <summary>
        /// Up to 'count' different perks that can still rank up. Each card rolls a rarity by the weights at
        /// LuckRoom (only rarities that still have a perk to give), then a perk of that rarity.
        /// </summary>
        public static List<PerkId> Offer(int count)
        {
            var open = new List<PerkInfo>();
            foreach (PerkInfo p in Perks.All) if (CanOffer(p)) open.Add(p);
            var picked = new List<PerkId>();
            RoguelikeSettings s = RoguelikeSettings.Current;
            float room = LuckRoom;
            var weights = new float[4];
            while (picked.Count < count && open.Count > 0)
            {
                float total = 0f;
                for (int r = 0; r < 4; r++)
                {
                    weights[r] = open.Exists(p => (int)p.rarity == r) ? s.Weight((PerkRarity)r, room) : 0f;
                    total += weights[r];
                }
                List<PerkInfo> bucket;
                if (total <= 0f) bucket = open; // nothing left that the curves allow here: any open perk
                else
                {
                    float roll = Value01() * total;
                    int rarity = 0;
                    for (; rarity < 3; rarity++)
                    {
                        if (roll < weights[rarity]) break;
                        roll -= weights[rarity];
                    }
                    while (weights[rarity] <= 0f) rarity--; // float edge: land on a real one
                    PerkRarity chosen = (PerkRarity)rarity;
                    bucket = open.FindAll(p => p.rarity == chosen);
                }
                PerkInfo pick = bucket[Mathf.Min(bucket.Count - 1, (int)(Value01() * bucket.Count))];
                picked.Add(pick.id);
                open.Remove(pick);
            }
            return picked;
        }

        /// <summary>0..1 (below 1): from the day's seed on a Daily Run, otherwise Unity's random.</summary>
        private static float Value01() => offerRng != null ? (float)offerRng.NextDouble() : UnityEngine.Random.Range(0f, 0.99999f);

        /// <summary>A new level up is on screen: the Reroll perk's rerolls refill.</summary>
        public static void ResetRerolls() => RerollsLeft = Rank(PerkId.Reroll);

        /// <summary>Spend a reroll; false if none are left.</summary>
        public static bool UseReroll()
        {
            if (RerollsLeft <= 0) return false;
            RerollsLeft--;
            return true;
        }

        /// <summary>A level up with nothing left to offer (every perk at its top rank).</summary>
        public static void ConsumeLevelUp()
        {
            PendingLevelUps = Mathf.Max(0, PendingLevelUps - 1);
            Changed?.Invoke();
        }

        public static void Take(PerkId p)
        {
            if (ranks[(int)p] < Perks.Get(p).maxRank) ranks[(int)p]++;
            PendingLevelUps = Mathf.Max(0, PendingLevelUps - 1);
            AchievementTracker.PerkTaken(p, ranks[(int)p]);
            PerkTaken?.Invoke(p);
            Changed?.Invoke();
        }

        /// <summary>Test / editor helper: set a perk's rank directly.</summary>
        public static void SetRank(PerkId p, int rank)
        {
            ranks[(int)p] = Mathf.Clamp(rank, 0, Perks.Get(p).maxRank);
            PerkTaken?.Invoke(p);
            Changed?.Invoke();
        }
    }

    /// <summary>What the run's perks do, read by gameplay code. Neutral outside the roguelike.</summary>
    public static class RunModifiers
    {
        private static bool On => RoguelikeRun.Active;
        private static RoguelikeSettings S => RoguelikeSettings.Current;
        private static ReplaySettings Replay => ReplaySettings.Current;
        private static Loadout L => On ? RoguelikeRun.Loadout : null;

        /// <summary>The run's Mutation level (0 outside the roguelike).</summary>
        public static int Mutation => On ? RoguelikeRun.Mutation : 0;
        /// <summary>Mutation rule 1..10 is in play.</summary>
        public static bool M(int rule) => Mutation >= rule;

        /// <summary>Points are multiplied by this (Mutations pay more). 1 outside the roguelike.</summary>
        public static float ScoreMultiplier => Mutations.ScoreMultiplier(Mutation);
        /// <summary>Perk cards per level up (Mutation 6 takes some away; never below 2).</summary>
        public static int PerkChoices => Mathf.Max(Mathf.Min(2, S.perkChoices), S.perkChoices - (M(6) ? Replay.m6FewerCards : 0));
        /// <summary>Enemy movement: Mutation 10 and the Daily twist.</summary>
        public static float EnemySpeedMultiplier =>
            (M(10) ? 1f + Replay.m10EnemiesFasterPercent / 100f : 1f) * (1f + (L != null ? L.enemiesFasterPercent : 0) / 100f);
        public static float EnemyBulletSpeedMultiplier => M(5) ? 1f + Replay.m5BulletsFasterPercent / 100f : 1f;
        /// <summary>Enemy health on top of the room's own: Mutations 1 and 7 (bosses).</summary>
        public static float EnemyHealthMultiplier(bool bossRoom) =>
            (M(1) ? 1f + Replay.m1EnemyHealthPercent / 100f : 1f) * (bossRoom && M(7) ? 1f + Replay.m7BossHealthPercent / 100f : 1f);
        /// <summary>Enemies per normal room (Mutation 3).</summary>
        public static int MoreEnemies(int count) => M(3) ? Mathf.CeilToInt(count * (1f + Replay.m3MoreEnemiesPercent / 100f)) : count;
        /// <summary>Mutation 8: clearing a room heals nothing.</summary>
        public static bool NoRoomHeal => M(8);
        public static int R(PerkId p) => On ? RoguelikeRun.Rank(p) : 0;
        private static float Pct(int percent, PerkId p) => 1f + percent / 100f * R(p);

        public static float FireIntervalMultiplier
        {
            get
            {
                float m = Mathf.Pow(1f - S.rapidFirePercent / 100f, R(PerkId.RapidFire));
                if (L != null && L.fireSlowerPercent > 0) m /= Mathf.Max(0.1f, 1f - L.fireSlowerPercent / 100f); // strain drawback
                PlayerController pc = PlayerController.Current;
                if (R(PerkId.LastStand) > 0 && pc != null && pc.Lives == 1) m /= Mathf.Max(1f, S.lastStandFireMultiplier);
                return m;
            }
        }

        /// <summary>Extra Bullet: parallel bullet lines beside the first (0-3, so up to 4 lines).</summary>
        public static int ExtraLines => R(PerkId.ExtraBarrel);
        public static int ShotDamage => 1 + R(PerkId.HeavyShots);
        public static int Pierce => R(PerkId.Piercing);
        public static float SpeedMultiplier => Pct(S.quickFeetPercent, PerkId.QuickFeet) * (1f - (L != null ? L.moveSlowerPercent : 0) / 100f);
        /// <summary>Vitality perk, plus the strain or twist (may be negative), minus Mutation 4. PlayerController keeps at least 1.</summary>
        public static int ExtraMaxLives => R(PerkId.Vitality) + (L != null ? L.extraLives : 0) - (M(4) ? Replay.m4LivesLost : 0);
        public static bool HealOnRoomClear => R(PerkId.FirstAid) > 0 && RoguelikeRun.Room % Mathf.Max(1, S.firstAidEveryRooms) == 0;
        public static int ShieldsPerRoom => R(PerkId.Shield);
        public static bool CanRevive => R(PerkId.SecondWind) > 0 && !RoguelikeRun.ReviveUsed;
        public static bool NovaOnHit => R(PerkId.Nova) > 0;
        public static float MagnetMultiplier => Pct(S.magnetPercent, PerkId.Magnet);
        public static float XpMultiplier => Pct(S.wisdomPercent, PerkId.Wisdom);
        public static float VitaminMultiplier => Pct(S.goldenTouchPercent, PerkId.GoldenTouch);
        public static float BonusDurationMultiplier => Pct(S.bonusLoverPercent, PerkId.BonusLover) *
            (M(2) ? Mathf.Max(0.1f, 1f - Replay.m2BonusShorterPercent / 100f) : 1f);
        public static float SlowmoRechargeMultiplier => Pct(S.timeBenderPercent, PerkId.TimeBender);

        public static float CritChance => S.criticalChancePercent / 100f * R(PerkId.CriticalHit);
        public static int ShotScale => R(PerkId.BigShots) > 0 ? Mathf.Max(1, S.bigShotScale) : 1;
        public static int Bounces => R(PerkId.Ricochet);
        public static float FrostChance => S.frostChancePercent / 100f * R(PerkId.FrostShots);
        public static int ChainZaps => R(PerkId.ChainZap);
        public static int Orbiters => R(PerkId.Orbiter);
        public static bool Vampire => R(PerkId.Vampire) > 0;
        public static bool Overdrive => R(PerkId.Overdrive) > 0;

        public static int Buddies => R(PerkId.GermBuddy);
        public static float BuddyFireInterval => S.buddyFireInterval * Mathf.Pow(1f - S.buddyTriggerPercent / 100f, R(PerkId.BuddyTrigger));
        /// <summary>Damage per buddy shot as a fraction (1.5 = one hit, plus a second on half the shots).</summary>
        public static float BuddyDamage => 1f + S.buddyPunchPercent / 100f * R(PerkId.BuddyPunch);
        public static bool Fetch => R(PerkId.Fetch) > 0;
        public static int BuddyLines => 1 + R(PerkId.DoubleBarrel);
        public static int BuddyPierce => R(PerkId.Sharpshooter);
        public static bool Bodyguard => R(PerkId.Bodyguard) > 0 && Buddies > 0;
        public static bool Copycat => R(PerkId.Copycat) > 0;
    }
}
