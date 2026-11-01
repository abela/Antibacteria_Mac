using System.IO;
using System.Text;
using AntiBacteria.Rendering;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TextCore;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

namespace AntiBacteria.EditorTools
{
    /// <summary>
    /// Builds the game's TextMeshPro font asset from m6x11cyrillic (Daniel Linssen + Natto Games, free with attribution:
    /// keep both names in the credits). Bitmap (not SDF) at 16 px, point-filtered, static atlas, so it stays pixel-crisp
    /// at the 6x canvas scale and ships without the source TTF. Use font size 16 (or 32, 48) on TMP labels.
    /// Re-running updates the same asset, so references in scenes and prefabs survive.
    /// </summary>
    public static class FontSetup
    {
        public const string SourceFontPath = "Assets/Fonts/m6x11cyrillic.ttf";
        public const string CharacterSetPath = "Assets/Fonts/GameCharacterSet.txt";
        public const string OutputPath = "Assets/_Project/Fonts/m6x11_Pixel.asset";
        public const int PointSize = 16;   // 1 font pixel = 1 game pixel at this size
        private const int AtlasSize = 512;
        private const int Padding = 2;

        // Typographic characters the font lacks, drawn with the nearest glyph it has.
        private static readonly uint[,] Aliases =
        {
            { 0x2019, 0x27 }, { 0x2018, 0x27 },                   // ’ ‘ -> '
            { 0x201C, 0x22 }, { 0x201D, 0x22 }, { 0x201E, 0x22 }, // “ ” „ -> "
            { 0x2013, 0x2D }, { 0x2014, 0x2D },                   // – — -> -
            { 0x00A0, 0x20 },                                     // no-break space -> space
        };

        [MenuItem("AntiBacteria/UI/Build Pixel Font Asset")]
        public static void BuildMenu() => Debug.Log(Build());

        public static string Build()
        {
            var log = new StringBuilder("FontSetup:\n");
            var font = AssetDatabase.LoadAssetAtPath<Font>(SourceFontPath);
            if (font == null) return log.Append("Missing ").Append(SourceFontPath).ToString();

            Directory.CreateDirectory(Path.GetDirectoryName(OutputPath));
            string name = Path.GetFileNameWithoutExtension(OutputPath);
            var asset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(OutputPath);
            if (asset == null)
            {
                asset = TMP_FontAsset.CreateFontAsset(font, PointSize, Padding, GlyphRenderMode.RASTER_HINTED,
                    AtlasSize, AtlasSize, AtlasPopulationMode.Dynamic, false);
                asset.name = name;
                AssetDatabase.CreateAsset(asset, OutputPath);
                asset.atlasTexture.name = name + " Atlas";
                AssetDatabase.AddObjectToAsset(asset.atlasTexture, asset);
                asset.material.name = name + " Material";
                AssetDatabase.AddObjectToAsset(asset.material, asset);
                log.AppendLine("Created " + OutputPath);
            }
            else
            {
                asset.atlasPopulationMode = AtlasPopulationMode.Dynamic;
                asset.ClearFontAssetData(true);
                log.AppendLine("Rebuilt " + OutputPath);
            }

            asset.TryAddCharacters(WantedCharacters(), out string notInFont);
            AddAliases(asset, log);
            asset.atlasPopulationMode = AtlasPopulationMode.Static;
            asset.atlasTexture.filterMode = FilterMode.Point;
            EditorUtility.SetDirty(asset.atlasTexture);
            EditorUtility.SetDirty(asset);

            string missingGame = MissingFromGameSet(asset);
            log.AppendLine($"{asset.characterTable.Count} characters in the atlas.");
            log.AppendLine("Game character set: " + (missingGame.Length == 0 ? "all covered" : "MISSING " + missingGame));

            var settings = new SerializedObject(TMP_Settings.instance);
            settings.FindProperty("m_defaultFontAsset").objectReferenceValue = asset;
            settings.FindProperty("m_defaultFontSize").floatValue = PointSize;
            settings.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(TMP_Settings.instance);
            log.AppendLine("Set as the TMP default font (size 16).");

            AssetDatabase.SaveAssets();
            return log.ToString();
        }

