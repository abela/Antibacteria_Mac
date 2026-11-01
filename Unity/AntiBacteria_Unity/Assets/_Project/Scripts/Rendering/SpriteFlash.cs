using UnityEngine;

namespace AntiBacteria.Rendering
{
    /// <summary>Hit flash for palette sprites: draws the sprite all in the foreground colour for a moment.</summary>
    public class SpriteFlash : MonoBehaviour
    {
        private static readonly int FlashId = Shader.PropertyToID("_Flash");

        [SerializeField] private SpriteRenderer[] renderers = new SpriteRenderer[0];
        [SerializeField] private float duration = 0.06f;

        private MaterialPropertyBlock block;
        private float until;
        private bool on;

        private void Awake()
        {
            block = new MaterialPropertyBlock();
            if (renderers == null || renderers.Length == 0) renderers = GetComponentsInChildren<SpriteRenderer>();
        }

        public void Flash()
        {
            until = Time.unscaledTime + duration;
            Set(true);
        }

        private void Update()
        {
            if (on && Time.unscaledTime >= until) Set(false);
        }

        private void Set(bool value)
        {
            on = value;
            foreach (SpriteRenderer r in renderers)
            {
                if (r == null) continue;
                r.GetPropertyBlock(block);
                block.SetFloat(FlashId, value ? 1f : 0f);
                r.SetPropertyBlock(block);
            }
        }
    }
}
