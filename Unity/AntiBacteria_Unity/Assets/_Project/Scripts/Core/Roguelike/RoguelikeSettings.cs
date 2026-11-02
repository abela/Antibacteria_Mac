using UnityEngine;

namespace AntiBacteria.Core
{
    /// <summary>
    /// The roguelike run: rooms, XP, perk rarity and strengths, the Germ Buddy. Rooms reuse the 50 campaign levels
    /// (enemies, pacing, bosses). Edit in Project Settings > AntiBacteria > Roguelike. Distances in game pixels,
    /// times in seconds.
    /// </summary>
    [CreateAssetMenu(menuName = "AntiBacteria/Roguelike Settings", fileName = "RoguelikeSettings")]
    public class RoguelikeSettings : ScriptableObject
    {
        public const string ResourcePath = "RoguelikeSettings";

        [Header("Rooms")]
        [Tooltip("Enemies a normal room sends before it can be cleared: base + perRoom x (room - 1).")]
        public int enemiesBase = 10;
        public float enemiesPerRoom = 0.8f;
        [Tooltip("Lives restored when a boss room is cleared.")]
        public int bossRoomHeal = 1;
        [Tooltip("Seconds before the boss of a boss room appears.")]
        public float bossDelaySeconds = 1.5f;

        [Header("XP")]
        [Tooltip("XP for the next level: base + step x (level - 1). Level 1 -> 2 needs 'base'.")]
        public int xpBase = 8;
        public int xpStep = 4;
        [Tooltip("XP orbs dropped by an enemy / by a boss.")]
        public int orbsPerKill = 1;
        public int orbsPerBoss = 15;
        [Tooltip("Orbs fly to the player from this far.")]
        public float orbMagnetPixels = 48f;
        public float orbSpeedPixels = 260f;
        [Tooltip("Perks offered at each level up.")]
        [Range(1, 4)] public int perkChoices = 3;

        [Header("XP orb look")]
        [Tooltip("Diamond radius in pixels: 3 = 7x7 (vitamin size). Boss orbs use the bigger one.")]
        [Range(2, 6)] public int orbRadius = 3;
        [Range(2, 6)] public int bossOrbRadius = 4;
        [Tooltip("Seconds per pulse (the core lights up and the orb twinkles).")]
        public float orbPulseSeconds = 0.8f;
        [Tooltip("Sparks thrown when orbs pop out of a kill.")]
        public int orbBurstSparks = 6;
        [Tooltip("Fading copies left behind while an orb flies to the player.")]
        public bool orbTrail = true;

        [Header("Perk rarity (weights by room; harder rolls as deeper)")]
        [Tooltip("Chance weight of each rarity per card, by room (1-50). Weights are compared, not percents.")]
        public AnimationCurve commonWeight = Linear(75, 65, 52, 40, 30, 22);
        public AnimationCurve rareWeight = Linear(22, 27, 32, 35, 36, 36);
        public AnimationCurve epicWeight = Linear(3, 7, 12, 18, 23, 27);
        public AnimationCurve legendaryWeight = Linear(0, 1, 4, 7, 11, 15);
        [Tooltip("Extra rooms of luck per difficulty (Normal, Hard, Impossible): harder runs roll rarer perks.")]
        public int[] difficultyLuckRooms = { 0, 5, 10 };
        [Tooltip("Rooms of luck per rank of the Lucky perk.")]
        public int luckyRooms = 5;

