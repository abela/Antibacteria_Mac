using UnityEngine;
using UnityEngine.UI;

namespace AntiBacteria.Rendering
{
    /// <summary>
    /// Colours a UI element (Image, TextMeshPro text, ...) with one palette role and follows
    /// palette changes. Sprites in the world use the palette shader instead.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(Graphic))]
    public class PaletteGraphic : MonoBehaviour
    {
        [SerializeField] private PaletteRole role = PaletteRole.Foreground;

        [Range(0f, 1f)]
        [SerializeField] private float alpha = 1f;

        private Graphic graphic;

        public PaletteRole Role
        {
            get => role;
            set { role = value; Refresh(PaletteManager.Current); }
        }

        private void OnEnable()
        {
            graphic = GetComponent<Graphic>();
            PaletteManager.Changed += Refresh;
            Refresh(PaletteManager.Current);
        }

        private void OnDisable() => PaletteManager.Changed -= Refresh;

        private void OnValidate()
        {
            graphic = GetComponent<Graphic>();
            Refresh(PaletteManager.Current);
        }

        private void Refresh(Palette p)
        {
            if (p == null || graphic == null) return;
            Color c = p.Get(role);
            c.a = alpha;
            graphic.color = c;
        }
    }
}
