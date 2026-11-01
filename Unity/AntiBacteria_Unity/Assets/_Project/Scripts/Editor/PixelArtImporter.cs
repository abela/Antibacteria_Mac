using UnityEditor;
using UnityEngine;

namespace AntiBacteria.EditorTools
{
    /// <summary>
    /// Every texture under Assets/_Project/Art is pixel art: imported as a Sprite at 16 pixels per
    /// unit, point filtering, no compression and no mipmaps, so it stays crisp at any whole-number
    /// scale (Docs/ArtDirection/StyleGuide.md). Applies on first import; later manual changes stick.
    /// </summary>
    public class PixelArtImporter : AssetPostprocessor
    {
        private const string ArtRoot = "Assets/_Project/Art/";
        public const float PixelsPerUnit = 16f;

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(ArtRoot)) return;
            var importer = (TextureImporter)assetImporter;
            if (!importer.importSettingsMissing) return; // only on first import

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = PixelsPerUnit;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteExtrude = 0;
            settings.spriteAlignment = (int)SpriteAlignment.Center;
            importer.SetTextureSettings(settings);
        }
    }
}