        /// <summary>The game's set plus printable ASCII, Latin-1 and Cyrillic, so new translations rarely need a rebuild.</summary>
        private static string WantedCharacters()
        {
            var sb = new StringBuilder();
            if (File.Exists(CharacterSetPath)) sb.Append(File.ReadAllText(CharacterSetPath).Replace("\r", "").Replace("\n", ""));
            for (int c = 0x20; c <= 0x7E; c++) sb.Append((char)c);
            for (int c = 0xA1; c <= 0xFF; c++) sb.Append((char)c);
            for (int c = 0x400; c <= 0x4FF; c++) sb.Append((char)c);
            sb.Append("…•№€");
            return sb.ToString();
        }

        private static void AddAliases(TMP_FontAsset asset, StringBuilder log)
        {
            asset.ReadFontAssetDefinition();
            for (int i = 0; i < Aliases.GetLength(0); i++)
            {
                uint alias = Aliases[i, 0], source = Aliases[i, 1];
                if (asset.characterLookupTable.ContainsKey(alias)) continue;
                if (!asset.characterLookupTable.TryGetValue(source, out TMP_Character src)) continue;
                asset.characterTable.Add(new TMP_Character(alias, asset, src.glyph));
            }
            asset.ReadFontAssetDefinition();
            log.AppendLine("Aliases added for ’ ‘ “ ” „ – — (drawn as ' \" -).");
        }

        private static string MissingFromGameSet(TMP_FontAsset asset)
        {
            if (!File.Exists(CharacterSetPath)) return "";
            var missing = new StringBuilder();
            foreach (char c in File.ReadAllText(CharacterSetPath))
            {
                if (c == '\r' || c == '\n' || c == '﻿') continue;
                if (!asset.characterLookupTable.ContainsKey(c) && missing.ToString().IndexOf(c) < 0) missing.Append(c);
            }
            return missing.ToString();
        }

        /// <summary>Dev check: renders sample lines in every game language at native 320x180 to a PNG.</summary>
        public static string RenderPreview(string pngPath)
        {
            var asset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(OutputPath);
            var palette = AssetDatabase.LoadAssetAtPath<Palette>("Assets/_Project/Data/Palettes/01_Classic.asset");
            if (asset == null || palette == null) return "Font asset or palette missing";

            var scene = EditorSceneManager.NewPreviewScene();
            try
            {
                var camGo = new GameObject("PreviewCamera");
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(camGo, scene);
                var cam = camGo.AddComponent<Camera>();
                cam.scene = scene;
                cam.orthographic = true;
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = palette.Get(PaletteRole.Background);

                var canvasGo = new GameObject("Canvas", typeof(RectTransform));
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(canvasGo, scene);
                var canvas = canvasGo.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = cam;
                canvas.planeDistance = 1f;
                canvas.pixelPerfect = true;
                var scaler = canvasGo.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(320, 180);

                string[] lines =
                {
                    "ANTIBACTERIA  1-1  x2  12:30",
                    "Get Ready! You don't have enough Vitamins",
                    "Vous n’avez pas assez - S’élancer, l’eau",
                    "Größe, Übung, Bärenstärke: Wählen",
                    "¡Prepárate! Año, niño, canción",
                    "Приготовься! Уровень, жизнь, щит",
                    "ВИТАМИНЫ: 250 × 2  ЁЖ ЩЪ ЭЮЯ",
                };
                for (int i = 0; i < lines.Length; i++)
                {
                    var go = new GameObject("Line" + i, typeof(RectTransform));
                    go.transform.SetParent(canvasGo.transform, false);
                    var rt = (RectTransform)go.transform;
                    rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
                    rt.pivot = new Vector2(0, 1);
                    rt.anchoredPosition = new Vector2(8, -8 - i * 22);
                    rt.sizeDelta = new Vector2(304, 20);
                    var t = go.AddComponent<TextMeshProUGUI>();
                    t.font = asset;
                    t.fontSize = PointSize;
                    t.enableWordWrapping = false;
                    t.color = palette.Get(i == 0 ? PaletteRole.Accent : PaletteRole.Foreground);
                    t.text = lines[i];
                }

                var target = new RenderTexture(320, 180, 24) { filterMode = FilterMode.Point };
                cam.targetTexture = target;
                Canvas.ForceUpdateCanvases();
                foreach (var t in canvasGo.GetComponentsInChildren<TextMeshProUGUI>()) t.ForceMeshUpdate();
                Canvas.ForceUpdateCanvases();
                cam.Render();
                RenderTexture.active = target;
                var tex = new Texture2D(320, 180, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, 320, 180), 0, 0);
                tex.Apply();
                RenderTexture.active = null;
                cam.targetTexture = null;
                File.WriteAllBytes(pngPath, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                target.Release();
                return "Preview written to " + pngPath;
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }
    }
}
