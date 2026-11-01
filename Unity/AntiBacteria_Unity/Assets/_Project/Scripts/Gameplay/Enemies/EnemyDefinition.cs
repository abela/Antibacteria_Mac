using UnityEngine;

namespace AntiBacteria.Gameplay.Enemies
{
    public enum EnemyMovement
    {
        /// <summary>Drift to random points, ignoring the player (original kStateRandomMovement).</summary>
        Wander,
        /// <summary>Fly straight at the player (original kStatePatrol / EnemyAgent.patrol).</summary>
        Homing,
        /// <summary>Homing plus a rotating velocity: big loops toward the player (SpiralCorosia, defenders, petals).</summary>
        Spiral,
        /// <summary>Homing plus a side-to-side wobble (ZigZagus).</summary>
        ZigZag,
        /// <summary>Dash between points, half of them the player's position at that moment (Adrenalium).</summary>
        Dash,
        /// <summary>Never moves (Sunux).</summary>
        Stationary,
        /// <summary>Moved by its owner (chain segments).</summary>
        Controlled,
        /// <summary>A chain of segments: the head roams, the body follows its path (Serpentus family).</summary>
        Chain,
    }

    public enum ShotPattern { None, Radial, Aimed }

    /// <summary>What a chain does when its head dies.</summary>
    public enum ChainDeath
    {
        /// <summary>The next segment becomes the head (Serpentus, ShootingSerpentus).</summary>
        PromoteNext,
        /// <summary>The whole chain pops (CaterPillar).</summary>
        KillAll,
        /// <summary>The body segments break free as separate spiralling enemies (LotusBacterius).</summary>
        Bloom,
    }

    /// <summary>
    /// Stats and behaviour for one enemy type, keyed by the original numeric ID used in the level files
    /// (Docs/DesignReference/02 and 03). Speeds are the original values in points/second (Box2D m/s x 32).
    /// Built by AntiBacteria > Build > Enemies (EnemyBuilder), which documents where each number comes from.
    /// </summary>
    [CreateAssetMenu(menuName = "AntiBacteria/Enemy Definition", fileName = "Enemy")]
    public class EnemyDefinition : ScriptableObject
    {
        [Tooltip("Original EnemyAgentType value (0 = BlueCorosia ... 26 = Frunctus).")]
        public int legacyId;
        public string displayName = "Enemy";
        [Tooltip("Root object: has an Enemy (or ChainEnemy) component.")]
        public GameObject prefab;

        [Header("Movement")]
        public EnemyMovement movement = EnemyMovement.Wander;
        [Tooltip("Points per second (Box2D m/s x 32).")]
        public float speedPoints = 256f;
        [Tooltip("Wander / Dash: pick a new target when this close (original 10 pt).")]
        public float arriveDistancePoints = 10f;
        [Tooltip("Visual spin of the body sprite, degrees per second (0 = none).")]
        public float spinDegreesPerSecond;

        [Header("Spiral (orbit added to homing; re-rolled after each full turn)")]
        public float orbitSpeedPointsMin = 320f;
        public float orbitSpeedPointsMax = 640f;
        [Tooltip("Original: 1-3 degrees per update, updated ~120 times a second.")]
        public float orbitDegreesPerSecondMin = 120f;
        public float orbitDegreesPerSecondMax = 360f;

        [Header("ZigZag (wobble across the heading)")]
        public float wobbleSpeedPoints = 640f;
        public float wobbleDegreesPerSecond = 600f;

        [Header("Dash")]
        public float dashSpeedPointsMin = 576f;
        public float dashSpeedPointsMax = 800f;
        [Range(0f, 1f)] public float dashPlayerChance = 0.5f;

        [Header("Dodge (sidestep while the player aims at it and fires)")]
        public bool dodgeWhenAimedAt;
        public float dodgeSpeedPoints = 512f;
        public float dodgeConeDegrees = 30f;

        [Header("Look")]
        [Tooltip("How far the red eye slides toward the player, in pixels (0 = no eye).")]
        public int eyeOffsetPixels;

        [Header("Life")]
        [Tooltip("Hits to kill.")]
        public int hits = 1;
        [Tooltip("Fade-in before it can hurt or be hurt (original CCFadeIn). 0 = active at once.")]
        public float fadeInSeconds = 0.1f;
        [Tooltip("Shots stop on it but never hurt it (Tentacle).")]
        public bool invulnerable;

        [Header("Shooting")]
        public ShotPattern shotPattern = ShotPattern.None;
        public EnemyBullet bulletPrefab;
        public float shotIntervalSeconds = 2f;
        [Tooltip("Radial: number of bullets per burst.")]
        public int shotCount = 4;
        [Tooltip("Radial: the burst turns with the spinning sprite (nozzles and bullets line up).")]
        public bool shotsFollowSpin = true;
        [Tooltip("Bullets appear this far from the centre.")]
        public float shotOffsetPoints = 80f;
        [Tooltip("Original enemy bullet: 30 m/s.")]
        public float shotSpeedPoints = 960f;

        [Header("Spawning children (Spawner, Chorux)")]
        public float spawnIntervalSeconds;
        public int[] spawnIds = new int[0];
        [Tooltip("Live children at most (the original had no cap).")]
        public int maxChildren = 8;

        [Header("Chain")]
        public int chainSegments = 6;
        [Tooltip("Stats of each segment (hits, score, vitamins); the segment prefab comes from its prefab field.")]
        public EnemyDefinition segmentDefinition;
        [Tooltip("Hits of the first head only (Lotus: 10). 0 = segment value.")]
        public int headHits;
        [Tooltip("Distance between segments along the path (original ~36 pt at speed 10).")]
        public float segmentSpacingPoints = 36f;
        public ChainDeath chainDeath = ChainDeath.PromoteNext;
        [Tooltip("Bloom: what the freed segments become.")]
        public EnemyDefinition bloomDefinition;
        public Sprite headSprite;
        [Tooltip("Body sprites from just behind the head to the tail (the chain picks evenly).")]
        public Sprite[] bodySprites = new Sprite[0];
        [Tooltip("Direction the head sprite faces, in degrees (180 = left). -1 = never rotate.")]
        public float headFacingDegrees = 180f;

        [Header("Difficulty overrides (Normal, Hard, Impossible); leave empty to use the values above")]
        public float[] speedPointsByDifficulty = new float[0];
        public float[] shotIntervalByDifficulty = new float[0];
        [Tooltip("Inferno: tentacles. Anakonda: unused.")]
        public int[] countByDifficulty = new int[0];
        [Tooltip("Anakonda: seconds between new tail segments.")]
        public float[] growIntervalByDifficulty = new float[0];

        [Header("Boss")]
        public bool isBoss;
        [Tooltip("Body sprites from healthy to nearly dead; the boss erodes as it loses hits.")]
        public Sprite[] damageStages = new Sprite[0];

        [Header("Rewards")]
        public int killScore = 50;
        public int vitamins = 2;

        /// <summary>The value for the current difficulty, or the fallback when no per-difficulty values are set.</summary>
        public static float ForDifficulty(float[] values, float fallback)
        {
            int d = (int)Core.GameSession.Difficulty;
            return values != null && values.Length > d ? values[d] : fallback;
        }

        public static int ForDifficulty(int[] values, int fallback)
        {
            int d = (int)Core.GameSession.Difficulty;
            return values != null && values.Length > d ? values[d] : fallback;
        }
    }
}
