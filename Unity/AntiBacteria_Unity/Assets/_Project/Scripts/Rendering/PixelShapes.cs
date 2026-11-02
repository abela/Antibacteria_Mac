using System;
using System.Collections.Generic;
using UnityEngine;

namespace AntiBacteria.Rendering
{
    /// <summary>
    /// Small pixel-art shapes made at runtime (rings, blobs, rods, hairs), white on transparent so the palette
    /// material colours them like any other sprite. Cached: each shape is built once per session.
    /// The pivot is always on a pixel corner, so a shape at a whole-pixel position stays on the pixel grid.
    /// </summary>
    public static class PixelShapes
    {
        public const float PixelsPerUnit = 16f;

        private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();
        private static readonly Color32 White = new Color32(255, 255, 255, 255);
        private static readonly Color32 Red = new Color32(255, 0, 0, 255); // accent in the palette material

        /// <summary>Outline of a circle; dotEvery > 1 keeps every Nth pixel around it.</summary>
        public static Sprite Ring(int radius, int dotEvery = 1) =>
            Get($"ring{radius}_{dotEvery}", () =>
            {
                bool[,] m = Mask(radius, (x, y) => x * x + y * y <= (radius + 0.4f) * (radius + 0.4f));
                return Draw(Outline(m, dotEvery));
            });

        /// <summary>
        /// A soft wobbly cell: outline of r(a) = radius + wobble * sin(lobes * a + phase), one frame per phase step,
        /// with an accent nucleus that drifts a pixel between frames. frames frames make one full wobble.
        /// </summary>
        public static Sprite[] Blob(int radius, int lobes, float wobble, int frames, int seed)
        {
            var result = new Sprite[frames];
            for (int f = 0; f < frames; f++)
            {
                int frame = f;
                result[f] = Get($"blob{radius}_{lobes}_{wobble}_{frames}_{seed}_{f}", () =>
                {
                    float phase = seed * 1.7f + frame * Mathf.PI * 2f / frames;
                    float R = radius + wobble + 1;
                    bool[,] m = Mask(Mathf.CeilToInt(R), (x, y) =>
                    {
                        float a = Mathf.Atan2(y, x);
                        float r = radius + wobble * Mathf.Sin(lobes * a + phase) + 0.5f * wobble * Mathf.Sin((lobes + 1) * a - phase * 2f);
                        return x * x + y * y <= r * r;
                    });
                    Color32[,] px = Outline(m, 1);
                    int c = m.GetLength(0) / 2;
                    int nx = c + Mathf.RoundToInt(Mathf.Cos(phase) * radius * 0.3f), ny = c + Mathf.RoundToInt(Mathf.Sin(phase * 0.5f) * radius * 0.3f);
                    px[nx, ny] = Red;
                    return Draw(px);
                });
            }
            return result;
        }

        /// <summary>
        /// A rod-shaped bacterium swimming right: capsule outline (length x 5) with a wavy flagellum trailing left.
        /// One frame per phase of the tail's wave.
        /// </summary>
        public static Sprite[] Rod(int length, int tail, int frames)
        {
            var result = new Sprite[frames];
            for (int f = 0; f < frames; f++)
            {
                int frame = f;
                result[f] = Get($"rod{length}_{tail}_{frames}_{f}", () =>
                {
                    int w = length + tail + 1, h = 9;
                    var body = new bool[w, h];
                    int x0 = tail, cy = h / 2;
                    for (int x = 0; x < length; x++)
                        for (int y = -2; y <= 2; y++)
                        {
                            bool capEnd = (x == 0 || x == length - 1) && Mathf.Abs(y) == 2; // rounded ends
                            if (!capEnd) body[x0 + x, cy + y] = true;
                        }
                    Color32[,] px = Outline(body, 1);
                    px[x0 + length / 2, cy] = Red;
                    for (int x = 0; x < tail; x++)
                    {
                        float k = (tail - x) / (float)tail; // wave grows towards the tip
                        int y = cy + Mathf.RoundToInt(Mathf.Sin(x * 1.1f - frame * Mathf.PI * 2f / frames) * 1.6f * k);
                        px[x, Mathf.Clamp(y, 0, h - 1)] = White;
                    }
                    return Draw(px);
                });
            }
            return result;
        }

        /// <summary>A vertical line of the given length, pivot under the bottom pixel (hairs along the walls).</summary>
        public static Sprite Hair(int length) =>
            Get("hair" + length, () =>
            {
                var px = new Color32[1, length];
                for (int y = 0; y < length; y++) px[0, y] = White;
                return Draw(px, new Vector2(0.5f, 0f));
            });

        /// <summary>A square of solid pixels (dust, specks).</summary>
        public static Sprite Dot(int size, bool accent = false) =>
            Get($"dot{size}_{accent}", () =>
            {
                var px = new Color32[size, size];
                for (int x = 0; x < size; x++) for (int y = 0; y < size; y++) px[x, y] = accent ? Red : White;
                return Draw(px);
            });

