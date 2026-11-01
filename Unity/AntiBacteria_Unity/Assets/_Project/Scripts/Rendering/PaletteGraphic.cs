using UnityEngine;
using UnityEngine.UI;

namespace AntiBacteria.Rendering
{
    /// <summary>
    /// Colours a UI element (Image, TextMeshPro text, ...) with one palette role and follows
    /// palette changes. Sprites in the world use the palette shader instead.
    /// "Dim" is the one tone outside the 3 colours: the role colour 38 % of the way from the background,
    /// used only for disabled menu items and secondary labels.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(Graphic))]
    public class PaletteGraphic : MonoBehaviour
    {
        public const float DimAmount = 0.38f;

        [SerializeField] private PaletteRole role = PaletteRole.Foreground;
        [SerializeField] private bool dim;

        [Range(0f, 1f)]
        [SerializeField] private float alpha = 1f;

        private Graphic graphic;

        public PaletteRole Role
        {
            get => role;
            set { role = value; Refresh(PaletteManager.Current); }
        }

        public bool Dim
        {
            get => dim;
            set { dim = value; Refresh(PaletteManager.Current); }
        }

        public void Set(PaletteRole newRole, bool newDim)
        {
            role = newRole;
            dim = newDim;
            Refresh(PaletteManager.Current);
        }

        public static Color Resolve(Palette p, PaletteRole role, bool dim)
        {
            Color c = p.Get(role);
            return dim ? Color.Lerp(p.background, c, DimAmount) : c;
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
            if (p == null) return;
            if (graphic == null) graphic = GetComponent<Graphic>();
            if (graphic == null) return;
            Color c = Resolve(p, role, dim);
            c.a = alpha;
            graphic.color = c;
        }
    }
}
