using UnityEditor;
using UnityEngine;

namespace AntiBacteria.EditorTools
{
    public enum SteamPlatform { Windows, MacOS, Linux }

    /// <summary>
    /// Shared Steam build settings (committed): depots per platform, the build folder and the branch to set live.
    /// Per-machine things (SteamCMD location, Steam login name, which platforms are ticked) live in EditorPrefs,
    /// see SteamBuildWindow. The Steam password is never stored: SteamCMD asks for it in its own window and
    /// remembers the login itself.
    /// </summary>
    public class SteamBuildSettings : ScriptableObject
    {
        public const string AssetPath = "Assets/_Project/Editor/SteamBuildSettings.asset";

        [Tooltip("Steamworks > SteamPipe > Depots. Steam created the first depot as App ID + 1. 0 = no depot yet: " +
                 "that platform can be built, not uploaded.")]
        public uint windowsDepot = 5554671;
        public uint macDepot;
        public uint linuxDepot;

        [Tooltip("Where builds go, relative to the repository root (git-ignored).")]
        public string buildFolder = "Builds/Steam";

        [Tooltip("Branch an upload is set live on, e.g. beta. Empty: upload only and set it live on Steamworks " +
                 "(SteamPipe never lets tools set the default branch live).")]
        public string setLiveBranch = "";

        [Tooltip("Counts Steam builds; shown in the build description on Steamworks.")]
        public int buildNumber;

        public uint DepotFor(SteamPlatform p) => p == SteamPlatform.Windows ? windowsDepot : p == SteamPlatform.MacOS ? macDepot : linuxDepot;

        public static SteamBuildSettings Get()
        {
            var s = AssetDatabase.LoadAssetAtPath<SteamBuildSettings>(AssetPath);
            if (s != null) return s;
            s = CreateInstance<SteamBuildSettings>();
            string dir = System.IO.Path.GetDirectoryName(AssetPath).Replace('\\', '/');
            if (!AssetDatabase.IsValidFolder(dir)) AssetDatabase.CreateFolder("Assets/_Project", "Editor");
            AssetDatabase.CreateAsset(s, AssetPath);
            AssetDatabase.SaveAssets();
            return s;
        }

        public void Save()
        {
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
        }
    }
}
