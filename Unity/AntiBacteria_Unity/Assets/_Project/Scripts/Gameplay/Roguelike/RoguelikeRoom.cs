using AntiBacteria.Audio;
using AntiBacteria.Core;
using AntiBacteria.Gameplay.Bonuses;
using AntiBacteria.Gameplay.Enemies;
using AntiBacteria.Gameplay.Level;
using AntiBacteria.Gameplay.Modes;
using AntiBacteria.Gameplay.Player;
using UnityEngine;

namespace AntiBacteria.Gameplay.Roguelike
{
    /// <summary>
    /// One room of a roguelike run, played on campaign level N's data (enemy pool, wave pace by difficulty, wave
    /// growth, bosses on 10/20/30/40/50). A normal room sends a fixed number of enemies; it is cleared when they
    /// are all dead. A boss room is cleared when its boss falls. Then every orb flies in, lives are restored
    /// (boss rooms, First Aid), and a gate opens in the top wall to the next room. Room 50 cleared = run complete.
    /// Kills drop XP orbs; filling the XP bar pauses for a perk choice (GameplayMenuController).
    /// </summary>
    public class RoguelikeRoom : EndlessMode
    {
        private LevelData level;
        private int toSpawn, perWave = 1;
        private float waveTimer, growTimer, checkTimer, bossTimer;
        private bool bossRoom, bossSpawned, cleared;
        private RoomGate gate;
        private PerkEffects effects;

        public override bool UsesLevelData => true;
        public bool Cleared => cleared;

        /// <summary>Raised when the room is cleared (the HUD says so).</summary>
        public static event System.Action RoomCleared;

        private static Material Mat => BonusSettings.Current.material;

        public override void Begin(CampaignLevelRunner r)
        {
            base.Begin(r);
            level = r.Level;
            bossRoom = r.IsBossLevel;
            // Daily Run: the same spawns for everyone (the room's draws start from the day's seed).
            if (RoguelikeRun.IsDaily) Random.InitState(DailyRun.SeedFor(RoguelikeRun.DailyDate) + RoguelikeRun.Room * 7919);
            toSpawn = bossRoom ? 0 : RunModifiers.MoreEnemies(RoguelikeSettings.Current.EnemiesIn(RoguelikeRun.Room, GameSession.Difficulty));
            Enemy.HealthMultiplier = RoguelikeSettings.Current.HealthIn(RoguelikeRun.Room, GameSession.Difficulty, bossRoom) *
                                     RunModifiers.EnemyHealthMultiplier(bossRoom);
            Enemy.SpeedMultiplier = RunModifiers.EnemySpeedMultiplier;
            EnemyBullet.SpeedMultiplier = RunModifiers.EnemyBulletSpeedMultiplier;
            if (level != null) perWave = RoguelikeSettings.Current.FirstWave(RoguelikeRun.Room, level.maxEnemiesPerWave);
            waveTimer = 0.5f; // the first wave comes quickly
            RoguelikeRun.ShieldsLeft = RunModifiers.ShieldsPerRoom;
            RoguelikeRun.BodyguardLeft = RunModifiers.Bodyguard ? 1 : 0;
            effects = PerkEffects.Create(); // buddies, orbiters, chain zap, vampire
            Enemy.Killed += OnKilled;
            RoguelikeRun.PerkTaken += OnPerkTaken;
        }

        public override void End()
        {
            Enemy.Killed -= OnKilled;
            RoguelikeRun.PerkTaken -= OnPerkTaken;
            Enemy.HealthMultiplier = 1f;
            Enemy.SpeedMultiplier = 1f;
            EnemyBullet.SpeedMultiplier = 1f;
            if (gate != null) Object.Destroy(gate.gameObject);
            if (effects != null) Object.Destroy(effects.gameObject);
        }

        public override void Tick(float dt)
        {
            if (cleared || level == null) return;
            if (bossRoom)
            {
                if (!bossSpawned)
                {
                    bossTimer += dt;
                    if (bossTimer >= RoguelikeSettings.Current.bossDelaySeconds) { bossSpawned = true; runner.SpawnBossNow(); }
                }
                else if (runner.Boss == null || runner.Boss.IsDefeated) Clear();
                return;
            }

            if (toSpawn > 0)
            {
                // Same pace as the campaign level: waves on its interval, growing to its cap.
                if (level.growIntervalSeconds > 0f)
                {
                    growTimer += dt;
                    if (growTimer >= level.growIntervalSeconds)
                    {
                        growTimer -= level.growIntervalSeconds;
                        perWave = Mathf.Min(perWave + 1, Mathf.Max(1, level.maxEnemiesPerWave));
                    }
                }
                waveTimer -= dt;
                if (waveTimer <= 0f)
                {
                    waveTimer += level.SpawnInterval(GameSession.Difficulty);
                    int n = Mathf.Min(perWave, toSpawn);
                    for (int i = 0; i < n; i++)
                        runner.SpawnAtBorder(level.enemyPool[Random.Range(0, level.enemyPool.Length)]);
                    toSpawn -= n;
                }
                return;
            }
            checkTimer -= dt;
            if (checkTimer > 0f) return;
            checkTimer = 0.25f;
            if (Enemy.AliveCount == 0) Clear();
        }

        private void Clear()
        {
            cleared = true;
            EnemyBullet.ClearAll();
            XpOrb.CallAll();
            foreach (Pickups.Vitamin v in Object.FindObjectsByType<Pickups.Vitamin>(FindObjectsSortMode.None)) v.Attract(); // and the vitamins
            PlayerController p = PlayerController.Current;
            if (p != null && p.IsAlive && !RunModifiers.NoRoomHeal) // Mutation 8: no healing
            {
                int heal = (bossRoom ? RoguelikeSettings.Current.bossRoomHeal : 0) + (RunModifiers.HealOnRoomClear ? 1 : 0);
                if (heal > 0) p.Heal(heal);
            }
            AchievementTracker.RoomCleared(RoguelikeRun.Room, GameSession.Difficulty);
            if (RoguelikeRun.Room >= RoguelikeRun.Rooms)
            {
                if (!RoguelikeRun.IsDaily) Unlocks.RoguelikeCompleted(RoguelikeRun.Mutation); // the next Mutation opens
                runner.CompleteRun();
                return;
            } // the last boss: run won
            Sound.Play(Sfx.LevelWin);
            RoomCleared?.Invoke();
            gate = RoomGate.Spawn(Mat, () => runner.NextRoom());
        }

        private void OnKilled(Enemy e)
        {
            if (e == null || e.Definition == null) return;
            RoguelikeSettings s = RoguelikeSettings.Current;
            XpOrb.Spawn(e.transform.position, e.Definition.isBoss ? s.orbsPerBoss : s.orbsPerKill, Mat, e.Definition.isBoss);
        }

        /// <summary>Perks that act straight away.</summary>
        private void OnPerkTaken(PerkId perk)
        {
            PlayerController p = PlayerController.Current;
            if (p == null) return;
            switch (perk)
            {
                case PerkId.Vitality: p.Heal(1); break;          // max lives went up already (RunModifiers)
                case PerkId.Shield: RoguelikeRun.ShieldsLeft++; break;
                case PerkId.Bodyguard: RoguelikeRun.BodyguardLeft = 1; break;
            }
        }
    }
}
