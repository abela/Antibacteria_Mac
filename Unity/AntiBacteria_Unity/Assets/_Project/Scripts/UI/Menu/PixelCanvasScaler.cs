using UnityEngine;
using UnityEngine.UI;

namespace AntiBacteria.UI.Menu
{
    /// <summary>
    /// Scales a 320x180 canvas by the largest WHOLE number that fits the screen (6x at 1920x1080, 4x at 1280x720,
    /// 8x at 2560x1440), so every game pixel is the same size and text stays sharp. The content lives in a
    /// 320x180 child centred on screen; any leftover border shows the camera background.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(CanvasScaler))]
    public class PixelCanvasScaler : MonoBehaviour
    {
        public const int Width = 320;
        public const int Height = 180;

        private CanvasScaler scaler;
        private int lastW, lastH;

        public static int ScaleFor(int screenW, int screenH) =>
            Mathf.Max(1, Mathf.Min(screenW / Width, screenH / Height));

        private void OnEnable()
        {
            scaler = GetComponent<CanvasScaler>();
            lastW = lastH = 0;
            Apply();
        }

        private void Update() => Apply();

        private void Apply()
        {
            if (scaler == null) return;
            if (Screen.width == lastW && Screen.height == lastH) return;
            lastW = Screen.width;
            lastH = Screen.height;
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.referencePixelsPerUnit = 16f;
            scaler.scaleFactor = ScaleFor(lastW, lastH);
        }
    }
}
