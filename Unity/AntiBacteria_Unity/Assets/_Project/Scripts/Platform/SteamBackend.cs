#if STEAMWORKS_NET && (UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX || STEAMWORKS_WIN || STEAMWORKS_LIN_OSX)
using System;
using System.Collections.Generic;
using Steamworks;
using UnityEngine;

namespace AntiBacteria.Platform
{
    /// <summary>
    /// Steam through Steamworks.NET: achievements, leaderboards (kept best) and Steam Cloud (ISteamRemoteStorage).
    /// Compiled only when the com.rlabrecque.steamworks.net package is installed (see the asmdef's version define).
    /// </summary>
    internal class SteamBackend : IPlatformBackend
    {
        public string Name => "Steam";

        private readonly Dictionary<string, SteamLeaderboard_t> boards = new Dictionary<string, SteamLeaderboard_t>();
        private readonly Dictionary<string, List<int>> pending = new Dictionary<string, List<int>>();
        private readonly List<object> calls = new List<object>(); // keep CallResults alive until they fire
        private Callback<GameOverlayActivated_t> overlay;
        private bool statsDirty;

        public static SteamBackend TryCreate(PlatformSettings s)
        {
            try
            {
                if (!Packsize.Test()) { Debug.LogError("Steam: wrong Steamworks.NET build for this platform."); return null; }
                if (!DllCheck.Test()) { Debug.LogError("Steam: steam_api library is the wrong version."); return null; }
#if !UNITY_EDITOR
                if (s.restartThroughSteam && SteamAPI.RestartAppIfNecessary(new AppId_t(s.steamAppId)))
                {
                    Application.Quit(); // Steam starts the game again
                    return null;
                }
#endif
                if (!SteamAPI.Init())
                {
                    Debug.Log("Steam: not available (Steam isn't running, or no steam_appid.txt next to the project). Playing offline.");
                    return null;
                }
            }
            catch (DllNotFoundException e)
            {
                Debug.LogError("Steam: steam_api library not found. " + e.Message);
                return null;
            }
            var b = new SteamBackend();
            b.overlay = Callback<GameOverlayActivated_t>.Create(r => PlatformServices.RaiseOverlay(r.m_bActive != 0));
            Debug.Log("Steam: signed in as " + SteamFriends.GetPersonaName() + ", app " + SteamUtils.GetAppID());
            return b;
        }

        public void Tick()
        {
            SteamAPI.RunCallbacks();
            if (statsDirty) statsDirty = !SteamUserStats.StoreStats();
        }

        public void Shutdown()
        {
            if (statsDirty) SteamUserStats.StoreStats();
            SteamAPI.Shutdown();
        }

        public void Unlock(string apiName)
        {
            if (SteamUserStats.GetAchievement(apiName, out bool done) && done) return;
            if (SteamUserStats.SetAchievement(apiName)) statsDirty = true; // stored (and the toast shown) on the next tick
            else Debug.LogWarning("Steam: no achievement named " + apiName + " in Steamworks.");
        }

        public void SubmitScore(string board, int score)
        {
            if (boards.TryGetValue(board, out SteamLeaderboard_t handle)) { Upload(handle, score); return; }
            if (pending.TryGetValue(board, out List<int> queue)) { queue.Add(score); return; } // lookup in flight
            pending[board] = new List<int> { score };
            SteamAPICall_t call = SteamUserStats.FindOrCreateLeaderboard(board,
                ELeaderboardSortMethod.k_ELeaderboardSortMethodDescending, ELeaderboardDisplayType.k_ELeaderboardDisplayTypeNumeric);
            CallResult<LeaderboardFindResult_t> result = null;
            result = CallResult<LeaderboardFindResult_t>.Create((r, failed) =>
            {
                calls.Remove(result);
                List<int> scores = pending[board];
                pending.Remove(board);
                if (failed || r.m_bLeaderboardFound == 0) { Debug.LogWarning("Steam: leaderboard " + board + " unavailable."); return; }
                boards[board] = r.m_hSteamLeaderboard;
                foreach (int sc in scores) Upload(r.m_hSteamLeaderboard, sc);
            });
            result.Set(call);
            calls.Add(result);
        }

        private void Upload(SteamLeaderboard_t handle, int score)
        {
            SteamAPICall_t call = SteamUserStats.UploadLeaderboardScore(handle,
                ELeaderboardUploadScoreMethod.k_ELeaderboardUploadScoreMethodKeepBest, score, null, 0);
            CallResult<LeaderboardScoreUploaded_t> result = null;
            result = CallResult<LeaderboardScoreUploaded_t>.Create((r, failed) => calls.Remove(result));
            result.Set(call);
            calls.Add(result);
        }

        public bool CloudAvailable => SteamRemoteStorage.IsCloudEnabledForAccount() && SteamRemoteStorage.IsCloudEnabledForApp();

        public void CloudWrite(string file, byte[] bytes)
        {
            if (!SteamRemoteStorage.FileWrite(file, bytes, bytes.Length)) Debug.LogWarning("Steam Cloud: could not write " + file);
        }

        public byte[] CloudRead(string file, out long unixTime)
        {
            unixTime = 0;
            if (!SteamRemoteStorage.FileExists(file)) return null;
            int size = SteamRemoteStorage.GetFileSize(file);
            if (size <= 0) return null;
            var bytes = new byte[size];
            if (SteamRemoteStorage.FileRead(file, bytes, size) != size) return null;
            unixTime = SteamRemoteStorage.GetFileTimestamp(file);
            return bytes;
        }
    }
}
#endif
