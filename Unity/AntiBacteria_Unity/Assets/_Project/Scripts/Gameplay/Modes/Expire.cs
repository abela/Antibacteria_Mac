using UnityEngine;

namespace AntiBacteria.Gameplay.Modes
{
    /// <summary>Pops an enemy (no reward) after a while, blinking for the last second.</summary>
    public class Expire : MonoBehaviour
    {
        public float Seconds = 8f;
        private float age;
        private SpriteRenderer[] renderers;

        private void Start() => renderers = GetComponentsInChildren<SpriteRenderer>();

        private void Update()
        {
            age += Time.deltaTime;
            float left = Seconds - age;
            if (left <= 1f && renderers != null)
            {
                bool on = Mathf.FloorToInt(left / 0.1f) % 2 == 0;
                foreach (SpriteRenderer r in renderers) if (r != null) r.enabled = on;
            }
            if (left > 0f) return;
            if (GetComponent<Enemies.Enemy>() is Enemies.Enemy e) e.Pop();
            else Destroy(gameObject);
        }
    }
}
