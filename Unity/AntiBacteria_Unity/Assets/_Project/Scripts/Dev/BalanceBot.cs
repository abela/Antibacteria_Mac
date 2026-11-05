#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Text;
using AntiBacteria.Core;
using AntiBacteria.Gameplay.Enemies;
using AntiBacteria.Gameplay.Level;
using AntiBacteria.Gameplay.Player;
using AntiBacteria.Gameplay.Roguelike;
using AntiBacteria.Input;
using AntiBacteria.UI.Menus;
using UnityEngine;

namespace AntiBacteria.Dev
{
    /// <summary>
    /// Editor only: plays roguelike runs hands-free to measure balance. Aims at the nearest enemy, holds fire, dodges
    /// (ScreenshotDirector), takes a random perk at every level up and walks through the gate once a room is clear.
    /// The player can't die, but every hit it would have taken is counted (with the normal 2 s blink), so runs are
    /// comparable. Time runs on a fixed 60 steps a second as fast as the editor can draw (Time.captureFramerate).
    /// Steam is muted for the whole run: nothing reaches achievements, leaderboards or the cloud save.
    ///
    /// Per room it logs: XP level, perks owned, kills, XP orbs, seconds, would-be hits, average enemies alive and
    /// (boss rooms) seconds to kill the boss. CSV in Temp/claude/balance_[label].csv; a summary in the console.
    /// Start: BalanceBot.Run("label", rooms, difficulty, seed). It stops itself after the last room.
    /// It plays real rooms, so vitamins, plays and the tutorial flag reach the local save: back it up first.
    /// </summary>
    public class BalanceBot : MonoBehaviour
    {
        private static BalanceBot instance;
        public static bool Running => instance != null;
        public static string LastSummary { get; private set; }

        private string label;
        private int rooms, seed;
        private Difficulty difficulty;
        private int room = -1, kills0, orbs, hits0, levelAtStart;
        private float roomStart, aliveSum, bossAppear = -1f;
        private int aliveSamples;
        private float clearedAt = -1f;
        private bool roomBegun;
        private int lastPausedFrame = -100;
        private readonly StringBuilder csv = new StringBuilder();
        private readonly List<string> perkLog = new List<string>();

        public static void Run(string label, int rooms, Difficulty difficulty, int seed)
        {
            if (instance != null) return;
            var go = new GameObject("BalanceBot");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<BalanceBot>();
            instance.label = label;
            instance.rooms = Mathf.Clamp(rooms, 1, RoguelikeRun.Rooms);
            instance.difficulty = difficulty;
            instance.seed = seed;
            instance.csv.AppendLine("room,level,perks,kills,orbs,seconds,hits,avgAlive,bossSeconds,enemyHealthX");
            Random.InitState(seed);
            if (!Progress.TutorialDone) Progress.MarkTutorialDone(); // the tutorial would hold the room (back up the save first)
            ScreenshotDirector.Begin(); // aim + mute Steam + invincible
            ScreenshotDirector.AutoMove = true;
            ScreenshotDirector.Personal = 3f;
            ScreenshotDirector.OrbitRadius = 0.5f;
            GameInput.EditorFireOverride = true;
            PlayerController.EditorHitsTaken = 0;
            XpOrb.PickedUp += instance.OnOrb;
            Time.captureFramerate = 60;
            GameSession.Mode = GameMode.Roguelike;
            GameSession.Difficulty = difficulty;
            RoguelikeRun.Begin();
            SceneFlow.ContinueRun(); // loads the gameplay scene with the fresh run
        }

        public static void Stop()
        {
            if (instance == null) return;
            instance.Finish();
        }

        private void OnOrb() => orbs++;

