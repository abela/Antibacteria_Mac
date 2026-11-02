using UnityEngine;

namespace AntiBacteria.UI.Menu
{
    /// <summary>
    /// Keeps a UI element a few pixels from a corner of the whole screen, not of the centred 320x180 menu frame,
    /// whatever the aspect ratio. The element can stay inside its menu screen, so it still shows and hides with it.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform))]
    public class PinToScreenCorner : MonoBehaviour
    {
        [Tooltip("Which corner: (0,0) bottom left, (1,0) bottom right, (0,1) top left, (1,1) top right.")]
        public Vector2 corner = Vector2.zero;
        [Tooltip("Distance from the screen edges, in game pixels.")]
        public Vector2 margin = new Vector2(3f, 2f);

        private RectTransform rt, canvasRect, parentRect;
        private Vector2 lastCanvas, lastParent;

        public static PinToScreenCorner Pin(RectTransform target, Vector2 corner, Vector2 margin)
        {
            var p = target.gameObject.AddComponent<PinToScreenCorner>();
            p.corner = corner;
            p.margin = margin;
            p.Apply();
            return p;
        }

        private void OnEnable() => Apply();
        private void LateUpdate() => Apply();

        private void Apply()
        {
            if (rt == null) rt = (RectTransform)transform;
            parentRect = rt.parent as RectTransform;
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null || parentRect == null) return;
            canvasRect = (RectTransform)canvas.rootCanvas.transform;
            Vector2 c = canvasRect.rect.size, p = parentRect.rect.size;
            if (c == lastCanvas && p == lastParent && rt.anchorMin == corner) return;
            lastCanvas = c;
            lastParent = p;
            rt.anchorMin = rt.anchorMax = corner;
            rt.pivot = corner;
            // The parent is centred on the canvas: step out of it by half the difference, in whole pixels.
            Vector2 outside = new Vector2(Mathf.Floor((c.x - p.x) * 0.5f), Mathf.Floor((c.y - p.y) * 0.5f));
            Vector2 sign = new Vector2(corner.x < 0.5f ? -1f : 1f, corner.y < 0.5f ? -1f : 1f);
            // A 0.5 coordinate means centred on that axis (the bottom-centre prompt bar).
            rt.anchoredPosition = new Vector2(Mathf.Approximately(corner.x, 0.5f) ? margin.x : sign.x * (outside.x - margin.x),
                                              Mathf.Approximately(corner.y, 0.5f) ? margin.y : sign.y * (outside.y - margin.y));
        }
    }
}
