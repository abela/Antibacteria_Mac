using UnityEngine;

namespace AntiBacteria.Rendering
{
    /// <summary>
    /// One of the game's three-colour palettes (Docs/ArtDirection/StyleGuide.md).
    /// Every sprite is authored in index colours and recoloured from the active palette.
    /// </summary>
    [CreateAssetMenu(menuName = "AntiBacteria/Palette", fileName = "Palette")]
    public class Palette : ScriptableObject
    {
        [Tooltip("Name shown in the settings / palette picker.")]
        public string displayName = "Classic";

        [Tooltip("Index 0: the microscope field.")]
        public Color background = new Color32(0x0b, 0x0b, 0x10, 0xff);

        [Tooltip("Index 1: player, shots, enemy bodies, walls, neutral effects.")]
        public Color foreground = new Color32(0xf2, 0xf2, 0xf2, 0xff);

        [Tooltip("Index 2: danger and reward - enemy cores, enemy bullets, vitamins, hearts.")]
        public Color accent = new Color32(0xff, 0x3b, 0x3b, 0xff);

        [Tooltip("Readable for red-green colour blindness (offered in accessibility options).")]
        public bool colourBlindSafe;

        public Color Get(PaletteRole role)
        {
            switch (role)
            {
                case PaletteRole.Background: return background;
                case PaletteRole.Accent: return accent;
                default: return foreground;
            }
        }
    }

    public enum PaletteRole
    {
        Background = 0,
        Foreground = 1,
        Accent = 2,
    }
}
