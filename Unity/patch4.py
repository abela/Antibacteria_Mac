import os,sys
root=sys.argv[1]
def patch(rel, pairs):
    p=os.path.join(root,rel); s=open(p,encoding='utf-8').read()
    for a,b in pairs:
        n=s.count(a); assert n==1,(rel,n,a[:70]); s=s.replace(a,b)
    open(p,'w',encoding='utf-8').write(s); print('ok',rel)

patch('Core/Progress.cs',[
("using AntiBacteria.Gameplay.Weapons;\n", "using AntiBacteria.Gameplay.Weapons;\nusing AntiBacteria.Platform;\n"),
("            Data.wallet.vitamins = Mathf.Max(0, Data.wallet.vitamins + amount);\n            dirty = true;\n",
 "            Data.wallet.vitamins = Mathf.Max(0, Data.wallet.vitamins + amount);\n            dirty = true;\n            AchievementTracker.WalletChanged(Data.wallet.vitamins);\n"),
("            Data.wallet.owned[(int)item] = true;\n            Save();\n            return true;\n",
 "            Data.wallet.owned[(int)item] = true;\n            Save();\n            int owned = 0;\n            foreach (bool o in Data.wallet.owned) if (o) owned++;\n            AchievementTracker.UpgradeBought(owned, UpgradeCount);\n            return true;\n"),
("        private static int ModeIndex(GameMode mode, Difficulty d)",
 "        /// <summary>Sum of the best score of every level on every difficulty (the campaign leaderboard).</summary>\n        public static int CampaignTotal()\n        {\n            long total = 0;\n            foreach (int b in Data.campaign.best) total += b;\n            return (int)Math.Min(total, int.MaxValue);\n        }\n\n        private static int ModeIndex(GameMode mode, Difficulty d)"),
("                if (File.Exists(FilePath)) data = JsonUtility.FromJson<SaveData>(File.ReadAllText(FilePath));\n",
 "                string json = File.Exists(FilePath) ? File.ReadAllText(FilePath) : null;\n                // Steam Cloud: take the cloud copy when there is no local file or the cloud one is newer.\n                byte[] cloud = PlatformServices.CloudRead(CloudFile, out long cloudTime);\n                if (cloud != null && (json == null || cloudTime > new DateTimeOffset(File.GetLastWriteTimeUtc(FilePath)).ToUnixTimeSeconds()))\n                    json = System.Text.Encoding.UTF8.GetString(cloud);\n                if (json != null) data = JsonUtility.FromJson<SaveData>(json);\n"),
("                File.Move(tmp, FilePath); // write-then-rename, so a crash never leaves half a file\n",
 "                File.Move(tmp, FilePath); // write-then-rename, so a crash never leaves half a file\n                PlatformServices.CloudWrite(CloudFile, File.ReadAllBytes(FilePath));\n"),
("        public static string FilePath =>", "        private const string CloudFile = \"save.json\";\n\n        public static string FilePath =>"),
])

patch('Gameplay/Modes/EndlessModes.cs',[
("        public virtual bool PlayerCanShoot => true;\n",
 "        public virtual bool PlayerCanShoot => true;\n        /// <summary>Flags taken or zones filled this run (achievements).</summary>\n        public int Completed { get; protected set; }\n"),
("            GameSession.AddScore(points);\n            HasObjective = false;\n",
 "            GameSession.AddScore(points);\n            HasObjective = false;\n            Completed++;\n"),
("                runner.TimeLeft = S.secondsPerFlag;\n                flag = null;\n",
 "                runner.TimeLeft = S.secondsPerFlag;\n                flag = null;\n                Completed++;\n"),
])

patch('Gameplay/Level/CampaignLevelRunner.cs',[
("            Progress.RecordWin(GameSession.CurrentLevel, GameSession.Difficulty, GameSession.Score); // unlocks the next level\n",
 "            Progress.RecordWin(GameSession.CurrentLevel, GameSession.Difficulty, GameSession.Score); // unlocks the next level\n            AchievementTracker.LevelWon(GameSession.CurrentLevel, GameSession.Difficulty, IsBossLevel);\n"),
("                    NewModeBest = Progress.RecordModeScore(GameSession.Mode, GameSession.Difficulty, GameSession.Score);\n",
 "                    NewModeBest = Progress.RecordModeScore(GameSession.Mode, GameSession.Difficulty, GameSession.Score);\n                    AchievementTracker.RunEnded(GameSession.Mode, GameSession.Score, Mode.Completed);\n"),
])

patch('UI/Menus/GameplayMenuController.cs',[
("            if (runner != null) runner.PhaseChanged += OnPhaseChanged;\n        }\n",
 "            if (runner != null) runner.PhaseChanged += OnPhaseChanged;\n            AntiBacteria.Platform.PlatformServices.OverlayChanged += OnOverlayChanged;\n        }\n"),
("            if (runner != null) runner.PhaseChanged -= OnPhaseChanged;\n            if (watchedBuffs",
 "            if (runner != null) runner.PhaseChanged -= OnPhaseChanged;\n            AntiBacteria.Platform.PlatformServices.OverlayChanged -= OnOverlayChanged;\n            if (watchedBuffs"),
("        public void Resume()\n        {\n            navigator.Clear();\n",
 "        /// <summary>The Steam overlay opened over a running level: pause it (the player resumes from the menu).</summary>\n        private void OnOverlayChanged(bool open)\n        {\n            if (open && !navigator.IsOpen && !PauseController.IsPaused && !PauseController.Locked) OpenPause();\n        }\n\n        public void Resume()\n        {\n            navigator.Clear();\n"),
])

patch('Editor/GameDataSettings.cs',[
("using AntiBacteria.Gameplay.Modes;\n", "using AntiBacteria.Gameplay.Modes;\nusing AntiBacteria.Platform;\n"),
("        public static ModeSettings Modes() =>",
 "        public static PlatformSettings PlatformCfg() =>\n            GetOrCreate<PlatformSettings>(Resources + \"/PlatformSettings.asset\", PlatformSettings.Use, null);\n\n        public static ModeSettings Modes() =>"),
("        [MenuItem(\"AntiBacteria/Settings/Game Modes...\")]\n",
 """        [SettingsProvider]
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
"""),
])
