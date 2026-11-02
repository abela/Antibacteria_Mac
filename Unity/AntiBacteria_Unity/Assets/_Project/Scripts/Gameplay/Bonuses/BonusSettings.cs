using System;
using AntiBacteria.Gameplay.Weapons;
using UnityEngine;

namespace AntiBacteria.Gameplay.Bonuses
{
    /// <summary>The 2013 bonus types (ConfigConstants.h BonusObjectType 0-7; Docs/DesignReference/01, section 8).</summary>
    public enum BonusType { WaterShot, CircleShot, WindmillShot, TornadoShot, Life, Boom, Immortality, DoubleSpeed }

    [Serializable]
    public class BonusInfo
    {
        public BonusType type;
        public WordIDs nameId;
        public Sprite icon;
        [Tooltip("Relative chance of being the type a new pickup starts as (2013: all equal).")]
        [Min(0f)] public float weight = 1f;
    }

    /// <summary>
    /// Everything about bonuses: when they appear, how the pickup behaves, what each one does and for how long.
    /// Edit in Project Settings > AntiBacteria > Bonuses. Lives in Resources so builds read the same values.
    /// Distances in points like the rest of the port (80 points = 1 unit = 16 game pixels).
    /// </summary>
    [CreateAssetMenu(menuName = "AntiBacteria/Bonus Settings", fileName = "BonusSettings")]
    public class BonusSettings : ScriptableObject
    {
        public const string ResourcePath = "BonusSettings";

        [Header("Spawning (campaign, not on boss levels)")]
        [Tooltip("A bonus appears every this many seconds of play (2013: every 20 s).")]
        [Min(1f)] public float spawnIntervalSeconds = 20f;
        [Tooltip("Appears this far from the player (2013: 700-710 pt), moved inside the arena if needed.")]
        public float spawnDistanceMinPoints = 700f;
        public float spawnDistanceMaxPoints = 710f;
        [Tooltip("Never closer than this to the player (2013 bug 24: its 'too close' check compared the wrong vector).")]
        public float minDistanceFromPlayerPoints = 300f;
        [Tooltip("Kept this far inside the walls (2013: 30 pt).")]
        public float edgeMarginPoints = 30f;

        [Header("Pickup")]
        [Tooltip("Wanders between random points at this speed (2013: 4 m/s = 128 pt/s).")]
        public float wanderSpeedPoints = 128f;
        [Tooltip("Disappears after this long (2013: about 10.9 s, the time its fade-out took to reach 70 opacity).")]
        [Min(1f)] public float lifetimeSeconds = 11f;
        [Tooltip("Blinks for this long before disappearing.")]
        public float blinkLastSeconds = 3f;
        [Tooltip("The pickup shows each type in turn and gives whichever it shows (2013 campaign: every 100 frames, 1.67 s).")]
        public bool cycleTypes = true;
        [Min(0.2f)] public float cycleSeconds = 1.6f;

        [Header("Effects")]
        [Tooltip("Weapon, Immortality and Double Speed last this long. 2013: 800 frames (13.3 s at 60 fps). " +
                 "Picking the same kind again restarts the full time (bug 18).")]
        [Min(1f)] public float durationSeconds = 12f;
        [Tooltip("The HUD icon blinks for the last this many seconds.")]
        public float warnSeconds = 2f;
        [Tooltip("Double Speed multiplies movement by this.")]
        public float speedMultiplier = 2f;
        [Tooltip("The cell is drawn this transparent while immortal (2013: opacity 50 of 255).")]
        [Range(0.1f, 1f)] public float immortalAlpha = 0.5f;
        [Tooltip("Boom: enemies within this distance of the player... (2013: 700 pt)")]
        public float boomRadiusPoints = 700f;
        [Tooltip("...lose this many hits, and kills pay their score (2013: 20 hits, no score - bug 27).")]
        [Min(1)] public int boomHits = 20;
        public float boomShakePixels = 5f;
        public float boomShakeSeconds = 0.45f;
        [Range(0, 200)] public int boomSparks = 90;

        [Header("Types")]
        public BonusInfo[] bonuses = Defaults();

        [Header("Art (filled in automatically)")]
        public Material material;
        public Sprite frame;
        public Sprite pixel;

        private static BonusSettings current;

        public static BonusSettings Current
        {
            get
            {
                if (current != null) return current;
                current = Resources.Load<BonusSettings>(ResourcePath);
                if (current == null)
                {
                    current = CreateInstance<BonusSettings>();
                    current.hideFlags = HideFlags.HideAndDontSave;
                }
                return current;
            }
        }

        public static void Use(BonusSettings settings) => current = settings;

        public BonusInfo Info(BonusType type)
        {
            foreach (BonusInfo b in bonuses) if (b != null && b.type == type) return b;
            return null;
        }

        /// <summary>A type picked by weight.</summary>
        public BonusType RandomType()
        {
            float total = 0f;
            foreach (BonusInfo b in bonuses) total += Mathf.Max(0f, b.weight);
            if (total <= 0f) return BonusType.Life;
            float r = UnityEngine.Random.value * total;
            foreach (BonusInfo b in bonuses)
            {
                r -= Mathf.Max(0f, b.weight);
                if (r <= 0f) return b.type;
            }
            return bonuses[bonuses.Length - 1].type;
        }

        /// <summary>Weapon bonuses map to a shot type; the others to null.</summary>
        public static ShotKind? ShotFor(BonusType type)
        {
            switch (type)
            {
                case BonusType.WaterShot: return ShotKind.Water;
                case BonusType.CircleShot: return ShotKind.Circle;
                case BonusType.WindmillShot: return ShotKind.Windmill;
                case BonusType.TornadoShot: return ShotKind.Tornado;
                default: return null;
            }
        }

        public static BonusInfo[] Defaults() => new[]
        {
            new BonusInfo { type = BonusType.WaterShot, nameId = WordIDs.WaterShoot },
            new BonusInfo { type = BonusType.CircleShot, nameId = WordIDs.CircleShoot },
            new BonusInfo { type = BonusType.WindmillShot, nameId = WordIDs.WindmillShoot },
            new BonusInfo { type = BonusType.TornadoShot, nameId = WordIDs.TornadoShoot },
            new BonusInfo { type = BonusType.Life, nameId = WordIDs.Life },
            new BonusInfo { type = BonusType.Boom, nameId = WordIDs.Boom },
            new BonusInfo { type = BonusType.Immortality, nameId = WordIDs.Immortality },
            new BonusInfo { type = BonusType.DoubleSpeed, nameId = WordIDs.DoubleSpeed },
        };
    }
}
