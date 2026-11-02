using System.Collections.Generic;
using System.Text.RegularExpressions;
using AntiBacteria.Audio;
using UnityEditor;
using UnityEngine;

namespace AntiBacteria.EditorTools
{
    /// <summary>
    /// Import rules for Assets/_Project/Audio and the link between files and SoundSettings.
    /// Sfx/sfx_enemy_kill.wav (+ sfx_enemy_kill_2.wav ...) becomes the clips of Sfx.EnemyKill;
    /// Music/music_menu|level|boss.wav become the three tracks. Drop in a composer's file with the same name to replace one.
    /// </summary>
    public class AudioImportRules : AssetPostprocessor
    {
        private const string Root = "Assets/_Project/Audio/";

        private void OnPreprocessAudio()
        {
            if (!assetPath.StartsWith(Root)) return;
            var importer = (AudioImporter)assetImporter;
            if (!importer.importSettingsMissing) return; // only on first import; later manual changes stick
            bool music = assetPath.StartsWith(Root + "Music/");
            importer.forceToMono = !music;
            importer.loadInBackground = music;
            AudioImporterSampleSettings s = importer.defaultSampleSettings;
            // Short effects: decoded once, instant to start. Music: streamed from disk, compressed.
            s.loadType = music ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad;
            s.compressionFormat = music ? AudioCompressionFormat.Vorbis : AudioCompressionFormat.ADPCM;
            s.quality = music ? 0.6f : 1f;
            importer.defaultSampleSettings = s;
        }

        private static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            foreach (string p in imported)
                if (p.StartsWith(Root)) { EditorApplication.delayCall += () => LinkClips(AudioSettingsPage.GetOrCreate()); return; }
        }

        /// <summary>Fill SoundSettings from the files in Audio/Sfx and Audio/Music.</summary>
        public static void LinkClips(SoundSettings s)
        {
            Undo.RecordObject(s, "Link Audio");
            var byId = new Dictionary<string, List<AudioClip>>();
            foreach (string guid in AssetDatabase.FindAssets("t:AudioClip", new[] { Root + "Sfx" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string file = System.IO.Path.GetFileNameWithoutExtension(path);
                Match m = Regex.Match(file, @"^sfx_([a-z0-9_]+?)(?:_(\d+))?$");
                if (!m.Success) continue;
                string id = m.Groups[1].Value.Replace("_", "");
                if (!byId.TryGetValue(id, out List<AudioClip> list)) byId[id] = list = new List<AudioClip>();
                list.Add(AssetDatabase.LoadAssetAtPath<AudioClip>(path));
            }
            foreach (SfxEntry e in s.effects)
            {
                if (e == null) continue;
                string id = e.id.ToString().ToLowerInvariant();
                if (byId.TryGetValue(id, out List<AudioClip> clips)) e.clips = clips.ToArray();
            }
            s.menuMusic = AssetDatabase.LoadAssetAtPath<AudioClip>(Root + "Music/music_menu.wav") ?? s.menuMusic;
            s.levelMusic = AssetDatabase.LoadAssetAtPath<AudioClip>(Root + "Music/music_level.wav") ?? s.levelMusic;
            s.bossMusic = AssetDatabase.LoadAssetAtPath<AudioClip>(Root + "Music/music_boss.wav") ?? s.bossMusic;
            EditorUtility.SetDirty(s);
            AssetDatabase.SaveAssetIfDirty(s);
        }
    }

    /// <summary>Project Settings > AntiBacteria > Audio.</summary>
    internal static class AudioSettingsPage
    {
        public const string AssetPath = "Assets/_Project/Resources/SoundSettings.asset";
        private const string SettingsPath = "Project/AntiBacteria/Audio";
        private static Editor editor;

        public static SoundSettings GetOrCreate()
        {
            var s = AssetDatabase.LoadAssetAtPath<SoundSettings>(AssetPath);
            if (s != null) return s;
            if (!AssetDatabase.IsValidFolder("Assets/_Project/Resources")) AssetDatabase.CreateFolder("Assets/_Project", "Resources");
            s = ScriptableObject.CreateInstance<SoundSettings>();
            AssetDatabase.CreateAsset(s, AssetPath);
            SoundSettings.Use(s);
            AudioImportRules.LinkClips(s);
            return s;
        }

        [SettingsProvider]
        public static SettingsProvider Create() => new SettingsProvider(SettingsPath, SettingsScope.Project)
        {
            label = "Audio",
            keywords = new HashSet<string> { "audio", "sound", "sfx", "music", "volume" },
            guiHandler = _ =>
            {
                SoundSettings s = GetOrCreate();
                if (editor == null || editor.target != s)
                {
                    if (editor != null) Object.DestroyImmediate(editor);
                    editor = Editor.CreateEditor(s);
                }
                float old = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = 220f;
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Space(10f);
                    using (new EditorGUILayout.VerticalScope())
                    {
                        EditorGUILayout.HelpBox("Sound effects and music. Files in Assets/_Project/Audio are linked by name " +
                            "(Sfx/sfx_enemy_kill.wav, sfx_enemy_kill_2.wav ... ; Music/music_menu, music_level, music_boss). " +
                            "The player's Sound and Music volumes in Options scale everything here.", MessageType.None);
                        EditorGUI.BeginChangeCheck();
                        editor.OnInspectorGUI();
                        if (EditorGUI.EndChangeCheck()) AssetDatabase.SaveAssetIfDirty(s);
                        EditorGUILayout.Space(6);
                        if (GUILayout.Button("Re-link audio files")) AudioImportRules.LinkClips(s);
                    }
                }
                EditorGUIUtility.labelWidth = old;
            },
        };

        [MenuItem("AntiBacteria/Settings/Audio...")]
        private static void Open() => SettingsService.OpenProjectSettings(SettingsPath);
    }
}
