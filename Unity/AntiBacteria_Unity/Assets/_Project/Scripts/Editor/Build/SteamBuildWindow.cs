using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using AntiBacteria.Platform;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Networking;
using Debug = UnityEngine.Debug;

namespace AntiBacteria.EditorTools
{
    /// <summary>
    /// AntiBacteria > Build > Steam Build Window. Builds the selected platforms into Builds/Steam/&lt;Platform&gt;
    /// (outside Assets, git-ignored), and either uploads them to Steam straight away (Build &amp; Upload) or keeps them
    /// for testing first (Build), with "Upload Last Build" to send that exact build later.
    /// Uploads run SteamCMD in its own terminal window: the first time it asks for the Steam password and the
    /// Steam Guard code there, then remembers the login. Nothing secret is stored by the project.
    /// </summary>
    public class SteamBuildWindow : EditorWindow
    {
        private const string Pref = "AntiBacteria.SteamBuild.";
        private static readonly SteamPlatform[] All = { SteamPlatform.Windows, SteamPlatform.MacOS, SteamPlatform.Linux };

        [Serializable]
        private class LastBuild
        {
            public string version;
            public int build;
            public string time;
            public string commit;
            public bool development;
            public string notes;
            public List<string> platforms = new List<string>();
        }

        private SteamBuildSettings settings;
        private LastBuild last;
        private bool development;
        private string notes = "";
        private Vector2 scroll, logScroll;
        private readonly StringBuilder log = new StringBuilder();

        [MenuItem("AntiBacteria/Build/Steam Build Window...", priority = 0)]
        public static void Open()
        {
            var w = GetWindow<SteamBuildWindow>("Steam Build");
            w.minSize = new Vector2(440f, 600f);
        }

        // ------------------------------------------------------------------ per-machine preferences

        private static string SteamCmdPath
        {
            get => EditorPrefs.GetString(Pref + "SteamCmd", DefaultSteamCmdPath);
            set => EditorPrefs.SetString(Pref + "SteamCmd", value);
        }

        private static string SteamUser
        {
            get => EditorPrefs.GetString(Pref + "User", "");
            set => EditorPrefs.SetString(Pref + "User", value);
        }

        private static bool Selected(SteamPlatform p) => EditorPrefs.GetBool(Pref + "Platform." + p, p == SteamPlatform.Windows);
        private static void SetSelected(SteamPlatform p, bool on) => EditorPrefs.SetBool(Pref + "Platform." + p, on);

        private static bool IsWindowsEditor => Application.platform == RuntimePlatform.WindowsEditor;

        private static string SteamCmdFolder => IsWindowsEditor
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SteamCMD")
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "SteamCMD");

        private static string DefaultSteamCmdPath => Path.Combine(SteamCmdFolder, IsWindowsEditor ? "steamcmd.exe" : "steamcmd.sh");

        // ------------------------------------------------------------------ paths

        private static string RepoRoot => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", ".."));
        private string BuildRoot => Path.GetFullPath(Path.Combine(RepoRoot, settings.buildFolder));
        private string PlatformDir(SteamPlatform p) => Path.Combine(BuildRoot, p.ToString());
        private string LastBuildFile => Path.Combine(BuildRoot, "last_build.json");

        private static string ExeName(SteamPlatform p)
        {
            string name = PlayerSettings.productName;
            switch (p)
            {
                case SteamPlatform.Windows: return name + ".exe";
                case SteamPlatform.MacOS: return name + ".app";
                default: return name + ".x86_64";
            }
        }

        private static BuildTarget Target(SteamPlatform p) =>
            p == SteamPlatform.Windows ? BuildTarget.StandaloneWindows64 : p == SteamPlatform.MacOS ? BuildTarget.StandaloneOSX : BuildTarget.StandaloneLinux64;

