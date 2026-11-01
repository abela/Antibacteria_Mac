using UnityEngine;

namespace AntiBacteria.Gameplay.Effects
{
    /// <summary>Plays a few pixel-art frames once, then removes itself (kill pops etc.).</summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class FrameAnimation : MonoBehaviour
    {
        [SerializeField] private Sprite[] frames = new Sprite[0];
        [SerializeField] private float framesPerSecond = 20f;
        [SerializeField] private bool destroyWhenDone = true;

        private SpriteRenderer sr;
        private float t;

        private void Awake()
        {
            sr = GetComponent<SpriteRenderer>();
            if (frames.Length > 0) sr.sprite = frames[0];
        }

        private void Update()
        {
            t += Time.deltaTime;
            int i = Mathf.FloorToInt(t * framesPerSecond);
            if (i >= frames.Length)
            {
                if (destroyWhenDone) Destroy(gameObject);
                return;
            }
            sr.sprite = frames[i];
        }
    }
}
