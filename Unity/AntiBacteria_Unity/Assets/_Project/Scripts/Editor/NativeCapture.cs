using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace AntiBacteria.EditorTools
{
    /// <summary>
    /// Dev tool: renders the current frame (world + all overlay canvases) at the native 320x180 to a PNG,
    /// so screens can be checked pixel for pixel. Works in play mode; restores everything afterwards.
    /// </summary>
    public static class NativeCapture
    {
        public static string Capture(string pngPath)
        {
            Camera cam = Camera.main;
            if (cam == null) return "No main camera";

            Behaviour ppc = null;
            foreach (Behaviour b in cam.GetComponents<Behaviour>())
                if (b.GetType().Name == "PixelPerfectCamera") ppc = b;
            bool ppcWas = ppc != null && ppc.enabled;
            if (ppc != null) ppc.enabled = false;
            float oldSize = cam.orthographicSize;
            cam.orthographicSize = 180f / 2f / 16f;

            var restore = new List<System.Action>();
            foreach (Canvas c in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                if (!c.isRootCanvas || c.renderMode != RenderMode.ScreenSpaceOverlay) continue;
                Canvas canvas = c;
                var scaler = canvas.GetComponent<CanvasScaler>();
                var pixelScaler = canvas.GetComponent<Behaviour>() != null ? canvas.GetComponent("PixelCanvasScaler") as Behaviour : null;
                CanvasScaler.ScaleMode mode = scaler != null ? scaler.uiScaleMode : CanvasScaler.ScaleMode.ConstantPixelSize;
                float factor = scaler != null ? scaler.scaleFactor : 1f;
                bool pixelWas = pixelScaler != null && pixelScaler.enabled;
                if (pixelScaler != null) pixelScaler.enabled = false;
                if (scaler != null && scaler.uiScaleMode == CanvasScaler.ScaleMode.ConstantPixelSize) scaler.scaleFactor = 1f;
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = cam;
                canvas.planeDistance = 1f;
                restore.Add(() =>
                {
                    canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    if (scaler != null) { scaler.uiScaleMode = mode; scaler.scaleFactor = factor; }
                    if (pixelScaler != null) pixelScaler.enabled = pixelWas;
                });
            }

            var rt = new RenderTexture(320, 180, 24) { filterMode = FilterMode.Point };
            cam.targetTexture = rt;
            Canvas.ForceUpdateCanvases();
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(320, 180, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 320, 180), 0, 0);
            tex.Apply();
            RenderTexture.active = null;
            cam.targetTexture = null;
            rt.Release();

            foreach (System.Action r in restore) r();
            cam.orthographicSize = oldSize;
            if (ppc != null) ppc.enabled = ppcWas;

            Directory.CreateDirectory(Path.GetDirectoryName(pngPath));
            File.WriteAllBytes(pngPath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            return pngPath;
        }
    }
}
