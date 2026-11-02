using System.Collections.Generic;
using AntiBacteria.Core;
using AntiBacteria.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace AntiBacteria.EditorTools
{
    /// <summary>
    /// Inspector for the ViewSettings asset, also shown in Project Settings > AntiBacteria > View.
    /// Presets, a 16:9 lock, a preview of what common screens will show, and a warning when the view is bigger than
    /// the arena. Every change applies at once: in play mode through CameraFollow / PixelCanvasScaler, in edit mode
    /// by updating the Pixel Perfect Camera of open gameplay scenes.
    /// </summary>
    [CustomEditor(typeof(ViewSettings))]
    public class ViewSettingsEditor : Editor
    {
        public const string AssetPath = "Assets/_Project/Resources/ViewSettings.asset";

        private static readonly (string name, int w, int h)[] Screens =
        {
            ("1280 x 720", 1280, 720),
            ("1920 x 1080", 1920, 1080),
            ("2560 x 1440", 2560, 1440),
            ("3840 x 2160", 3840, 2160),
            ("Steam Deck 1280 x 800", 1280, 800),
        };

        // Views that divide 1920x1080 (the most common Steam screen) exactly. Others round down to the next
        // whole zoom there and show more than asked (416 or 448 wide both end up showing 480 x 270).
        private static readonly (string label, int w)[] Presets =
        {
            ("320 x 180   (6x at 1080p, the original, closest)", 320),
            ("384 x 216   (5x at 1080p)", 384),
            ("480 x 270   (4x at 1080p, wider than the arena)", 480),
        };

        public static ViewSettings GetOrCreate()
        {
            var s = AssetDatabase.LoadAssetAtPath<ViewSettings>(AssetPath);
            if (s != null) return s;
            if (!AssetDatabase.IsValidFolder("Assets/_Project/Resources")) AssetDatabase.CreateFolder("Assets/_Project", "Resources");
            s = CreateInstance<ViewSettings>();
            AssetDatabase.CreateAsset(s, AssetPath);
            AssetDatabase.SaveAssets();
            ViewSettings.Use(s);
            return s;
        }

        public override void OnInspectorGUI()
        {
            var s = (ViewSettings)target;
            int w = s.ViewWidth, h = s.ViewHeight;
            bool keep = s.Keep16by9;

            EditorGUILayout.HelpBox(
                "How many game pixels the camera shows. Bigger = camera further away and everything smaller on screen. " +
                "The HUD and menus always use the same pixel size as the world.", MessageType.None);
            EditorGUILayout.Space(4);

            EditorGUI.BeginChangeCheck();
            keep = EditorGUILayout.Toggle(new GUIContent("Keep 16:9", "1080p and 4K screens then show exactly this area. " +
                "The width moves in 32 px steps so both sides stay whole, even numbers."), keep);
            if (keep)
            {
                w = EditorGUILayout.IntSlider(new GUIContent("View width", "Game pixels shown across."), w, 192, ViewSettings.MaxWidth);
                using (new EditorGUI.DisabledScope(true))
                    EditorGUILayout.IntField(new GUIContent("View height", "Follows the width (16:9)."), s.ViewHeight);
            }
            else
            {
                w = EditorGUILayout.IntField(new GUIContent("View width", "Game pixels shown across (even)."), w);
                h = EditorGUILayout.IntField(new GUIContent("View height", "Game pixels shown top to bottom (even)."), h);
            }
            if (EditorGUI.EndChangeCheck()) Apply(s, w, keep ? w * 9 / 16 : h, keep);

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Presets", EditorStyles.boldLabel);
            foreach ((string label, int pw) in Presets)
            {
                bool on = s.Keep16by9 && s.ViewWidth == pw;
                using (new EditorGUI.DisabledScope(on))
                    if (GUILayout.Button(on ? label + "   (current)" : label)) Apply(s, pw, pw * 9 / 16, true);
            }

            Vector2 arena = ArenaPixels();
            const float wallShown = 16f; // CameraFollow shows 8 px of wall on each side
            bool Overflows(int vw, int vh) => vw > arena.x + wallShown || vh > arena.y + wallShown;

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("What players see", EditorStyles.boldLabel);
            var overflowing = new List<string>();
            foreach ((string name, int sw, int sh) in Screens)
            {
                int k = PixelScreen.ScaleFor(sw, sh);
                int vw = Mathf.RoundToInt(sw / (float)k), vh = Mathf.RoundToInt(sh / (float)k);
                bool exact = vw == s.ViewWidth && vh == s.ViewHeight;
                bool over = Overflows(vw, vh);
                if (over) overflowing.Add(name);
                EditorGUILayout.LabelField(name, $"{k}x pixels, shows {vw} x {vh}" +
                    (exact ? "" : "  (more than the view)") + (over ? "  - past the walls!" : ""));
            }
            if (overflowing.Count > 0)
                EditorGUILayout.HelpBox(
                    $"On {string.Join(", ", overflowing)} the camera shows more than the arena ({arena.x:0} x {arena.y:0} px), " +
                    "so players see the empty space past the walls. Screens zoom by whole numbers, so a view that doesn't " +
                    "divide the screen exactly shows extra. Use a preset, or make the arena bigger.", MessageType.Warning);

            EditorGUILayout.Space(4);
            EditorGUILayout.HelpBox("Saved in " + AssetPath + " (shipped with the game).", MessageType.None);
        }

        private static void Apply(ViewSettings s, int w, int h, bool keep)
        {
            Undo.RecordObject(s, "Change View Size");
            s.Set(w, h, keep);
            EditorUtility.SetDirty(s);
            AssetDatabase.SaveAssetIfDirty(s);
            SyncSceneCameras();
        }

        /// <summary>Edit mode: keep the Game view of open gameplay scenes in step (play mode syncs by itself).</summary>
        public static void SyncSceneCameras()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            int vw = PixelScreen.ViewWidth, vh = PixelScreen.ViewHeight;
            foreach (PixelPerfectCamera ppc in Object.FindObjectsByType<PixelPerfectCamera>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (ppc.GetComponent<CameraFollow>() == null) continue;
                if (ppc.refResolutionX == vw && ppc.refResolutionY == vh) continue;
                Undo.RecordObject(ppc, "Change View Size");
                ppc.refResolutionX = vw;
                ppc.refResolutionY = vh;
                EditorUtility.SetDirty(ppc);
                EditorSceneManager.MarkSceneDirty(ppc.gameObject.scene);
            }
        }

        private static Vector2 ArenaPixels()
        {
            Arena a = Object.FindFirstObjectByType<Arena>(FindObjectsInactive.Include);
            if (a == null) return new Vector2(432f, 304f);
            return new SerializedObject(a).FindProperty("size").vector2Value * Legacy.PixelsPerUnit;
        }
    }

    /// <summary>Project Settings > AntiBacteria > View, and the AntiBacteria > Settings > View menu item.</summary>
    internal static class ViewSettingsProvider
    {
        private const string SettingsPath = "Project/AntiBacteria/View";
        private static Editor editor;

        [SettingsProvider]
        public static SettingsProvider Create() => new SettingsProvider(SettingsPath, SettingsScope.Project)
        {
            label = "View",
            keywords = new HashSet<string> { "camera", "view", "zoom", "pixel", "resolution", "distance" },
            guiHandler = _ =>
            {
                ViewSettings s = ViewSettingsEditor.GetOrCreate();
                if (editor == null || editor.target != s)
                {
                    if (editor != null) Object.DestroyImmediate(editor);
                    editor = Editor.CreateEditor(s);
                }
                float old = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = 180f;
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Space(10f);
                    using (new EditorGUILayout.VerticalScope()) editor.OnInspectorGUI();
                }
                EditorGUIUtility.labelWidth = old;
            },
        };

        [MenuItem("AntiBacteria/Settings/View...")]
        private static void Open() => SettingsService.OpenProjectSettings(SettingsPath);
    }
}
