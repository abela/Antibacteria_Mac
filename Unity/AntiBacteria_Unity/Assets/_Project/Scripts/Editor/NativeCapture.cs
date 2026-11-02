using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace AntiBacteria.EditorTools
{
    /// <summary>
    /// Dev tool: renders the current frame (world + all overlay canvases) at the native view size to a PNG,
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
            // What the player actually sees: the whole-number zoom for this screen can show MORE than the
            // configured view (a 416-wide view on a 1920-wide screen zooms 4x and shows 480 px).
            int zoom = AntiBacteria.Core.PixelScreen.ScaleFor(cam.pixelWidth, cam.pixelHeight);
            int vw = Mathf.Max(1, cam.pixelWidth / zoom), vh = Mathf.Max(1, cam.pixelHeight / zoom);
            cam.orthographicSize = vh / 2f / 16f;

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
                // Overlay canvases always draw over the world; in camera space they sort against sprites,
                // so lift them above everything to match what the player sees.
                int oldLayer = canvas.sortingLayerID, oldOrder = canvas.sortingOrder;
                SortingLayer[] layers = SortingLayer.layers;
                canvas.sortingLayerID = layers[layers.Length - 1].id;
                canvas.sortingOrder = 30000 + oldOrder;
                restore.Add(() =>
                {
                    canvas.sortingLayerID = oldLayer;
                    canvas.sortingOrder = oldOrder;
                    canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    if (scaler != null) { scaler.uiScaleMode = mode; scaler.scaleFactor = factor; }
                    if (pixelScaler != null) pixelScaler.enabled = pixelWas;
                });
            }

            var rt = new RenderTexture(vw, vh, 24) { filterMode = FilterMode.Point };
            cam.targetTexture = rt;
            Canvas.ForceUpdateCanvases();
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(vw, vh, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, vw, vh), 0, 0);
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

        private static readonly List<(float time, string path)> pending = new List<(float, string)>();
        public static readonly List<string> Done = new List<string>();

        /// <summary>
        /// Capture after a number of game seconds (play mode). Editor tool calls take seconds of their own, which is
        /// too slow to catch a half-second attack; this fires from the editor loop at the right moment.
        /// </summary>
        public static string CaptureAt(float gameSecondsFromNow, string pngPath)
        {
            if (pending.Count == 0) UnityEditor.EditorApplication.update += Tick;
            pending.Add((Time.time + gameSecondsFromNow, pngPath));
            return "scheduled " + pngPath;
        }

        private static void Tick()
        {
            if (!Application.isPlaying) { pending.Clear(); UnityEditor.EditorApplication.update -= Tick; return; }
            for (int i = pending.Count - 1; i >= 0; i--)
            {
                if (Time.time < pending[i].time) continue;
                Done.Add(pending[i].path + " @" + Time.time.ToString("0.00"));
                Capture(pending[i].path);
                pending.RemoveAt(i);
            }
            if (pending.Count == 0) UnityEditor.EditorApplication.update -= Tick;
        }
    }
}
