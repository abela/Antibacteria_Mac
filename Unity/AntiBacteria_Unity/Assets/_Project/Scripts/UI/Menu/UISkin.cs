using TMPro;
using UnityEngine;

namespace AntiBacteria.UI.Menu
{
    /// <summary>Everything the menus are drawn with. Created by AntiBacteria > Build > Menus.</summary>
    [CreateAssetMenu(menuName = "AntiBacteria/UI Skin", fileName = "UISkin")]
    public class UISkin : ScriptableObject
    {
        public TMP_FontAsset font;
        [Tooltip("Text size that maps 1 font pixel to 1 game pixel. Titles use 2x.")]
        public int fontSize = 16;
        public int rowHeight = 16;

        [Header("Shapes (white, tinted by palette)")]
        public Sprite pixel;
        public Sprite arrow;        // 3x5, points right
        public Sprite dot;          // 192x1 strip: one pixel, 2 px gap, repeated
        public Sprite blockEmpty;   // 5x7 outline

        [Header("Palette-indexed sprites (drawn with the palette material)")]
        public Material paletteMaterial;
        public Sprite heartFull;
        public Sprite heartEmpty;
        public Sprite[] backdropSprites = new Sprite[0];
    }
}
