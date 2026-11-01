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

        public EnemyDefinition Get(int legacyId)
        {
            foreach (EnemyDefinition e in enemies)
                if (e != null && e.legacyId == legacyId) return e;
            if (warned.Add(legacyId))
                Debug.LogWarning($"EnemyCatalog: enemy type {legacyId} is not ported yet - using {(enemies.Count > 0 ? enemies[0].displayName : "nothing")} instead.");
            return enemies.Count > 0 ? enemies[0] : null;
        }
    }
}