        [Header("Perk strengths (per rank)")]
        public int rapidFirePercent = 15;
        public int quickFeetPercent = 12;
        public int magnetPercent = 50;
        public int wisdomPercent = 25;
        public int goldenTouchPercent = 50;
        public int bonusLoverPercent = 40;
        public int timeBenderPercent = 50;
        [Tooltip("Extra Bullet: pixels between parallel bullet lines.")]
        public float lineSpacingPixels = 4f;
        public int criticalChancePercent = 10;
        [Tooltip("Big Shots: bullet scale (whole numbers keep the pixels square).")]
        public int bigShotScale = 2;
        public int frostChancePercent = 15;
        [Tooltip("Frost Shots: enemy speed while slowed (0.5 = half) and for how long.")]
        [Range(0.1f, 1f)] public float frostSlow = 0.5f;
        public float frostSeconds = 1.5f;
        [Tooltip("Chain Zap: how far the bolt jumps from the kill.")]
        public float chainZapPixels = 48f;
        [Tooltip("Orbiter: circle radius, turn speed, and seconds before the same enemy can be hit again.")]
        public float orbiterRadiusPixels = 18f;
        public float orbiterDegreesPerSecond = 240f;
        public float orbiterHitCooldown = 0.4f;
        [Tooltip("Vampire: kills per healed life.")]
        public int vampireKills = 40;
        [Tooltip("Last Stand: fire rate multiplier on the last life.")]
        public float lastStandFireMultiplier = 2f;
        [Tooltip("Overdrive: every Nth volley also fires a huge piercing blast, with this damage and scale.")]
        public int overdriveEvery = 10;
        public int overdriveDamage = 3;
        public int overdriveScale = 3;

        [Header("Germ Buddy")]
        [Tooltip("Seconds between the buddy's shots (Buddy Trigger shortens it).")]
        public float buddyFireInterval = 0.6f;
        [Tooltip("It shoots enemies within this range.")]
        public float buddyRangePixels = 120f;
        [Tooltip("How far it floats from the player, and how quickly it catches up (higher = less lag).")]
        public float buddyDistancePixels = 14f;
        public float buddyFollow = 6f;
        public int buddyTriggerPercent = 20;
        [Tooltip("Buddy Punch: extra damage per rank (25 = a quarter of a hit, rolled per shot).")]
        public int buddyPunchPercent = 25;
        [Tooltip("Fetch: the buddy pulls pickups within this range of itself.")]
        public float fetchPixels = 64f;

        [Header("Gate")]
        [Tooltip("Width of the exit at the top of a cleared room.")]
        public float gateWidthPixels = 32f;

        public int XpFor(int level) => Mathf.Max(1, xpBase + xpStep * (level - 1));
        public int EnemiesIn(int room) => Mathf.Max(1, Mathf.RoundToInt(enemiesBase + enemiesPerRoom * (room - 1)));

        /// <summary>Chance weight of a rarity at a (luck-adjusted) room.</summary>
        public float Weight(PerkRarity r, float room)
        {
            AnimationCurve c = r == PerkRarity.Common ? commonWeight : r == PerkRarity.Rare ? rareWeight
                : r == PerkRarity.Epic ? epicWeight : legendaryWeight;
            return c != null && c.length > 0 ? Mathf.Max(0f, c.Evaluate(room)) : 0f;
        }

        public int DifficultyLuck(Difficulty d)
        {
            int i = (int)d;
            return difficultyLuckRooms != null && i >= 0 && i < difficultyLuckRooms.Length ? difficultyLuckRooms[i] : 0;
        }

        /// <summary>A straight-line curve through rooms 1, 10, 20, 30, 40, 50.</summary>
        private static AnimationCurve Linear(params float[] v)
        {
            float[] rooms = { 1f, 10f, 20f, 30f, 40f, 50f };
            var keys = new Keyframe[rooms.Length];
            for (int i = 0; i < rooms.Length; i++) keys[i] = new Keyframe(rooms[i], v[i]);
            for (int i = 0; i < keys.Length; i++)
            {
                float inT = i > 0 ? (keys[i].value - keys[i - 1].value) / (keys[i].time - keys[i - 1].time) : 0f;
                float outT = i < keys.Length - 1 ? (keys[i + 1].value - keys[i].value) / (keys[i + 1].time - keys[i].time) : 0f;
                keys[i].inTangent = i > 0 ? inT : outT;
                keys[i].outTangent = i < keys.Length - 1 ? outT : inT;
            }
            return new AnimationCurve(keys);
        }

        private static RoguelikeSettings current;

        public static RoguelikeSettings Current
        {
            get
            {
                if (current != null) return current;
                current = Resources.Load<RoguelikeSettings>(ResourcePath);
                if (current == null)
                {
                    current = CreateInstance<RoguelikeSettings>();
                    current.hideFlags = HideFlags.HideAndDontSave;
                }
                return current;
            }
        }

        public static void Use(RoguelikeSettings settings) => current = settings;
    }
}
