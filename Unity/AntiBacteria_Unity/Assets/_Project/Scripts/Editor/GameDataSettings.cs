using System;
using System.Collections.Generic;
using AntiBacteria.Core;
using AntiBacteria.Gameplay.Bonuses;
using AntiBacteria.Gameplay.Modes;
using AntiBacteria.Platform;
using AntiBacteria.Gameplay.Weapons;
using UnityEditor;
using UnityEngine;

namespace AntiBacteria.EditorTools
{
    /// <summary>
    /// Project Settings pages for the game-data assets that live in Resources:
    /// AntiBacteria > Weapons (shot types, bullets per volley) and AntiBacteria > Bonuses (drops, effects, durations).
    /// Edits apply at once, also while playing, and are saved straight to the asset.
    /// </summary>
    internal static class GameDataSettings
    {
        private const string Resources = "Assets/_Project/Resources";
        private const string Art = "Assets/_Project/Art";

        // ------------------------------------------------------------------ assets

        public static WeaponSettings Weapons() => GetOrCreate<WeaponSettings>(Resources + "/WeaponSettings.asset", WeaponSettings.Use, null);

        public static ShopSettings Shop() => GetOrCreate<ShopSettings>(Resources + "/ShopSettings.asset", ShopSettings.Use, null);

        public static BonusSettings Bonuses() =>
            GetOrCreate<BonusSettings>(Resources + "/BonusSettings.asset", BonusSettings.Use, b => b.frame == null || b.material == null, FillBonusArt);