        private void Update()
        {
            // Fire is held, but let go for a few frames after every menu: the game ignores a held button
            // after a resume until it is released (so the click that closed a menu doesn't shoot).
            if (PauseController.IsPaused || PauseController.JustResumed) lastPausedFrame = Time.frameCount;
            GameInput.EditorFireOverride = Time.frameCount - lastPausedFrame > 3;

            var runner = FindFirstObjectByType<CampaignLevelRunner>();
            if (runner == null) return;

            // A new room: reset the counters once play starts.
            if (RoguelikeRun.Room != room)
            {
                room = RoguelikeRun.Room;
                roomBegun = false;
            }
            if (!roomBegun && runner.Current == CampaignLevelRunner.Phase.Playing)
            {
                roomBegun = true;
                roomStart = Time.time;
                kills0 = GameSession.Kills;
                hits0 = PlayerController.EditorHitsTaken;
                orbs = 0;
                aliveSum = 0f; aliveSamples = 0;
                bossAppear = -1f;
                clearedAt = -1f;
                levelAtStart = RoguelikeRun.Level;
            }
            if (!roomBegun) return;

            // Level up on screen: take a random perk from the offer (as an average player would).
            if (RoguelikeRun.PendingLevelUps > 0 && PauseController.IsPaused) PickPerk();
            if (PauseController.IsPaused) return;

            if (runner.Current == CampaignLevelRunner.Phase.Playing)
            {
                aliveSum += Enemy.AliveCount; aliveSamples++;
                if (runner.IsBossLevel && bossAppear < 0f && runner.Boss != null) bossAppear = Time.time;
            }

            var mode = runner.Mode as RoguelikeRoom;
            bool cleared = (mode != null && mode.Cleared) || runner.Current == CampaignLevelRunner.Phase.Won;
            if (cleared && clearedAt < 0f) clearedAt = Time.time;
            // Give the orbs a moment to fly in, then log and go on.
            if (clearedAt >= 0f && Time.time - clearedAt > 1.5f && RoguelikeRun.PendingLevelUps == 0)
            {
                LogRoom(runner);
                if (room >= rooms || runner.Current == CampaignLevelRunner.Phase.Won) { Finish(); return; }
                runner.NextRoom();
                roomBegun = false;
            }
            if (runner.Current == CampaignLevelRunner.Phase.Lost) { LogRoom(runner); Finish(); }
        }

        private void PickPerk()
        {
            var gm = FindFirstObjectByType<GameplayMenuController>();
            if (gm == null) return;
            List<PerkId> offer = RoguelikeRun.Offer(RoguelikeSettings.Current.perkChoices);
            if (offer.Count == 0) { RoguelikeRun.ConsumeLevelUp(); return; }
            PerkId pick = offer[Random.Range(0, offer.Count)];
            perkLog.Add("r" + room + ":" + pick);
            typeof(GameplayMenuController).GetMethod("TakePerk", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                ?.Invoke(gm, new object[] { pick });
        }

        private void LogRoom(CampaignLevelRunner runner)
        {
            int perks = 0;
            foreach (PerkInfo p in Perks.All) perks += RoguelikeRun.Rank(p.id);
            float seconds = Time.time - roomStart;
            float boss = runner.IsBossLevel && bossAppear >= 0f && clearedAt >= 0f ? clearedAt - bossAppear : -1f;
            float avgAlive = aliveSamples > 0 ? aliveSum / aliveSamples : 0f;
            csv.AppendLine(string.Join(",", room, RoguelikeRun.Level, perks, GameSession.Kills - kills0, orbs, seconds.ToString("0.0"),
                PlayerController.EditorHitsTaken - hits0, avgAlive.ToString("0.0"), boss.ToString("0.0"),
                RoguelikeSettings.Current.HealthIn(room, difficulty).ToString("0.00")));
        }

        private void Finish()
        {
            XpOrb.PickedUp -= OnOrb;
            GameInput.EditorFireOverride = false;
            Time.captureFramerate = 0;
            ScreenshotDirector.End();
            // Stay muted until play mode ends: the menu and the quit both save, and test progress must not reach Steam Cloud.
            AntiBacteria.Platform.PlatformServices.EditorMuted = true;
            string dir = Path.GetFullPath("Temp/claude");
            Directory.CreateDirectory(dir);
            string path = Path.Combine(dir, "balance_" + label + ".csv");
            File.WriteAllText(path, csv.ToString() + "\nperks," + string.Join(" ", perkLog) + "\n");
            LastSummary = "BALANCEBOT " + label + " done: " + path + "\n" + csv;
            Debug.Log(LastSummary);
            instance = null;
            Destroy(gameObject);
            SceneFlow.LoadMainMenu();
        }

        private void OnDestroy()
        {
            if (instance == this) { instance = null; GameInput.EditorFireOverride = false; Time.captureFramerate = 0; }
        }
    }
}
#endif
