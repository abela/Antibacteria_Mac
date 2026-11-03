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
        // Lookups in flight: everyone waiting for the board's handle (null if Steam couldn't find it).
        private readonly Dictionary<string, List<Action<SteamLeaderboard_t?>>> waiting = new Dictionary<string, List<Action<SteamLeaderboard_t?>>>();
        private readonly List<object> calls = new List<object>(); // keep CallResults alive until they fire
        private Callback<GameOverlayActivated_t> overlay;
        private Callback<PersonaStateChange_t> persona;
        private bool statsDirty;

        public static SteamBackend TryCreate(PlatformSettings s)
        {
            try
            {
                if (!Packsize.Test()) { Debug.LogError("Steam: wrong Steamworks.NET build for this platform."); return null; }
                if (!DllCheck.Test()) { Debug.LogError("Steam: steam_api library is the wrong version."); return null; }
#if !UNITY_EDITOR && !DEVELOPMENT_BUILD
                if (s.restartThroughSteam && SteamAPI.RestartAppIfNecessary(new AppId_t(s.steamAppId)))
                {
                    Application.Quit(); // Steam starts the game again
                    return null;
                }
#endif
                ESteamAPIInitResult init = SteamAPI.InitEx(out string err);
                if (init != ESteamAPIInitResult.k_ESteamAPIInitResult_OK)
                {
                    Debug.Log("Steam: not available (" + init + ": " + err + "). Playing offline.");
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
            b.persona = Callback<PersonaStateChange_t>.Create(r =>
            {
                if ((r.m_nChangeFlags & EPersonaChange.k_EPersonaChangeName) != 0) PlatformServices.RaiseNames();
            });
            // Fetch this player's achievements and stats now (and the latest schema), so unlocks work from the first level.
            SteamUserStats.RequestUserStats(SteamUser.GetSteamID());
            Debug.Log("Steam: signed in as " + SteamFriends.GetPersonaName() + " (" + SteamUser.GetSteamID() + "), app " + SteamUtils.GetAppID() +
                      ", cloud " + (SteamRemoteStorage.IsCloudEnabledForAccount() ? "on" : "off for this account") + "/" +
                      (SteamRemoteStorage.IsCloudEnabledForApp() ? "on" : "off for this app") +
                      ", " + SteamUserStats.GetNumAchievements() + " achievements in Steamworks");
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

        public void SubmitScore(string board, int score) => WithBoard(board, h => { if (h.HasValue) Upload(h.Value, score); });

        /// <summary>Calls <paramref name="use"/> with the board's handle, looking it up (once) if needed.</summary>
        private void WithBoard(string board, Action<SteamLeaderboard_t?> use)
        {
            if (boards.TryGetValue(board, out SteamLeaderboard_t handle)) { use(handle); return; }
            if (waiting.TryGetValue(board, out List<Action<SteamLeaderboard_t?>> queue)) { queue.Add(use); return; } // lookup in flight
            waiting[board] = new List<Action<SteamLeaderboard_t?>> { use };
            SteamAPICall_t call = SteamUserStats.FindOrCreateLeaderboard(board,
                ELeaderboardSortMethod.k_ELeaderboardSortMethodDescending, ELeaderboardDisplayType.k_ELeaderboardDisplayTypeNumeric);
            CallResult<LeaderboardFindResult_t> result = null;
            result = CallResult<LeaderboardFindResult_t>.Create((r, failed) =>
            {
                calls.Remove(result);
                List<Action<SteamLeaderboard_t?>> users = waiting[board];
                waiting.Remove(board);
                SteamLeaderboard_t? found = null;
                if (failed || r.m_bLeaderboardFound == 0) Debug.LogWarning("Steam: leaderboard " + board + " unavailable.");
                else boards[board] = (found = r.m_hSteamLeaderboard).Value;
                foreach (Action<SteamLeaderboard_t?> u in users) u(found);
            });
            result.Set(call);
            calls.Add(result);
        }

        public void FetchTop(string board, int count, Action<ScorePage> done)
        {
            WithBoard(board, h =>
            {
                if (!h.HasValue) { done(null); return; }
                Download(h.Value, ELeaderboardDataRequest.k_ELeaderboardDataRequestGlobal, 1, count, top =>
                {
                    if (top == null) { done(null); return; }
                    // The player's own line, wherever they are on the board.
                    Download(h.Value, ELeaderboardDataRequest.k_ELeaderboardDataRequestGlobalAroundUser, 0, 0, around =>
                    {
                        var page = new ScorePage { top = top };
                        if (around != null) foreach (ScoreRow r in around) if (r.isMe) page.me = r;
                        done(page);
                    });
                });
            });
        }

        private void Download(SteamLeaderboard_t handle, ELeaderboardDataRequest kind, int from, int to, Action<List<ScoreRow>> done)
        {
            SteamAPICall_t call = SteamUserStats.DownloadLeaderboardEntries(handle, kind, from, to);
            CallResult<LeaderboardScoresDownloaded_t> result = null;
            result = CallResult<LeaderboardScoresDownloaded_t>.Create((r, failed) =>
            {
                calls.Remove(result);
                if (failed) { done(null); return; }
                ulong me = SteamUser.GetSteamID().m_SteamID;
                var rows = new List<ScoreRow>(r.m_cEntryCount);
                for (int i = 0; i < r.m_cEntryCount; i++)
                {
                    if (!SteamUserStats.GetDownloadedLeaderboardEntry(r.m_hSteamLeaderboardEntries, i, out LeaderboardEntry_t e, null, 0)) continue;
                    // Names of players we don't know yet arrive later through PersonaStateChange.
                    SteamFriends.RequestUserInformation(e.m_steamIDUser, true);
                    rows.Add(new ScoreRow { rank = e.m_nGlobalRank, score = e.m_nScore, user = e.m_steamIDUser.m_SteamID, isMe = e.m_steamIDUser.m_SteamID == me });
                }
                done(rows);
            });
            result.Set(call);
            calls.Add(result);
        }

        public string PlayerName(ulong user) => SteamFriends.GetFriendPersonaName(new CSteamID(user));

        private void Upload(SteamLeaderboard_t handle, int score)
        {
            SteamAPICall_t call = SteamUserStats.UploadLeaderboardScore(handle,
                ELeaderboardUploadScoreMethod.k_ELeaderboardUploadScoreMethodKeepBest, score, null, 0);
            CallResult<LeaderboardScoreUploaded_t> result = null;
            result = CallResult<LeaderboardScoreUploaded_t>.Create((r, failed) => calls.Remove(result));
            result.Set(call);
            calls.Add(result);
        }

        public void ResetAllAchievements()
        {
            if (SteamUserStats.ResetAllStats(true)) SteamUserStats.StoreStats();
            Debug.Log("Steam: achievements and stats reset for this account.");
        }

        public bool IsUnlocked(string apiName) => SteamUserStats.GetAchievement(apiName, out bool done) && done;

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
