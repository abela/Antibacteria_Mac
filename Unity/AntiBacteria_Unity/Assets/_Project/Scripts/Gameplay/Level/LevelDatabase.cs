using UnityEngine;

namespace AntiBacteria.Gameplay.Level
{
    /// <summary>All 50 campaign levels in order. Filled by AntiBacteria > Build > Menus (MenuBuilder).</summary>
    [CreateAssetMenu(menuName = "AntiBacteria/Level Database", fileName = "LevelDatabase")]
    public class LevelDatabase : ScriptableObject
    {
        [SerializeField] private LevelData[] levels = new LevelData[0];

        public int Count => levels.Length;

        /// <summary>Level by campaign number (1-based), or null.</summary>
        public LevelData Get(int number)
        {
            foreach (LevelData l in levels)
                if (l != null && l.number == number) return l;
            return null;
        }

        public bool IsLast(int number) => Get(number + 1) == null;
    }
}
