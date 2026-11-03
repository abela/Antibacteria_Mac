using System;
using System.IO;
using System.Linq;
using AntiBacteria.Core;
using UnityEditor;
using UnityEngine;
using Progress = AntiBacteria.Core.Progress;

namespace AntiBacteria.EditorTools
{
    /// <summary>
    /// AntiBacteria > Progress Tool: what the save file holds, and buttons to reset it (all of it, or just the
    /// intro), restore a backup, or clear the player's settings. Every reset backs the save up first
    /// (persistentDataPath/Backups), so nothing is lost by accident. Works in and out of Play mode.
    /// </summary>
    public class ProgressTool : EditorWindow
    {
        private Vector2 scroll;

        private static string BackupDir => Path.Combine(Application.persistentDataPath, "Backups");

        [MenuItem("AntiBacteria/Progress Tool", priority = 10)]
        private static void Open()
        {
            var w = GetWindow<ProgressTool>("Progress");
            w.minSize = new Vector2(340f, 420f);
        }

        [MenuItem("AntiBacteria/Progress/Replay Intro on Next Launch", priority = 11)]
        private static void ResetIntroMenu() => ResetIntro();

        [MenuItem("AntiBacteria/Progress/Reset All Progress...", priority = 12)]
        private static void ResetAllMenu() => ResetAll();

        private void OnFocus() => Repaint();

        private void OnGUI()
        {
            scroll = EditorGUILayout.BeginScrollView(scroll);

            // ---------------------------------------------------------------- the save file
            EditorGUILayout.LabelField("Save file", EditorStyles.boldLabel);
            string path = Progress.FilePath;
            bool exists = File.Exists(path);
            EditorGUILayout.SelectableLabel(path, EditorStyles.miniLabel, GUILayout.Height(16f));
            EditorGUILayout.LabelField(exists ? "Last saved " + File.GetLastWriteTime(path).ToString("yyyy-MM-dd HH:mm:ss") : "No save yet (fresh game)");
            if (GUILayout.Button("Show in Finder")) EditorUtility.RevealInFinder(exists ? path : Application.persistentDataPath);

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("What it holds", EditorStyles.boldLabel);
            if (!EditorApplication.isPlaying) Progress.Load(); // edit mode: always the file as it is now
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.Toggle("Intro seen", Progress.IntroSeen);
                EditorGUILayout.IntField("Plays started", Progress.Plays);
                EditorGUILayout.IntField("Vitamins (wallet)", Progress.Vitamins);
                int cleared = Enumerable.Range(1, SceneFlow.LevelCount).Count(Progress.IsClearedOnAny);
                EditorGUILayout.TextField("Campaign levels cleared", cleared + " / " + SceneFlow.LevelCount);
                EditorGUILayout.IntField("Campaign score (best total)", Progress.CampaignTotal());
                EditorGUILayout.TextField("Roguelike best room (N / H / I)",
                    Progress.RoguelikeBestRoom(Difficulty.Normal) + " / " + Progress.RoguelikeBestRoom(Difficulty.Hard) + " / " +
                    Progress.RoguelikeBestRoom(Difficulty.Impossible));
            }

            // ---------------------------------------------------------------- actions
            EditorGUILayout.Space(10f);
            EditorGUILayout.LabelField("Reset", EditorStyles.boldLabel);
            if (EditorApplication.isPlaying)
                EditorGUILayout.HelpBox("Play mode: the running game's data changes too. Menus show it the next time they open.", MessageType.Info);

            if (GUILayout.Button(new GUIContent("Replay Intro on Next Launch", "Only the 'intro seen' flag. Everything else stays.")))
                ResetIntro();

            GUI.backgroundColor = new Color(1f, 0.55f, 0.55f);
            if (GUILayout.Button(new GUIContent("Reset All Progress...", "Campaign, scores, wallet, upgrades, roguelike records, intro. Backed up first."), GUILayout.Height(28f)))
                ResetAll();
            GUI.backgroundColor = Color.white;

            string latest = LatestBackup();
            using (new EditorGUI.DisabledScope(latest == null))
            {
                if (GUILayout.Button(new GUIContent("Restore Last Backup" + (latest != null ? "  (" + Path.GetFileNameWithoutExtension(latest).Replace("save-", "") + ")" : ""),
                        "Puts back the save from just before the last reset.")))
                    Restore(latest);
            }
            if (Directory.Exists(BackupDir) && GUILayout.Button("Show Backups in Finder")) EditorUtility.RevealInFinder(BackupDir);

            EditorGUILayout.Space(10f);
            EditorGUILayout.LabelField("Player settings", EditorStyles.boldLabel);
            if (GUILayout.Button(new GUIContent("Reset Settings...", "Volumes, language, palette, auto-shoot, display and remapped controls (PlayerPrefs). Progress stays.")))
            {
                if (EditorUtility.DisplayDialog("Reset settings?",
                        "Clears volumes, language, palette, auto-shoot, display mode and remapped controls (all PlayerPrefs for this project).\n\nProgress is not touched.",
                        "Reset Settings", "Cancel"))
                {
                    PlayerPrefs.DeleteAll();
                    PlayerPrefs.Save();
                    Debug.Log("Progress Tool: settings (PlayerPrefs) cleared. Restart Play mode to see the defaults.");
                }
            }

            EditorGUILayout.EndScrollView();
        }

        // -------------------------------------------------------------------- actions

        private static void ResetIntro()
        {
            Backup();
            Progress.ResetIntro();
            Debug.Log("Progress Tool: the intro will play on the next launch.");
            RepaintAll();
        }

        private static void ResetAll()
        {
            if (!EditorUtility.DisplayDialog("Reset all progress?",
                    "Clears the campaign, best scores, vitamins, bought upgrades, roguelike records, play count and the intro flag - " +
                    "like a first launch.\n\nThe current save is backed up first and can be restored from this window.",
                    "Reset Everything", "Cancel"))
                return;
            string backup = Backup();
            Progress.ResetAll();
            Debug.Log("Progress Tool: progress reset." + (backup != null ? " Backup: " + backup : ""));
            RepaintAll();
        }

        private static void Restore(string backup)
        {
            if (backup == null || !EditorUtility.DisplayDialog("Restore backup?",
                    "Replaces the current save with " + Path.GetFileName(backup) + ".", "Restore", "Cancel"))
                return;
            File.Copy(backup, Progress.FilePath, true);
            Progress.Load();
            Progress.Save(); // also refreshes the Steam Cloud copy, so it doesn't win on the next load
            Debug.Log("Progress Tool: restored " + backup);
            RepaintAll();
        }

        /// <summary>Copy the save into Backups (timestamped). Returns the copy's path, or null if there is no save.</summary>
        private static string Backup()
        {
            Progress.SaveIfDirty();
            if (!File.Exists(Progress.FilePath)) return null;
            Directory.CreateDirectory(BackupDir);
            string to = Path.Combine(BackupDir, "save-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".json");
            File.Copy(Progress.FilePath, to, true);
            return to;
        }

        private static string LatestBackup()
        {
            if (!Directory.Exists(BackupDir)) return null;
            return Directory.GetFiles(BackupDir, "save-*.json").OrderBy(f => f).LastOrDefault();
        }

        private static void RepaintAll()
        {
            foreach (ProgressTool w in Resources.FindObjectsOfTypeAll<ProgressTool>()) w.Repaint();
        }
    }
}
