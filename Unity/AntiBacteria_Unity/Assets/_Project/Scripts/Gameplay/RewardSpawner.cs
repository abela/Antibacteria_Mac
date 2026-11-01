using AntiBacteria.Core;
using UnityEngine;

namespace AntiBacteria.Gameplay
{
    /// <summary>Spawns vitamins and kill effects. One per gameplay scene.</summary>
    public class RewardSpawner : MonoBehaviour
    {
        [SerializeField] private GameObject vitaminPrefab;
        [SerializeField] private GameObject popPrefab;
        [Tooltip("Vitamins land in a square of +- this many points around the kill (original VITAMIN_CREATE_RADIUS 80).")]
        [SerializeField] private float scatterPoints = 80f;

        public static RewardSpawner Instance { get; private set; }

        private void Awake() => Instance = this;

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void DropVitamins(Vector2 at, int count)
        {
            if (vitaminPrefab == null) return;
            float s = Legacy.Units(scatterPoints);
            for (int i = 0; i < count; i++)
            {
                Vector2 p = at + new Vector2(Random.Range(-s, s), Random.Range(-s, s));
                if (Arena.Current != null) p = Arena.Current.Clamp(p, 0.5f);
                Instantiate(vitaminPrefab, p, Quaternion.identity);
            }
        }

        public void SpawnPop(Vector2 at)
        {
            if (popPrefab != null) Instantiate(popPrefab, at, Quaternion.identity);
        }
    }
}