        /// <summary>An L-shaped corner bracket, arms of the given length, opening down-right (rotate for the other corners).</summary>
        public static Sprite Bracket(int arm, bool accent) =>
            Get($"bracket{arm}_{accent}", () =>
            {
                var px = new Color32[arm, arm];
                Color32 c = accent ? Red : White;
                for (int i = 0; i < arm; i++) { px[i, arm - 1] = c; px[0, arm - 1 - i] = c; }
                return Draw(px, new Vector2(0f, 1f)); // pivot on the bracket's outer corner
            });

        /// <summary>Outline of a square (2*half+1 wide); dotEvery > 1 keeps every Nth pixel.</summary>
        public static Sprite SquareRing(int half, int dotEvery = 1, bool accent = false) =>
            Get($"square{half}_{dotEvery}_{accent}", () =>
            {
                int n = 2 * half + 1;
                var px = new Color32[n, n];
                Color32 c = accent ? Red : White;
                int k = 0;
                for (int i = 0; i < n; i++)
                    foreach (Vector2Int p in new[] { new Vector2Int(i, 0), new Vector2Int(i, n - 1), new Vector2Int(0, i), new Vector2Int(n - 1, i) })
                        if (k++ % Mathf.Max(1, dotEvery) == 0) px[p.x, p.y] = c;
                return Draw(px);
            });

        /// <summary>A 7x7 arrow pointing right, or up-right when diagonal (rotate by 90 degrees for the rest).</summary>
        public static Sprite Arrow(bool diagonal, bool accent) =>
            Get($"arrow{diagonal}_{accent}", () =>
            {
                string[] rows = diagonal
                    ? new[] { "..#####", "....###", "...#.##", "..#..##", ".#....#", "#......", "......." }
                    : new[] { "...#...", "....#..", ".....#.", "#######", ".....#.", "....#..", "...#..." };
                var px = new Color32[7, 7];
                Color32 c = accent ? Red : White;
                for (int y = 0; y < 7; y++)
                    for (int x = 0; x < 7; x++)
                        if (rows[y][x] == '#') px[x, 6 - y] = c;
                return Draw(px);
            });

        // ------------------------------------------------------------------ helpers

        private static Sprite Get(string key, Func<Sprite> make)
        {
            if (Cache.TryGetValue(key, out Sprite s) && s != null) return s;
            s = make();
            s.name = key;
            Cache[key] = s;
            return s;
        }

        /// <summary>A (2r+1)^2 mask; inside(x, y) is asked with x, y relative to the centre pixel.</summary>
        private static bool[,] Mask(int r, Func<int, int, bool> inside)
        {
            int n = 2 * r + 3; // a pixel of room around the shape for its outline
            var m = new bool[n, n];
            int c = n / 2;
            for (int x = 0; x < n; x++) for (int y = 0; y < n; y++) m[x, y] = inside(x - c, y - c);
            return m;
        }

        /// <summary>The pixels of the mask that touch the outside (4-neighbourhood); every Nth one in scan order around.</summary>
        private static Color32[,] Outline(bool[,] m, int dotEvery)
        {
            int w = m.GetLength(0), h = m.GetLength(1);
            var px = new Color32[w, h];
            var edge = new List<Vector2Int>();
            for (int x = 0; x < w; x++)
                for (int y = 0; y < h; y++)
                {
                    if (!m[x, y]) continue;
                    bool outside = x == 0 || y == 0 || x == w - 1 || y == h - 1 || !m[x - 1, y] || !m[x + 1, y] || !m[x, y - 1] || !m[x, y + 1];
                    if (outside) edge.Add(new Vector2Int(x, y));
                }
            if (dotEvery > 1)
            {
                Vector2 c = new Vector2((w - 1) * 0.5f, (h - 1) * 0.5f);
                edge.Sort((a, b) => Mathf.Atan2(a.y - c.y, a.x - c.x).CompareTo(Mathf.Atan2(b.y - c.y, b.x - c.x)));
            }
            for (int i = 0; i < edge.Count; i += Mathf.Max(1, dotEvery)) px[edge[i].x, edge[i].y] = White;
            return px;
        }

        private static Sprite Draw(Color32[,] px, Vector2? pivotOverride = null)
        {
            int w = px.GetLength(0), h = px.GetLength(1);
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.DontSave,
            };
            var data = new Color32[w * h];
            for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) data[y * w + x] = px[x, y];
            tex.SetPixels32(data);
            tex.Apply(false, true);
            // Pivot on a pixel corner next to the centre (or as asked), never in the middle of a pixel.
            Vector2 pivot = pivotOverride ?? new Vector2(Mathf.Floor(w * 0.5f) / w, Mathf.Floor(h * 0.5f) / h);
            if (pivotOverride.HasValue) pivot = new Vector2(Mathf.Floor(w * pivot.x) / w, Mathf.Floor(h * pivot.y) / h);
            var s = Sprite.Create(tex, new Rect(0, 0, w, h), pivot, PixelsPerUnit, 0, SpriteMeshType.FullRect);
            s.hideFlags = HideFlags.DontSave;
            return s;
        }
    }
}
