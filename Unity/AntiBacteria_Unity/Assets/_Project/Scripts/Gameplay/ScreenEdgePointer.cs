using AntiBacteria.Core;
using AntiBacteria.Rendering;
using UnityEngine;

namespace AntiBacteria.Gameplay
{
    /// <summary>
    /// A blinking accent arrow at the screen edge pointing at this object while it is off screen
    /// (8 directions, turned in 90-degree steps so the pixels stay on the grid). Used by the roguelike gate.
    /// </summary>
    public class ScreenEdgePointer : MonoBehaviour
    {
        public float marginPixels = 10f;
        private SpriteRenderer arrow;
        private float age;

        public static ScreenEdgePointer Attach(GameObject target, Material material, int sortingOrder = 40)
        {
            var p = target.AddComponent<ScreenEdgePointer>();
            var go = new GameObject("EdgeArrow");
            go.transform.SetParent(target.transform, false);
            p.arrow = go.AddComponent<SpriteRenderer>();
            p.arrow.sharedMaterial = material;
            p.arrow.sortingOrder = sortingOrder;
            p.arrow.enabled = false;
            return p;
        }

        private void LateUpdate()
        {
            age += Time.unscaledDeltaTime;
            Camera cam = Camera.main;
            if (arrow == null || cam == null || !cam.orthographic) return;
            float halfH = cam.orthographicSize, halfW = halfH * cam.aspect;
            float m = marginPixels / Legacy.PixelsPerUnit;
            Vector2 c = cam.transform.position;
            Vector2 d = (Vector2)transform.position - c;
            if (Mathf.Abs(d.x) < halfW - 0.2f && Mathf.Abs(d.y) < halfH - 0.2f) { arrow.enabled = false; return; }
            float sx = (halfW - m) / Mathf.Max(0.001f, Mathf.Abs(d.x)), sy = (halfH - m) / Mathf.Max(0.001f, Mathf.Abs(d.y));
            Vector2 p = c + d * Mathf.Min(sx, sy);
            arrow.transform.position = new Vector3(Mathf.Round(p.x * Legacy.PixelsPerUnit) / Legacy.PixelsPerUnit,
                                                   Mathf.Round(p.y * Legacy.PixelsPerUnit) / Legacy.PixelsPerUnit, 0f);
            int oct = Mathf.RoundToInt(Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg / 45f);
            oct = ((oct % 8) + 8) % 8;
            arrow.sprite = PixelShapes.Arrow(oct % 2 == 1, true);
            arrow.transform.rotation = Quaternion.Euler(0f, 0f, (oct / 2) * 90f);
            arrow.enabled = Mathf.FloorToInt(age / 0.25f) % 2 == 0;
        }
    }
}
