using System;
using System.Collections.Generic;
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
        public static int XpNeeded => RoguelikeSettings.Current.XpFor(Level);
        public static float Xp01 => Mathf.Clamp01((float)Xp / XpNeeded);
        /// <summary>Level ups waiting for a perk choice.</summary>
        public static int PendingLevelUps { get; private set; }
        /// <summary>Lives to start the next room with (-1: full).</summary>
        public static int CarriedLives { get; private set; } = -1;
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
            ContinuingRoom = false;
            SecondsBeforeThisRoom = 0f;
            ReviveUsed = false;
            RerollsLeft = 0;
            VampireKills = 0;
            Array.Clear(ranks, 0, ranks.Length);
            GameSession.CurrentLevel = Room;
            Changed?.Invoke();
        }

        /// <summary>Through the gate: the next room starts with these lives.</summary>
        public static void NextRoom(int lives, float roomSeconds)
        {
            Room = Mathf.Min(Rooms, Room + 1);
            CarriedLives = lives;
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
                    float roll = UnityEngine.Random.value * total;
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
                PerkInfo pick = bucket[UnityEngine.Random.Range(0, bucket.Count)];
                picked.Add(pick.id);
                open.Remove(pick);
            }
            return picked;
        }

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
        public static int R(PerkId p) => On ? RoguelikeRun.Rank(p) : 0;
        private static float Pct(int percent, PerkId p) => 1f + percent / 100f * R(p);

        public static float FireIntervalMultiplier
        {
            get
            {
                float m = Mathf.Pow(1f - S.rapidFirePercent / 100f, R(PerkId.RapidFire));
                PlayerController pc = PlayerController.Current;
                if (R(PerkId.LastStand) > 0 && pc != null && pc.Lives == 1) m /= Mathf.Max(1f, S.lastStandFireMultiplier);
                return m;
            }
        }

        /// <summary>Extra Bullet: parallel bullet lines beside the first (0-3, so up to 4 lines).</summary>
        public static int ExtraLines => R(PerkId.ExtraBarrel);
        public static int ShotDamage => 1 + R(PerkId.HeavyShots);
        public static int Pierce => R(PerkId.Piercing);
        public static float SpeedMultiplier => Pct(S.quickFeetPercent, PerkId.QuickFeet);
        public static int ExtraMaxLives => R(PerkId.Vitality);
        public static bool HealOnRoomClear => R(PerkId.FirstAid) > 0;
        public static int ShieldsPerRoom => R(PerkId.Shield);
        public static bool CanRevive => R(PerkId.SecondWind) > 0 && !RoguelikeRun.ReviveUsed;
        public static bool NovaOnHit => R(PerkId.Nova) > 0;
        public static float MagnetMultiplier => Pct(S.magnetPercent, PerkId.Magnet);
        public static float XpMultiplier => Pct(S.wisdomPercent, PerkId.Wisdom);
        public static float VitaminMultiplier => Pct(S.goldenTouchPercent, PerkId.GoldenTouch);
        public static float BonusDurationMultiplier => Pct(S.bonusLoverPercent, PerkId.BonusLover);
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
