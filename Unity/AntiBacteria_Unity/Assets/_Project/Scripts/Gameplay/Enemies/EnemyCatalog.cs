using System.Collections.Generic;
using UnityEngine;

namespace AntiBacteria.Gameplay.Enemies
{
    /// <summary>All enemy types by original ID. Types not ported yet fall back to the first entry (with a warning).</summary>
    [CreateAssetMenu(menuName = "AntiBacteria/Enemy Catalog", fileName = "EnemyCatalog")]
    public class EnemyCatalog : ScriptableObject
    {
        [SerializeField] private List<EnemyDefinition> enemies = new List<EnemyDefinition>();

        private readonly HashSet<int> warned = new HashSet<int>();

        /// <summary>The catalog of the running level (set by the level runner), used by enemies that spawn enemies.</summary>
        public static EnemyCatalog Active { get; set; }

        public IReadOnlyList<EnemyDefinition> All => enemies;

        public bool Has(int legacyId)
        {
            foreach (EnemyDefinition e in enemies)
                if (e != null && e.legacyId == legacyId) return true;
            return false;
        }

        public EnemyDefinition Get(int legacyId)
        {
            foreach (EnemyDefinition e in enemies)
                if (e != null && e.legacyId == legacyId) return e;
            if (warned.Add(legacyId))
                Debug.LogWarning($"EnemyCatalog: enemy type {legacyId} is not ported yet - using {(enemies.Count > 0 ? enemies[0].displayName : "nothing")} instead.");
            return enemies.Count > 0 ? enemies[0] : null;
        }

        /// <summary>Create and initialise one enemy (or chain) of this type.</summary>
        public static GameObject Spawn(EnemyDefinition def, Vector2 position)
        {
            if (def == null || def.prefab == null) return null;
            GameObject go = Instantiate(def.prefab, position, Quaternion.identity);
            go.name = def.displayName;
            go.GetComponent<IEnemyRoot>()?.Init(def);
            return go;
        }

        public static GameObject SpawnById(int legacyId, Vector2 position) =>
            Active != null ? Spawn(Active.Get(legacyId), position) : null;
    }
}
