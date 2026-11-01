using UnityEngine;

namespace AntiBacteria.Gameplay.Enemies
{
    public enum EnemyMovement
    {
        /// <summary>Drift to random points, ignoring the player (original kStateRandomMovement).</summary>
        Wander,
        /// <summary>Fly straight at the player (original kStatePatrol / EnemyAgent.patrol).</summary>
        Homing,
    }

    /// <summary>
    /// Stats for one enemy type, keyed by the original numeric ID used in the level files
    /// (Docs/DesignReference/02, enemy ID table). Values are the original ones in points/seconds.
    /// </summary>
    [CreateAssetMenu(menuName = "AntiBacteria/Enemy Definition", fileName = "Enemy")]
    public class EnemyDefinition : ScriptableObject
    {
        [Tooltip("Original EnemyAgentType value (0 = BlueCorosia ... 26 = Frunctus).")]
        public int legacyId;
        public string displayName = "Enemy";
        public Enemy prefab;

        [Header("Behaviour")]
        public EnemyMovement movement = EnemyMovement.Wander;
        [Tooltip("Original speed in points per second (Box2D m/s x 32).")]
        public float speedPoints = 256f;
        [Tooltip("Wander: pick a new target when this close (original 10 pt).")]
        public float arriveDistancePoints = 10f;
        [Tooltip("Visual spin of the body sprite, degrees per second (0 = none).")]
        public float spinDegreesPerSecond;

        [Header("Life")]
        [Tooltip("Hits to kill.")]
        public int hits = 1;
        [Tooltip("Fade-in before it can hurt or be hurt (original CCFadeIn).")]
        public float fadeInSeconds = 0.1f;

        [Header("Rewards")]
        public int killScore = 50;
        public int vitamins = 2;
    }
}
