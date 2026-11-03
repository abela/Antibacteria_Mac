using System;
using System.Collections.Generic;
using UnityEngine;

namespace AntiBacteria.Platform
{
    /// <summary>One line of a leaderboard: global rank (1 = top), score and who.</summary>
    public struct ScoreRow
    {
        public int rank;
        public int score;
        public ulong user;
        public bool isMe;
    }

    /// <summary>A downloaded leaderboard: the top entries, and the player's own line (null if they have no score).</summary>
    public class ScorePage
    {
        public List<ScoreRow> top = new List<ScoreRow>();
        public ScoreRow? me;
    }

    /// <summary>What a store has to provide. Steam today; console stores later implement the same.</summary>
    public interface IPlatformBackend
    {
        string Name { get; }
        void Tick();
        void Shutdown();
        void Unlock(string apiName);
        void SubmitScore(string board, int score);
        /// <summary>Download the top <paramref name="count"/> and the player's own entry; null on failure.</summary>
        void FetchTop(string board, int count, Action<ScorePage> done);
        /// <summary>A player's display name, or "" / "[unknown]" until the store has sent it (then NamesChanged fires).</summary>
        string PlayerName(ulong user);
        /// <summary>Testing only: clear this account's achievements and stats.</summary>
        void ResetAllAchievements();
        bool IsUnlocked(string apiName);
        bool CloudAvailable { get; }
        void CloudWrite(string file, byte[] bytes);
        /// <summary>The file's bytes and its time (Unix seconds), or null if it isn't in the cloud.</summary>
        byte[] CloudRead(string file, out long unixTime);
    }

    /// <summary>
    /// The one place the game talks to a store. Without a store (editor without Steam running, consoles before
    /// their SDKs are in, DRM-free builds) every call is a no-op, so game code never checks.
    /// </summary>
    public static class PlatformServices
    {
        private static IPlatformBackend backend;

        public static string BackendName => backend != null ? backend.Name : "None";
        public static bool HasBackend => backend != null;

        /// <summary>The store overlay (Shift+Tab on Steam) opened or closed: pause the game while it is open.</summary>
        public static event Action<bool> OverlayChanged;

        internal static void RaiseOverlay(bool open) => OverlayChanged?.Invoke(open);

        /// <summary>Player names arrived from the store: redraw leaderboards.</summary>
        public static event Action NamesChanged;

        internal static void RaiseNames() => NamesChanged?.Invoke();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static void Init()
        {
            backend = null;
#if STEAMWORKS_NET && (UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX || STEAMWORKS_WIN || STEAMWORKS_LIN_OSX)
            backend = SteamBackend.TryCreate(PlatformSettings.Current);
#endif
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void CreatePump()
        {
            if (backend == null) return;
            var go = new GameObject("PlatformServices") { hideFlags = HideFlags.HideInHierarchy };
            UnityEngine.Object.DontDestroyOnLoad(go);
            go.AddComponent<Pump>();
        }

        private static void Shutdown()
        {
            backend?.Shutdown();
            backend = null;
        }

        public static void Unlock(Achievement a)
        {
            if (backend == null) return;
            string api = PlatformSettings.Current.ApiName(a);
            if (!string.IsNullOrEmpty(api)) backend.Unlock(api);
        }

        public static void SubmitScore(Board b, int score)
        {
            if (backend == null || score <= 0) return;
            string name = PlatformSettings.Current.BoardName(b);
            if (!string.IsNullOrEmpty(name)) backend.SubmitScore(name, score);
        }

        public static bool LeaderboardsAvailable => backend != null;

        /// <summary>The top <paramref name="count"/> of a board plus the player's own line. Calls back with null without a store or on failure.</summary>
        public static void FetchTop(Board b, int count, Action<ScorePage> done)
        {
            string name = backend != null ? PlatformSettings.Current.BoardName(b) : null;
            if (string.IsNullOrEmpty(name)) { done?.Invoke(null); return; }
            backend.FetchTop(name, count, done);
        }

        public static string PlayerName(ulong user) => backend != null ? backend.PlayerName(user) : "";

        /// <summary>Testing only (the Progress Tool): clear this account's achievements and stats.</summary>
        public static void ResetAllAchievements() => backend?.ResetAllAchievements();

        public static bool IsUnlocked(Achievement a)
        {
            string api = backend != null ? PlatformSettings.Current.ApiName(a) : null;
            return !string.IsNullOrEmpty(api) && backend.IsUnlocked(api);
        }

        public static bool CloudAvailable => backend != null && PlatformSettings.Current.useSteamCloud && backend.CloudAvailable;

        public static void CloudWrite(string file, byte[] bytes)
        {
            if (CloudAvailable) backend.CloudWrite(file, bytes);
        }

        public static byte[] CloudRead(string file, out long unixTime)
        {
            unixTime = 0;
            return CloudAvailable ? backend.CloudRead(file, out unixTime) : null;
        }

        private class Pump : MonoBehaviour
        {
            private void Update() => backend?.Tick();
            // Destroyed only when the game quits (it lives in DontDestroyOnLoad): after the save on quit has gone to the cloud.
            private void OnDestroy() => Shutdown();
        }
    }
}
