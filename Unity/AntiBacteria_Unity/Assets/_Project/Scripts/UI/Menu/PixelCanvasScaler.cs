using UnityEngine;
using UnityEngine.UI;

namespace AntiBacteria.UI.Menu
{
    /// <summary>
    /// Scales a canvas by the same WHOLE number as the world camera (PixelScreen: 5x at 1920x1080), so UI and world
    /// pixels are the same size and text stays sharp. Menus are laid out in a 320x180 child centred on screen;
    /// things that belong to the screen edges (HUD, boss bar) anchor to the canvas itself.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(CanvasScaler))]
    public class PixelCanvasScaler : MonoBehaviour
    {
        public const int Width = 320;
        public const int Height = 180;

        private CanvasScaler scaler;
        private int lastW, lastH, lastVersion = -1;

        public static int ScaleFor(int screenW, int screenH) => AntiBacteria.Core.PixelScreen.ScaleFor(screenW, screenH);

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
            if (Screen.width == lastW && Screen.height == lastH && lastVersion == AntiBacteria.Core.PixelScreen.Version) return;
            lastW = Screen.width;
            lastH = Screen.height;
            lastVersion = AntiBacteria.Core.PixelScreen.Version;
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.referencePixelsPerUnit = 16f;
            scaler.scaleFactor = ScaleFor(lastW, lastH);
        }
    }
}
