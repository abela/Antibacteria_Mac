using System.IO;
using System.Linq;
using AntiBacteria.Gameplay.Level;
using AntiBacteria.Rendering;
using AntiBacteria.UI.Menu;
using AntiBacteria.UI.Menus;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace AntiBacteria.EditorTools
{
    /// <summary>
    /// Builds the menus: UI sprites, the UI skin, the level database, the MainMenu scene and the in-game menu
    /// canvas in Gameplay. The screens themselves are created at runtime by MainMenuController and
    /// GameplayMenuController, so their layout lives in code. Re-run any time; it replaces what it built.
    /// </summary>
    public static class MenuBuilder
    {
        private const string Root = "Assets/_Project";
        public const string MainMenuScenePath = Root + "/Scenes/MainMenu.unity";
        public const string GameplayScenePath = Root + "/Scenes/Gameplay.unity";
        private const string UiArt = Root + "/Art/UI";
        private const string SkinPath = Root + "/Data/UISkin.asset";
        public const string LevelDatabasePath = Root + "/Data/LevelDatabase.asset";

        [UnityEditor.MenuItem("AntiBacteria/Build/Menus")]
        public static void BuildMenu() => Debug.Log(Build());

        public static string Build()
        {
            var log = new System.Text.StringBuilder("MenuBuilder:\n");
            if (EditorSceneManager.GetActiveScene().isDirty) EditorSceneManager.SaveOpenScenes();

            UISkin skin = BuildSkin(log);
            LevelDatabase db = BuildLevelDatabase(log);
            AssetDatabase.SaveAssets(); // scene switches below unload unsaved in-memory changes
            BuildMainMenuScene(skin, log);
            WireGameplayScene(skin, db, log);
            SetBuildScenes();
            log.AppendLine("Build scenes: MainMenu, Gameplay.");
            AssetDatabase.SaveAssets();
            return log.ToString();
        }

        public static void SetBuildScenes()
        {
            var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>();
            if (File.Exists(MainMenuScenePath)) scenes.Add(new EditorBuildSettingsScene(MainMenuScenePath, true));
            scenes.Add(new EditorBuildSettingsScene(GameplayScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        // ------------------------------------------------------------------ sprites + skin

        private static UISkin BuildSkin(System.Text.StringBuilder log)
        {
            Sprite arrow = WriteSprite("ui_arrow", new[]
            {
                "#..",
                "##.",
                "###",
                "##.",
                "#..",
            });
            Sprite dot = WriteSprite("ui_dot", new[] { string.Concat(Enumerable.Repeat("#..", 64)) }); // 192 px dotted strip
            Sprite blockEmpty = WriteSprite("ui_block_empty", new[]
            {
                "#####",
                "#...#",
                "#...#",
                "#...#",
                "#...#",
                "#...#",
                "#####",
            });

            var skin = AssetDatabase.LoadAssetAtPath<UISkin>(SkinPath);
            if (skin == null)
            {
                skin = ScriptableObject.CreateInstance<UISkin>();
                AssetDatabase.CreateAsset(skin, SkinPath);
            }
            skin.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontSetup.OutputPath);
            skin.fontSize = FontSetup.PointSize;
            skin.rowHeight = 16;
            skin.pixel = S("Sprites/pixel_white");
            skin.arrow = arrow;
            skin.dot = dot;
            skin.blockEmpty = blockEmpty;
            skin.paletteMaterial = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Art/PaletteSprite.mat");
            skin.heartFull = S("Sprites/hud_heart");
            skin.heartEmpty = S("Sprites/hud_heart_empty");
            skin.backdropSprites = new[]
                {
                    S("Sprites/enemy_spiky"), S("Sprites/enemy_big"), S("Sprites/enemy_zigzag"), S("Sprites/vitamin"),
                    S("Sprites/enemy_spiral"), S("Sprites/enemy_gnida"), S("Sprites/enemy_loctopus"), S("Sprites/enemy_zamri"),
                    S("Sprites/enemy_adrenalium"), S("Sprites/enemy_chorux"), S("Sprites/enemy_defender"), S("Sprites/enemy_tentacle"),
                    S("Sprites/enemy_sunux"), S("Sprites/vitamin"),
                }
                .Where(s => s != null).ToArray();
            skin.crawlerHead = S("Sprites/snake_head");
            skin.crawlerSegment = S("Sprites/snake_segment_r2");
            skin.bossMark = S("Sprites/Bosses/eye_boss");
            EditorUtility.SetDirty(skin);
            if (skin.font == null) log.AppendLine("WARNING: font asset missing - run AntiBacteria > UI > Build Pixel Font Asset first.");
            log.AppendLine("UI skin: " + SkinPath);
            return skin;
        }

        private static Sprite S(string relative) => AssetDatabase.LoadAssetAtPath<Sprite>(Root + "/Art/" + relative + ".png");

        /// <summary>Writes a white-on-transparent PNG from an ASCII picture ('#' = pixel) and imports it as a sprite.</summary>
        private static Sprite WriteSprite(string name, string[] rows, TextureWrapMode wrap = TextureWrapMode.Clamp)
        {
            int h = rows.Length, w = rows[0].Length;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    tex.SetPixel(x, h - 1 - y, rows[y][x] == '#' ? Color.white : Color.clear);
            tex.Apply();
            string path = UiArt + "/" + name + ".png";
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer.wrapMode != wrap)
            {
                importer.wrapMode = wrap;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        // ------------------------------------------------------------------ data

        private static LevelDatabase BuildLevelDatabase(System.Text.StringBuilder log)
        {
            var db = AssetDatabase.LoadAssetAtPath<LevelDatabase>(LevelDatabasePath);
            if (db == null)
            {
                db = ScriptableObject.CreateInstance<LevelDatabase>();
                AssetDatabase.CreateAsset(db, LevelDatabasePath);
            }
            LevelData[] levels = AssetDatabase.FindAssets("t:LevelData", new[] { Root + "/Data/Levels" })
                .Select(g => AssetDatabase.LoadAssetAtPath<LevelData>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(l => l != null).OrderBy(l => l.number).ToArray();
            var so = new SerializedObject(db);
            SerializedProperty list = so.FindProperty("levels");
            list.arraySize = levels.Length;
            for (int i = 0; i < levels.Length; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = levels[i];
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(db);
            log.AppendLine($"Level database: {levels.Length} levels.");
            return db;
        }

        // ------------------------------------------------------------------ scenes

        private static void BuildMainMenuScene(UISkin skin, System.Text.StringBuilder log)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            skin = AssetDatabase.LoadAssetAtPath<UISkin>(SkinPath); // NewScene unloaded it

            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = AntiBacteria.Core.PixelScreen.ViewHeight / 2f / 16f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            camGo.transform.position = new Vector3(0f, 0f, -10f);

            var pmGo = new GameObject("PaletteManager");
            var pm = pmGo.AddComponent<PaletteManager>();
            var pso = new SerializedObject(pm);
            Palette[] palettes = AssetDatabase.FindAssets("t:Palette", new[] { Root + "/Data/Palettes" })
                .Select(AssetDatabase.GUIDToAssetPath).OrderBy(p => p)
                .Select(AssetDatabase.LoadAssetAtPath<Palette>).ToArray();
            SerializedProperty pal = pso.FindProperty("palettes");
            pal.arraySize = palettes.Length;
            for (int i = 0; i < palettes.Length; i++) pal.GetArrayElementAtIndex(i).objectReferenceValue = palettes[i];
            pso.ApplyModifiedPropertiesWithoutUndo();

            var rewiredPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Prefabs/RewiredInputManager.prefab");
            if (rewiredPrefab != null) PrefabUtility.InstantiatePrefab(rewiredPrefab);
            else log.AppendLine("WARNING: Rewired Input Manager prefab not found.");

            (MenuNavigator nav, RectTransform root) = MenuCanvas("MenuCanvas", 0);
            var mm = nav.gameObject.AddComponent<MainMenuController>();
            SetRef(mm, "skin", skin);
            SetRef(mm, "navigator", nav);
            SetRef(mm, "root", root);

            EditorSceneManager.SaveScene(scene, MainMenuScenePath);
            log.AppendLine("MainMenu scene: " + MainMenuScenePath);
        }

        private static void WireGameplayScene(UISkin skin, LevelDatabase db, System.Text.StringBuilder log)
        {
            EditorSceneManager.OpenScene(GameplayScenePath, OpenSceneMode.Single);
            // OpenScene unloads unused assets: reload what we wire.
            skin = AssetDatabase.LoadAssetAtPath<UISkin>(SkinPath);
            db = AssetDatabase.LoadAssetAtPath<LevelDatabase>(LevelDatabasePath);

            GameObject old = GameObject.Find("MenuCanvas");
            if (old != null) Object.DestroyImmediate(old);

            var runner = Object.FindFirstObjectByType<CampaignLevelRunner>();
            if (runner != null) SetRef(runner, "database", db);

            (MenuNavigator nav, RectTransform root) = MenuCanvas("MenuCanvas", 10); // above the HUD
            var gm = nav.gameObject.AddComponent<GameplayMenuController>();
            SetRef(gm, "skin", skin);
            SetRef(gm, "navigator", nav);
            SetRef(gm, "root", root);
            SetRef(gm, "runner", runner);
            SetRef(gm, "database", db);

            Scene scene = SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            log.AppendLine("Gameplay scene: menu canvas added" + (runner == null ? " (WARNING: no level runner found)" : "") + ".");
        }

        /// <summary>Overlay canvas at a whole-number pixel scale with a centred 320x180 root.</summary>
        private static (MenuNavigator, RectTransform) MenuCanvas(string name, int sortingOrder)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.pixelPerfect = true;
            canvas.sortingOrder = sortingOrder;
            go.AddComponent<CanvasScaler>();
            go.AddComponent<PixelCanvasScaler>();

            var rootGo = new GameObject("Root", typeof(RectTransform));
            rootGo.layer = go.layer;
            var root = (RectTransform)rootGo.transform;
            root.SetParent(go.transform, false);
            root.anchorMin = root.anchorMax = root.pivot = new Vector2(0.5f, 0.5f);
            root.sizeDelta = new Vector2(PixelCanvasScaler.Width, PixelCanvasScaler.Height);
            root.anchoredPosition = Vector2.zero;

            var nav = go.AddComponent<MenuNavigator>();
            return (nav, root);
        }

        private static void SetRef(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            SerializedProperty p = so.FindProperty(field);
            if (p == null) { Debug.LogError($"MenuBuilder: {target.GetType().Name}.{field} not found"); return; }
            p.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