        private static T GetOrCreate<T>(string path, Action<T> use, Func<T, bool> needsArt, Action<T> fillArt = null) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                if (!AssetDatabase.IsValidFolder(Resources)) AssetDatabase.CreateFolder("Assets/_Project", "Resources");
                asset = ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(asset, path);
                use(asset);
            }
            if (fillArt != null && needsArt != null && needsArt(asset)) fillArt(asset);
            return asset;
        }

        /// <summary>Palette material, the pickup frame and each type's icon (Art/Sprites/Bonus).</summary>
        public static PlatformSettings PlatformCfg() =>
            GetOrCreate<PlatformSettings>(Resources + "/PlatformSettings.asset", PlatformSettings.Use, null);

        public static ModeSettings Modes() =>
            GetOrCreate<ModeSettings>(Resources + "/ModeSettings.asset", ModeSettings.Use, m => m.flag == null || m.material == null, FillModeArt);

        public static void FillModeArt(ModeSettings s)
        {
            Undo.RecordObject(s, "Link Mode Art");
            s.material = AssetDatabase.LoadAssetAtPath<Material>(Art + "/PaletteSprite.mat");
            s.flag = LoadSprite("Modes/mode_flag");
            s.peaceFlag = LoadSprite("Modes/mode_peace_flag");
            s.dot = LoadSprite("Modes/mode_dot");
            s.dotFilled = LoadSprite("Modes/mode_dot_lit");
            EditorUtility.SetDirty(s);
            AssetDatabase.SaveAssetIfDirty(s);
        }

        public static void FillBonusArt(BonusSettings s)
        {
            Undo.RecordObject(s, "Link Bonus Art");
            s.material = AssetDatabase.LoadAssetAtPath<Material>(Art + "/PaletteSprite.mat");
            s.frame = LoadSprite("Bonus/bonus_frame");
            s.pixel = LoadSprite("pixel_white");
            var icons = new Dictionary<BonusType, string>
            {
                { BonusType.WaterShot, "bonus_water" }, { BonusType.CircleShot, "bonus_circle" },
                { BonusType.WindmillShot, "bonus_windmill" }, { BonusType.TornadoShot, "bonus_tornado" },
                { BonusType.Life, "bonus_life" }, { BonusType.Boom, "bonus_boom" },
                { BonusType.Immortality, "bonus_immortality" }, { BonusType.DoubleSpeed, "bonus_speed" },
            };
            foreach (BonusInfo b in s.bonuses)
                if (b != null && icons.TryGetValue(b.type, out string file)) b.icon = LoadSprite("Bonus/" + file);
            EditorUtility.SetDirty(s);
            AssetDatabase.SaveAssetIfDirty(s);
        }

        private static Sprite LoadSprite(string name) => AssetDatabase.LoadAssetAtPath<Sprite>($"{Art}/Sprites/{name}.png");

        // ------------------------------------------------------------------ pages

        private static readonly Dictionary<string, Editor> Editors = new Dictionary<string, Editor>();

        private static SettingsProvider Page<T>(string path, string label, string help, Func<T> get, HashSet<string> keywords,
            Action<T> extraButtons = null) where T : ScriptableObject
        {
            return new SettingsProvider(path, SettingsScope.Project)
            {
                label = label,
                keywords = keywords,
                guiHandler = _ =>
                {
                    T asset = get();
                    if (!Editors.TryGetValue(path, out Editor editor) || editor == null || editor.target != asset)
                    {
                        if (editor != null) UnityEngine.Object.DestroyImmediate(editor);
                        editor = Editor.CreateEditor(asset);
                        Editors[path] = editor;
                    }
                    float old = EditorGUIUtility.labelWidth;
                    EditorGUIUtility.labelWidth = 220f;
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.Space(10f);
                        using (new EditorGUILayout.VerticalScope())
                        {
                            EditorGUILayout.HelpBox(help, MessageType.None);
                            EditorGUI.BeginChangeCheck();
                            editor.OnInspectorGUI();
                            if (EditorGUI.EndChangeCheck()) AssetDatabase.SaveAssetIfDirty(asset);
                            EditorGUILayout.Space(6);
                            using (new EditorGUILayout.HorizontalScope())
                            {
                                if (GUILayout.Button("Reset to defaults"))
                                {
                                    Undo.RecordObject(asset, "Reset " + label);
                                    T fresh = ScriptableObject.CreateInstance<T>();
                                    EditorUtility.CopySerialized(fresh, asset);
                                    UnityEngine.Object.DestroyImmediate(fresh);
                                    EditorUtility.SetDirty(asset);
                                    get(); // re-links art where needed
                                    AssetDatabase.SaveAssetIfDirty(asset);
                                }
                                extraButtons?.Invoke(asset);
                            }
                        }
                    }
                    EditorGUIUtility.labelWidth = old;
                },
            };
        }

        [SettingsProvider]
        public static SettingsProvider WeaponsPage() => Page(
            "Project/AntiBacteria/Weapons", "Weapons",
            "The player's shot types and how many bullets a volley has at each campaign level. " +
            "Rates are in game time. Changes apply at once, also while playing.",
            Weapons, new HashSet<string> { "weapon", "shot", "volley", "tornado", "windmill", "water", "circle" });

        [SettingsProvider]
        public static SettingsProvider BonusesPage() => Page(
            "Project/AntiBacteria/Bonuses", "Bonuses",
            "When bonuses appear, how the pickup behaves, and what each one does. Distances in points (80 pt = 16 game pixels). " +
            "Changes apply at once, also while playing.",
            Bonuses, new HashSet<string> { "bonus", "power-up", "pickup", "boom", "immortality", "speed", "life" },
            s => { if (GUILayout.Button("Re-link bonus sprites")) FillBonusArt(s); });

        [SettingsProvider]
        public static SettingsProvider ModesPage() => Page(
            "Project/AntiBacteria/Game Modes", "Game Modes",
            "Capture the Flag, Rush and Peace Walker: waves, the difficulty ramp per objective, points and the Peace Walker clock.",
            Modes, new HashSet<string> { "mode", "endless", "flag", "rush", "peace", "walker", "zone", "tentacle" },
            s => { if (GUILayout.Button("Re-link mode sprites")) FillModeArt(s); });

        [SettingsProvider]
        public static SettingsProvider PlatformPage() => Page(
            "Project/AntiBacteria/Platform (Steam)", "Platform (Steam)",
            "Steam App ID, Steam Cloud, achievement API names, run-achievement thresholds and leaderboard names. " +
            "The API names must match Stats & Achievements in the Steamworks partner site.",
            PlatformCfg, new HashSet<string> { "steam", "achievement", "leaderboard", "cloud", "app id", "platform" },
            PlatformButtons);

        private const string SteamPackage = "com.rlabrecque.steamworks.net";
        private const string SteamPackageUrl =
            "https://github.com/rlabrecque/Steamworks.NET.git?path=/com.rlabrecque.steamworks.net#2025.164.1";

        private static void PlatformButtons(PlatformSettings s)
        {
            bool installed = UnityEditor.PackageManager.PackageInfo.FindForAssetPath("Packages/" + SteamPackage) != null;
            EditorGUILayout.HelpBox(installed
                    ? "Steamworks.NET is installed: Steam is live in Windows, macOS and Linux builds, and in the editor while Steam is running."
                    : "Steamworks.NET is not installed, so Steam is off and every achievement / leaderboard / cloud call does nothing. " +
                      "Add it with Package Manager > + > Add package from git URL:\n" + SteamPackageUrl,
                installed ? MessageType.Info : MessageType.Warning);
            if (!installed && GUILayout.Button("Copy the package URL")) EditorGUIUtility.systemCopyBuffer = SteamPackageUrl;
            string file = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../steam_appid.txt"));
            if (GUILayout.Button("Write steam_appid.txt (" + s.steamAppId + ") for testing in the editor"))
                System.IO.File.WriteAllText(file, s.steamAppId.ToString());
        }

        [MenuItem("AntiBacteria/Settings/Platform (Steam)...")]
        private static void OpenPlatform() => SettingsService.OpenProjectSettings("Project/AntiBacteria/Platform (Steam)");

        [MenuItem("AntiBacteria/Settings/Game Modes...")]
        private static void OpenModes() => SettingsService.OpenProjectSettings("Project/AntiBacteria/Game Modes");

        [SettingsProvider]
        public static SettingsProvider ShopPage() => Page(
            "Project/AntiBacteria/Upgrade Shop", "Upgrade Shop",
            "What the Upgrade Character screen sells and for how many vitamins, in the order shown.",
            Shop, new HashSet<string> { "shop", "upgrade", "price", "vitamin", "store" });

        [MenuItem("AntiBacteria/Settings/Upgrade Shop...")]
        private static void OpenShop() => SettingsService.OpenProjectSettings("Project/AntiBacteria/Upgrade Shop");

        [MenuItem("AntiBacteria/Settings/Weapons...")]
        private static void OpenWeapons() => SettingsService.OpenProjectSettings("Project/AntiBacteria/Weapons");

        [MenuItem("AntiBacteria/Settings/Bonuses...")]
        private static void OpenBonuses() => SettingsService.OpenProjectSettings("Project/AntiBacteria/Bonuses");
    }
}
