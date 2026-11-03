#if UNITY_EDITOR
using System.Collections;
using System.IO;
using AntiBacteria.Core;
using UnityEngine;

namespace AntiBacteria.Dev
{
    /// <summary>
    /// Editor only: records the Game view at one sample per game pixel, for store GIFs. Game time is locked to the
    /// recording frame rate (Time.captureFramerate), so the clip is smooth however slowly the editor captures.
    /// Writes raw RGB frames to path (row 0 = top) and path + ".txt" with "width height frames fps".
    /// Docs/Steam.md explains how the frames become GIFs.
    /// </summary>
    public class GifRecorder : MonoBehaviour
    {
        private static GifRecorder instance;

        public static bool Recording => instance != null;
        public static int FramesDone { get; private set; }

        private string path;
        private int frames, fps;

        public static void Begin(string path, float seconds, int fps = 25)
        {
            if (instance != null) return;
            var go = new GameObject("GifRecorder");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<GifRecorder>();
            instance.path = path;
            instance.fps = fps;
            instance.frames = Mathf.Max(1, Mathf.RoundToInt(seconds * fps));
            FramesDone = 0;
            Time.captureFramerate = fps;
        }

        public static void Stop()
        {
            if (instance != null) instance.frames = FramesDone;
        }

        private IEnumerator Start()
        {
            int step = PixelScreen.Scale;
            int w = Screen.width / step, h = Screen.height / step;
            byte[] rgb = new byte[w * h * 3];
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
            using (var fs = new FileStream(path, FileMode.Create))
            {
                while (FramesDone < frames)
                {
                    yield return new WaitForEndOfFrame();
                    Texture2D tex = ScreenCapture.CaptureScreenshotAsTexture();
                    Color32[] px = tex.GetPixels32();
                    int tw = tex.width, th = tex.height;
                    Destroy(tex);
                    int ox = (tw - w * step) / 2 + step / 2, oy = (th - h * step) / 2 + step / 2;
                    for (int y = 0; y < h; y++)
                    {
                        int sy = th - 1 - (oy + y * step); // texture rows go bottom-up
                        for (int x = 0; x < w; x++)
                        {
                            Color32 c = px[sy * tw + ox + x * step];
                            int i = (y * w + x) * 3;
                            rgb[i] = c.r; rgb[i + 1] = c.g; rgb[i + 2] = c.b;
                        }
                    }
                    fs.Write(rgb, 0, rgb.Length);
                    FramesDone++;
                }
            }
            File.WriteAllText(path + ".txt", w + " " + h + " " + FramesDone + " " + fps);
            Time.captureFramerate = 0;
            instance = null;
            Destroy(gameObject);
        }
    }
}
#endif
