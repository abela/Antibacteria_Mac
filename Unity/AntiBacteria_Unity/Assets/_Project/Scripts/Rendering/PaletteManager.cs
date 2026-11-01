using System;
using UnityEngine;

namespace AntiBacteria.Rendering
{
    /// <summary>
    /// Owns the active palette. Writes the three colours as global shader values used by
    /// "AntiBacteria/Palette Sprite", sets the camera background, and tells UI (PaletteGraphic)
    /// to recolour. Runs in the editor too, so scenes show the real colours while editing.
    /// </summary>
    [ExecuteAlways]
    [DefaultExecutionOrder(-1000)]
    public class PaletteManager : MonoBehaviour
    {
        private static readonly int BackgroundId = Shader.PropertyToID("_PaletteBackground");
        private static readonly int ForegroundId = Shader.PropertyToID("_PaletteForeground");
        private static readonly int AccentId = Shader.PropertyToID("_PaletteAccent");

        [Tooltip("All palettes the player can choose from. The first one is the default.")]
        [SerializeField] private Palette[] palettes = new Palette[0];

        [SerializeField] private int currentIndex;

        [Tooltip("Cameras whose background is set to the palette's background. Empty = Camera.main.")]
        [SerializeField] private Camera[] cameras = new Camera[0];

        /// <summary>Raised whenever the active palette changes (UI recolours itself on this).</summary>
        public static event Action<Palette> Changed;

        /// <summary>The palette currently applied, or null before any PaletteManager has run.</summary>
        public static Palette Current { get; private set; }

        public int Count => palettes != null ? palettes.Length : 0;
        public int CurrentIndex => currentIndex;

        private void OnEnable() => Apply();

        private void OnValidate() => Apply();

        public void SetPalette(int index)
        {
            if (Count == 0) return;
            currentIndex = ((index % Count) + Count) % Count;
            Apply();
        }

        public void Next() => SetPalette(currentIndex + 1);

        public void Previous() => SetPalette(currentIndex - 1);

        private static Vector4 ToShader(Color c)
        {
            return QualitySettings.activeColorSpace == ColorSpace.Linear ? (Vector4)c.linear : (Vector4)c;
        }

        private void Apply()
        {
            if (Count == 0) return;
            currentIndex = Mathf.Clamp(currentIndex, 0, Count - 1);
            Palette p = palettes[currentIndex];
            if (p == null) return;

            Current = p;
            // Palette colours are authored in sRGB. Shader globals are NOT converted automatically, so in a
            // linear-colour-space project pass linear values or every colour comes out washed out.
            Shader.SetGlobalVector(BackgroundId, ToShader(p.background));
            Shader.SetGlobalVector(ForegroundId, ToShader(p.foreground));
            Shader.SetGlobalVector(AccentId, ToShader(p.accent));

            if (cameras != null && cameras.Length > 0)
            {
                foreach (Camera c in cameras)
                    if (c != null) c.backgroundColor = p.background;
            }
            else if (Camera.main != null)
            {
                Camera.main.backgroundColor = p.background;
            }

            Changed?.Invoke(p);
        }
    }
}