        private static bool ModuleInstalled(SteamPlatform p) => BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone, Target(p));

        // ------------------------------------------------------------------ window

        private void OnEnable()
        {
            settings = SteamBuildSettings.Get();
            LoadLastBuild();
        }

        private void OnGUI()
        {
            if (settings == null) settings = SteamBuildSettings.Get();
            scroll = EditorGUILayout.BeginScrollView(scroll);

            EditorGUILayout.LabelField("AntiBacteria for Steam  (App " + PlatformSettings.Current.steamAppId + ")", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            string version = EditorGUILayout.TextField("Version", PlayerSettings.bundleVersion);
            if (EditorGUI.EndChangeCheck()) PlayerSettings.bundleVersion = version;
            EditorGUILayout.LabelField("Next build number", (settings.buildNumber + 1).ToString());

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Platforms", EditorStyles.boldLabel);
            foreach (SteamPlatform p in All)
            {
                bool module = ModuleInstalled(p);
                uint depot = settings.DepotFor(p);
                string note = !module ? "  (build module not installed in this Unity)" : depot == 0 ? "  (no depot yet: build only)" : "  depot " + depot;
                using (new EditorGUI.DisabledScope(!module))
                {
                    bool on = EditorGUILayout.ToggleLeft(p + note, module && Selected(p));
                    if (module && on != Selected(p)) SetSelected(p, on);
                }
            }

            EditorGUILayout.Space();
            development = EditorGUILayout.ToggleLeft("Development build (profiler, debug log; never upload these to players)", development);
            notes = EditorGUILayout.TextField(new GUIContent("Notes", "Added to the build description on Steamworks."), notes);

            EditorGUILayout.Space();
            using (new EditorGUI.DisabledScope(SelectedPlatforms().Count == 0))
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Build", GUILayout.Height(32f))) EditorApplication.delayCall += () => Build(false);
                if (GUILayout.Button("Build & Upload to Steam", GUILayout.Height(32f))) EditorApplication.delayCall += () => Build(true);
            }

            EditorGUILayout.Space();
            DrawLastBuild();
            EditorGUILayout.Space();
            DrawSteamAccount();
            EditorGUILayout.Space();
            DrawLog();

            EditorGUILayout.EndScrollView();
        }

        private void DrawLastBuild()
        {
            EditorGUILayout.LabelField("Last build", EditorStyles.boldLabel);
            if (last == null || last.platforms.Count == 0)
            {
                EditorGUILayout.HelpBox("No build yet. Build first, test it, then upload it from here.", MessageType.Info);
                return;
            }
            EditorGUILayout.LabelField("Version", "v" + last.version + "  build " + last.build + (last.development ? "  (development)" : ""));
            EditorGUILayout.LabelField("Built", last.time + (string.IsNullOrEmpty(last.commit) ? "" : "  commit " + last.commit));
            EditorGUILayout.LabelField("Platforms", string.Join(", ", last.platforms));
            if (!string.IsNullOrEmpty(last.notes)) EditorGUILayout.LabelField("Notes", last.notes);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Play")) PlayLastBuild();
                if (GUILayout.Button("Open Folder")) EditorUtility.RevealInFinder(BuildRoot);
                if (GUILayout.Button("Upload Last Build to Steam")) Upload(last);
            }
        }

        private void DrawSteamAccount()
        {
            EditorGUILayout.LabelField("Steam upload (this computer)", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                SteamCmdPath = EditorGUILayout.TextField("SteamCMD", SteamCmdPath);
                if (GUILayout.Button("...", GUILayout.Width(28f)))
                {
                    string picked = EditorUtility.OpenFilePanel("SteamCMD", Path.GetDirectoryName(SteamCmdPath), IsWindowsEditor ? "exe" : "sh");
                    if (!string.IsNullOrEmpty(picked)) SteamCmdPath = picked;
                }
            }
            if (!File.Exists(SteamCmdPath))
            {
                EditorGUILayout.HelpBox("SteamCMD isn't there yet. Download it from Valve (about 3 MB, into " + SteamCmdFolder + ").", MessageType.Warning);
                if (GUILayout.Button("Download SteamCMD")) DownloadSteamCmd();
            }
            SteamUser = EditorGUILayout.TextField(new GUIContent("Steam login name", "The account that uploads builds (needs the Edit App Metadata and Publish permissions)."), SteamUser);
            EditorGUI.BeginChangeCheck();
            string branch = EditorGUILayout.TextField(new GUIContent("Set live on branch", "e.g. beta. Empty: set the build live on Steamworks yourself."), settings.setLiveBranch);
            if (EditorGUI.EndChangeCheck()) { settings.setLiveBranch = branch.Trim(); settings.Save(); }
            if (IsDefaultBranch(settings.setLiveBranch))
                EditorGUILayout.HelpBox("Steam refuses to set the default branch live from SteamCMD (the commit fails). The upload " +
                                        "leaves SetLive out; set the build live on Steamworks > SteamPipe > Builds.", MessageType.Info);
            EditorGUILayout.HelpBox("The upload opens its own window. The first time, SteamCMD asks for the password and the " +
                                    "Steam Guard code there; after that it stays logged in. Depots: Project > " + SteamBuildSettings.AssetPath + ".",
                MessageType.None);
        }

        private void DrawLog()
        {
            EditorGUILayout.LabelField("Log", EditorStyles.boldLabel);
            logScroll = EditorGUILayout.BeginScrollView(logScroll, GUILayout.Height(140f));
            EditorGUILayout.TextArea(log.ToString(), EditorStyles.wordWrappedLabel);
            EditorGUILayout.EndScrollView();
        }

        private void Log(string line, bool error = false)
        {
            log.AppendLine(DateTime.Now.ToString("HH:mm:ss") + "  " + line);
            if (error) Debug.LogError("Steam Build: " + line);
            else Debug.Log("Steam Build: " + line);
            logScroll.y = float.MaxValue;
            Repaint();
        }

        private List<SteamPlatform> SelectedPlatforms() => All.Where(p => ModuleInstalled(p) && Selected(p)).ToList();

        // ------------------------------------------------------------------ build

        private void Build(bool thenUpload)
        {
            List<SteamPlatform> platforms = SelectedPlatforms();
            string[] scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            if (scenes.Length == 0) { Log("No scenes in Build Settings.", true); return; }
            if (thenUpload && development &&
                !EditorUtility.DisplayDialog("Steam Build", "This is a development build. Upload it to Steam anyway?", "Upload", "Cancel"))
                return;
            if (!EditorSceneManagerSaveCheck()) return;

            BuildTarget original = EditorUserBuildSettings.activeBuildTarget;
            var built = new List<string>();
            try
            {
                foreach (SteamPlatform p in platforms)
                {
                    string dir = PlatformDir(p);
                    if (Directory.Exists(dir)) Directory.Delete(dir, true); // no leftovers from older builds in the upload
                    Directory.CreateDirectory(dir);
                    Log("Building " + p + "...");
                    var options = new BuildPlayerOptions
                    {
                        scenes = scenes,
                        locationPathName = Path.Combine(dir, ExeName(p)),
                        target = Target(p),
                        targetGroup = BuildTargetGroup.Standalone,
                        options = development ? BuildOptions.Development : BuildOptions.None,
                    };
                    BuildReport report = BuildPipeline.BuildPlayer(options);
                    BuildSummary sum = report.summary;
                    if (sum.result != BuildResult.Succeeded)
                    {
                        Log(p + " build " + sum.result + " (" + sum.totalErrors + " errors, see the Console).", true);
                        return;
                    }
                    RemoveDoNotShipFolders(dir);
                    WriteAppIdFile(dir, p);
                    Log(p + " built: " + (sum.totalSize / (1024f * 1024f)).ToString("0.0") + " MB in " + sum.totalTime.TotalSeconds.ToString("0") + " s.");
                    built.Add(p.ToString());
                }
            }
            finally
            {
                if (EditorUserBuildSettings.activeBuildTarget != original)
                    EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone, original);
            }

            settings.buildNumber++;
            settings.Save();
            last = new LastBuild
            {
                version = PlayerSettings.bundleVersion,
                build = settings.buildNumber,
                time = DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
                commit = GitCommit(),
                development = development,
                notes = notes,
                platforms = built,
            };
            File.WriteAllText(LastBuildFile, JsonUtility.ToJson(last, true));
            Log("Build " + last.build + " ready in " + BuildRoot + ".");
            if (thenUpload) Upload(last);
        }

        private static bool EditorSceneManagerSaveCheck() => UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();

        /// <summary>Unity's debug-symbol and IL2CPP backup folders: never part of a Steam depot.</summary>
        private static void RemoveDoNotShipFolders(string dir)
        {
            foreach (string sub in Directory.GetDirectories(dir))
            {
                string n = Path.GetFileName(sub);
                if (n.EndsWith("_DoNotShip", StringComparison.Ordinal) || n.EndsWith("_ButDontShipItWithYourGame", StringComparison.Ordinal))
                    Directory.Delete(sub, true);
            }
        }

        /// <summary>
        /// steam_appid.txt next to the game, so the build also starts when launched by hand (a release build started
        /// outside Steam otherwise asks Steam to relaunch it and quits). Every depot excludes it: players never get it.
        /// </summary>
        private static void WriteAppIdFile(string dir, SteamPlatform p)
        {
            string id = PlatformSettings.Current.steamAppId.ToString();
            File.WriteAllText(Path.Combine(dir, "steam_appid.txt"), id);
            if (p == SteamPlatform.MacOS) // a Mac app is started from inside its bundle
                File.WriteAllText(Path.Combine(dir, ExeName(p), "Contents", "MacOS", "steam_appid.txt"), id);
        }

        private void LoadLastBuild()
        {
            last = null;
            try { if (File.Exists(LastBuildFile)) last = JsonUtility.FromJson<LastBuild>(File.ReadAllText(LastBuildFile)); }
            catch (Exception e) { Debug.LogWarning("Steam Build: could not read " + LastBuildFile + ": " + e.Message); }
        }

        private static string GitCommit()
        {
            try
            {
                var psi = new ProcessStartInfo("git", "rev-parse --short HEAD")
                {
                    WorkingDirectory = RepoRoot, UseShellExecute = false, RedirectStandardOutput = true, CreateNoWindow = true,
                };
                using (Process p = Process.Start(psi))
                {
                    string s = p.StandardOutput.ReadToEnd().Trim();
                    p.WaitForExit(5000);
                    return p.ExitCode == 0 ? s : "";
                }
            }
            catch { return ""; }
        }

        // ------------------------------------------------------------------ test

        /// <summary>
        /// Starts the last build of this computer's platform (with steam_appid.txt, see WriteAppIdFile).
        /// </summary>
        private void PlayLastBuild()
        {
            SteamPlatform mine = IsWindowsEditor ? SteamPlatform.Windows
                : Application.platform == RuntimePlatform.OSXEditor ? SteamPlatform.MacOS : SteamPlatform.Linux;
            string dir = PlatformDir(mine);
            string exe = Path.Combine(dir, ExeName(mine));
            if (!File.Exists(exe) && !Directory.Exists(exe)) { Log("No " + mine + " build to play. Build " + mine + " first.", true); return; }
            WriteAppIdFile(dir, mine);
            if (mine == SteamPlatform.MacOS) Process.Start("open", "\"" + exe + "\"");
            else Process.Start(new ProcessStartInfo(exe) { WorkingDirectory = dir, UseShellExecute = true });
            Log("Started " + exe + ".");
        }

        // ------------------------------------------------------------------ upload

        private void Upload(LastBuild build)
        {
            if (build == null || build.platforms.Count == 0) { Log("Nothing to upload: build first.", true); return; }
            if (!File.Exists(SteamCmdPath)) { Log("SteamCMD not found at " + SteamCmdPath + ".", true); return; }
            if (string.IsNullOrWhiteSpace(SteamUser)) { Log("Enter the Steam login name first.", true); return; }

            var depots = new List<(uint id, string folder)>();
            foreach (string name in build.platforms)
            {
                var p = (SteamPlatform)Enum.Parse(typeof(SteamPlatform), name);
                uint depot = settings.DepotFor(p);
                if (depot == 0) { Log(p + " has no depot yet: skipped (set it in " + SteamBuildSettings.AssetPath + ").", true); continue; }
                if (!Directory.Exists(PlatformDir(p))) { Log(p + " build folder is missing: skipped.", true); continue; }
                depots.Add((depot, p.ToString()));
            }
            if (depots.Count == 0) { Log("No platform of this build can be uploaded.", true); return; }

            string scripts = Path.Combine(BuildRoot, "Scripts");
            Directory.CreateDirectory(scripts);
            Directory.CreateDirectory(Path.Combine(BuildRoot, "Output"));
            string vdf = Path.Combine(scripts, "app_build_" + PlatformSettings.Current.steamAppId + ".vdf");
            File.WriteAllText(vdf, AppBuildVdf(build, depots));

            string desc = Description(build);
            if (!EditorUtility.DisplayDialog("Upload to Steam",
                    "Upload " + desc + " (" + string.Join(", ", depots.Select(d => d.folder)) + ")?\n\n" +
                    (string.IsNullOrEmpty(SetLiveBranch)
                        ? (IsDefaultBranch(settings.setLiveBranch)
                            ? "Steam doesn't let SteamCMD set the default branch live: after the upload, set it live on Steamworks (SteamPipe > Builds)."
                            : "It will not be set live: do that on Steamworks (SteamPipe > Builds).")
                        : "It will be set live on the '" + SetLiveBranch + "' branch."),
                    "Upload", "Cancel"))
                return;

            string script = WriteUploadScript(vdf);
            if (IsWindowsEditor) Process.Start(new ProcessStartInfo(script) { UseShellExecute = true, WorkingDirectory = scripts });
            else Process.Start("open", "-a Terminal \"" + script + "\"");
            Log("Upload of " + desc + " started in its own window. Check it for the result (and the Steam password / Steam Guard code the first time).");
        }

        private string Description(LastBuild b) =>
            "v" + b.version + " build " + b.build + (b.development ? " DEV" : "") +
            (string.IsNullOrEmpty(b.commit) ? "" : " (" + b.commit + ")") + (string.IsNullOrEmpty(b.notes) ? "" : " - " + b.notes);

        private static bool IsDefaultBranch(string branch) =>
            string.Equals(branch?.Trim(), "default", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(branch?.Trim(), "public", StringComparison.OrdinalIgnoreCase);

        /// <summary>Branch for the VDF's SetLive. Empty for default/public: SteamCMD may not set those live (commit fails).</summary>
        private string SetLiveBranch => IsDefaultBranch(settings.setLiveBranch) ? "" : settings.setLiveBranch;

        private string AppBuildVdf(LastBuild b, List<(uint id, string folder)> depots)
        {
            string Q(string s) => "\"" + s.Replace("\\", "\\\\").Replace("\"", "'") + "\"";
            var v = new StringBuilder();
            v.AppendLine("\"AppBuild\"").AppendLine("{");
            v.AppendLine("\t\"AppID\" " + Q(PlatformSettings.Current.steamAppId.ToString()));
            v.AppendLine("\t\"Desc\" " + Q(Description(b)));
            v.AppendLine("\t\"ContentRoot\" " + Q(BuildRoot + Path.DirectorySeparatorChar));
            v.AppendLine("\t\"BuildOutput\" " + Q(Path.Combine(BuildRoot, "Output") + Path.DirectorySeparatorChar));
            if (!string.IsNullOrEmpty(SetLiveBranch)) v.AppendLine("\t\"SetLive\" " + Q(SetLiveBranch));
            v.AppendLine("\t\"Depots\"").AppendLine("\t{");
            foreach (var d in depots)
            {
                v.AppendLine("\t\t" + Q(d.id.ToString())).AppendLine("\t\t{");
                v.AppendLine("\t\t\t\"FileMapping\"").AppendLine("\t\t\t{");
                v.AppendLine("\t\t\t\t\"LocalPath\" " + Q(d.folder + Path.DirectorySeparatorChar + "*"));
                v.AppendLine("\t\t\t\t\"DepotPath\" \".\"");
                v.AppendLine("\t\t\t\t\"recursive\" \"1\"");
                v.AppendLine("\t\t\t}");
                v.AppendLine("\t\t\t\"FileExclusion\" \"*.pdb\"");
                v.AppendLine("\t\t\t\"FileExclusion\" \"*steam_appid.txt\"");
                v.AppendLine("\t\t}");
            }
            v.AppendLine("\t}").AppendLine("}");
            return v.ToString();
        }

        /// <summary>A .bat (Windows) or .command (Mac) that runs SteamCMD and keeps its window open to show the result.</summary>
        private string WriteUploadScript(string vdf)
        {
            string scripts = Path.GetDirectoryName(vdf);
            if (IsWindowsEditor)
            {
                string bat = Path.Combine(scripts, "upload.bat");
                File.WriteAllText(bat,
                    "@echo off\r\n" +
                    "title AntiBacteria Steam upload\r\n" +
                    "\"" + SteamCmdPath + "\" +login " + SteamUser.Trim() + " +run_app_build \"" + vdf + "\" +quit\r\n" +
                    "echo.\r\n" +
                    "if %ERRORLEVEL%==0 (echo Upload finished.) else (echo Upload FAILED, exit code %ERRORLEVEL%. See the lines above.)\r\n" +
                    "pause\r\n");
                return bat;
            }
            string sh = Path.Combine(scripts, "upload.command");
            File.WriteAllText(sh,
                "#!/bin/bash\n" +
                "\"" + SteamCmdPath + "\" +login " + SteamUser.Trim() + " +run_app_build \"" + vdf + "\" +quit\n" +
                "code=$?\necho\n" +
                "if [ $code -eq 0 ]; then echo \"Upload finished.\"; else echo \"Upload FAILED, exit code $code. See the lines above.\"; fi\n" +
                "read -n 1 -s -r -p \"Press any key to close\"\n");
            using (Process p = Process.Start("chmod", "+x \"" + sh + "\"")) p.WaitForExit(5000);
            return sh;
        }

        // ------------------------------------------------------------------ SteamCMD download

        /// <summary>Valve's SteamCMD (Windows zip / macOS tar.gz) into the per-user SteamCMD folder; it updates itself on first run.</summary>
        private void DownloadSteamCmd()
        {
            string url = IsWindowsEditor
                ? "https://steamcdn-a.akamaihd.net/client/installer/steamcmd.zip"
                : "https://steamcdn-a.akamaihd.net/client/installer/steamcmd_osx.tar.gz";
            string folder = SteamCmdFolder;
            Directory.CreateDirectory(folder);
            string archive = Path.Combine(folder, IsWindowsEditor ? "steamcmd.zip" : "steamcmd_osx.tar.gz");
            try
            {
                using (UnityWebRequest req = UnityWebRequest.Get(url))
                {
                    req.downloadHandler = new DownloadHandlerFile(archive);
                    UnityWebRequestAsyncOperation op = req.SendWebRequest();
                    while (!op.isDone)
                    {
                        if (EditorUtility.DisplayCancelableProgressBar("SteamCMD", "Downloading from Valve...", req.downloadProgress)) { req.Abort(); break; }
                        System.Threading.Thread.Sleep(50);
                    }
                    if (req.result != UnityWebRequest.Result.Success) { Log("SteamCMD download failed: " + req.error, true); return; }
                }
                // tar ships with Windows 10+ and macOS, and unpacks both the zip and the tar.gz.
                var psi = new ProcessStartInfo("tar", (IsWindowsEditor ? "-xf \"" : "-xzf \"") + archive + "\" -C \"" + folder + "\"")
                {
                    UseShellExecute = false, CreateNoWindow = true, RedirectStandardError = true,
                };
                using (Process p = Process.Start(psi))
                {
                    string err = p.StandardError.ReadToEnd();
                    p.WaitForExit(60000);
                    if (p.ExitCode != 0) { Log("Unpacking SteamCMD failed: " + err, true); return; }
                }
                File.Delete(archive);
                SteamCmdPath = DefaultSteamCmdPath;
                Log("SteamCMD ready at " + SteamCmdPath + ". It updates itself the first time it runs.");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }
    }
}
