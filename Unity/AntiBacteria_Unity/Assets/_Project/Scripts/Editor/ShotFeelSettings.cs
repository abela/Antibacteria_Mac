using System.Collections.Generic;
using AntiBacteria.Gameplay.Effects;
using UnityEditor;
using UnityEngine;

namespace AntiBacteria.EditorTools
{
    /// <summary>
    /// Project Settings > AntiBacteria > Shooting Feel (and AntiBacteria > Settings > Shooting Feel...):
    /// every number for shot spread, recoil, camera kick, hit sparks, knockback and kill shake. Edits apply at once,
    /// also while playing, and are saved straight to the asset.
    /// </summary>
    internal static class ShotFeelSettings
    {
        public const string AssetPath = "Assets/_Project/Resources/ShotFeel.asset";
        private const string SettingsPath = "Project/AntiBacteria/Shooting Feel";
        private const string Art = "Assets/_Project/Art";

        private static Editor editor;

        public static ShotFeel GetOrCreate()
        {
            var feel = AssetDatabase.LoadAssetAtPath<ShotFeel>(AssetPath);
            if (feel == null)
            {
                if (!AssetDatabase.IsValidFolder("Assets/_Project/Resources")) AssetDatabase.CreateFolder("Assets/_Project", "Resources");
                feel = ScriptableObject.CreateInstance<ShotFeel>();
                AssetDatabase.CreateAsset(feel, AssetPath);
                ShotFeel.Use(feel);
            }
            if (feel.material == null || feel.sparkForeground == null) FillArt(feel);
            return feel;
        }

        /// <summary>Point the asset at the effect sprites (Art/Sprites/Fx) and the palette material.</summary>
        public static void FillArt(ShotFeel feel)
        {
            Undo.RecordObject(feel, "Fill Shooting Feel Art");
            feel.material = AssetDatabase.LoadAssetAtPath<Material>(Art + "/PaletteSprite.mat");
            feel.shotBirth = FxSprite("fx_shot_birth");
            feel.shotTrail = FxSprite("fx_shot_trail");
            feel.muzzleFrames = Frames("fx_muzzle_");
            feel.hitFrames = Frames("fx_hit_");
            feel.wallFrames = Frames("fx_wall_");
            feel.sparkForeground = FxSprite("fx_px_fg");
            feel.sparkAccent = FxSprite("fx_px_accent");
            feel.sparkBig = FxSprite("fx_px_fg2");
            EditorUtility.SetDirty(feel);
            AssetDatabase.SaveAssetIfDirty(feel);
        }

        private static Sprite FxSprite(string name) => AssetDatabase.LoadAssetAtPath<Sprite>($"{Art}/Sprites/Fx/{name}.png");

        private static Sprite[] Frames(string prefix)
        {
            var list = new List<Sprite>();
            for (int i = 0; ; i++)
            {
                Sprite s = FxSprite(prefix + i);
                if (s == null) break;
                list.Add(s);
            }
            return list.ToArray();
        }

        [SettingsProvider]
        public static SettingsProvider Create() => new SettingsProvider(SettingsPath, SettingsScope.Project)
        {
            label = "Shooting Feel",
            keywords = new HashSet<string> { "shoot", "shot", "bullet", "recoil", "shake", "spark", "juice", "knockback", "muzzle" },
            guiHandler = _ =>
            {
                ShotFeel feel = GetOrCreate();
                if (editor == null || editor.target != feel)
                {
                    if (editor != null) Object.DestroyImmediate(editor);
                    editor = Editor.CreateEditor(feel);
                }
                float old = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = 220f;
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Space(10f);
                    using (new EditorGUILayout.VerticalScope())
                    {
                        EditorGUILayout.HelpBox("How shooting feels: spread, recoil, camera kick, sparks, knockback and shake. " +
                            "Distances are in game pixels, speeds in pixels per second. Changes apply at once, also while playing.",
                            MessageType.None);
                        EditorGUI.BeginChangeCheck();
                        editor.OnInspectorGUI();
                        if (EditorGUI.EndChangeCheck()) AssetDatabase.SaveAssetIfDirty(feel);
                        EditorGUILayout.Space(6);
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            if (GUILayout.Button("Reset to defaults"))
                            {
                                Undo.RecordObject(feel, "Reset Shooting Feel");
                                var fresh = ScriptableObject.CreateInstance<ShotFeel>();
                                EditorUtility.CopySerialized(fresh, feel);
                                Object.DestroyImmediate(fresh);
                                FillArt(feel);
                            }
                            if (GUILayout.Button("Re-link effect sprites")) FillArt(feel);
                        }
                    }
                }
                EditorGUIUtility.labelWidth = old;
            },
        };

        [MenuItem("AntiBacteria/Settings/Shooting Feel...")]
        private static void Open() => SettingsService.OpenProjectSettings(SettingsPath);
    }
}
