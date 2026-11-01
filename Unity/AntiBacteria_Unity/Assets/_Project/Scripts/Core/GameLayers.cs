using UnityEngine;

namespace AntiBacteria.Core
{
    /// <summary>Physics layers. Created and wired (collision matrix) by AntiBacteria > Build > Vertical Slice.</summary>
    public static class GameLayers
    {
        public const string Player = "Player";
        public const string PlayerShot = "PlayerShot";
        public const string Enemy = "Enemy";
        public const string EnemyShot = "EnemyShot";
        public const string Pickup = "Pickup";
        public const string Wall = "Wall";

        public static readonly string[] All = { Player, PlayerShot, Enemy, EnemyShot, Pickup, Wall };

        /// <summary>Which layers touch. Everything else is ignored - the original had no filtering at all,
        /// so every bullet hurt the player and bullets cancelled each other (Docs: QUIRK).</summary>
        public static readonly string[,] Pairs =
        {
            { Player, Enemy }, { Player, EnemyShot }, { Player, Pickup }, { Player, Wall },
            { PlayerShot, Enemy }, { PlayerShot, Wall }, { PlayerShot, EnemyShot },
            { EnemyShot, Wall },
        };

        public static int Get(string name) => LayerMask.NameToLayer(name);
    }
}
