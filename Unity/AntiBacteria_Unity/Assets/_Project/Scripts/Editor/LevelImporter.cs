using System;
using AntiBacteria.Core;
using AntiBacteria.Gameplay.Level;
using UnityEditor;
using UnityEngine;

namespace AntiBacteria.EditorTools
{
    /// <summary>
    /// Imports the 50 campaign levels converted from the 2013 plists
    /// (Assets/_Project/Data/Source/levels_all.json) into LevelData assets in Assets/_Project/Data/Levels.
    /// Safe to re-run: existing assets are updated in place, so references keep working.
    /// </summary>
    public static class LevelImporter
    {
        public const string SourcePath = "Assets/_Project/Data/Source/levels_all.json";
        public const string OutFolder = "Assets/_Project/Data/Levels";

        [Serializable] private class Row
        {
            public int boss; public int level; public int number; public float time;
            public float enemiescreatetime; public float enemiesgrowinterval; public int maximumenemies; public int[] enemies;
        }
        [Serializable] private class Wrapper { public Row[] items; }

        public static string AssetPath(int number) => $"{OutFolder}/Level_{number:00}.asset";

        [MenuItem("AntiBacteria/Data/Import Levels")]
        public static void ImportMenu() => Debug.Log(ImportAll());

        public static string ImportAll()
        {
            var json = AssetDatabase.LoadAssetAtPath<TextAsset>(SourcePath);
            if (json == null) return "LevelImporter: " + SourcePath + " not found.";
            Wrapper data = JsonUtility.FromJson<Wrapper>("{\"items\":" + json.text + "}");
            if (!AssetDatabase.IsValidFolder(OutFolder)) AssetDatabase.CreateFolder("Assets/_Project/Data", "Levels");

            int count = 0;
            foreach (Row r in data.items)
            {
                // "level" is the plist file number level_<N> (1..50, global); "number" is the in-game id,
                // which is wrong once in the 2013 data (level_49 says 48).
                int number = r.level;
                int levelInWorld = (number - 1) % 10 + 1;
                if (r.number != number)
                    Debug.Log($"LevelImporter: level_{number} says number {r.number}; using {number} (2013 data typo).");

                string path = AssetPath(number);
                var asset = AssetDatabase.LoadAssetAtPath<LevelData>(path);
                bool isNew = asset == null;
                if (isNew) asset = ScriptableObject.CreateInstance<LevelData>();

                asset.world = r.boss;
                asset.levelInWorld = levelInWorld;
                asset.number = number;
                asset.timeSeconds = r.time;
                asset.spawnIntervalSeconds = r.enemiescreatetime;
                asset.maxEnemiesPerWave = Mathf.Max(1, r.maximumenemies);
                // grow counter: +0.01 per frame at 60 fps -> one unit = 1/0.6 s; 0 means "never grows"
                asset.growIntervalSeconds = r.enemiesgrowinterval * Legacy.SecondsPerFrameUnit;
                asset.enemyPool = r.enemies ?? new int[0];

                if (isNew) AssetDatabase.CreateAsset(asset, path);
                else EditorUtility.SetDirty(asset);
                count++;
            }
            AssetDatabase.SaveAssets();
            return $"LevelImporter: {count} levels written to {OutFolder}.";
        }
    }
}
