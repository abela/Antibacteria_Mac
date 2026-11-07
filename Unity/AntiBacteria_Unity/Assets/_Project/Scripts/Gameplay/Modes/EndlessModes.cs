using AntiBacteria.Audio;
using AntiBacteria.Core;
using AntiBacteria.Gameplay.Bonuses;
using AntiBacteria.Gameplay.Level;
using AntiBacteria.Gameplay.Player;
using AntiBacteria.Gameplay.Weapons;
using UnityEngine;

namespace AntiBacteria.Gameplay.Modes
{
    /// <summary>
    /// Rules of an endless mode, run by CampaignLevelRunner instead of a level's waves. No win: the run ends when the
    /// player dies (or, in Peace Walker, when the clock runs out) and the score is the result.
    /// Port of TakeTheFlagGameMode, RushGameMode and PeceWalkerGameMode (Docs/DesignReference/04, sections 2-7),
    /// with every frame counter turned into seconds of game time.
    /// </summary>
    public abstract class EndlessMode
    {
        protected CampaignLevelRunner runner;
        protected static ModeSettings S => ModeSettings.Current;

        /// <summary>The HUD clock is shown (Peace Walker's countdown).</summary>
        public virtual bool ShowsTimer => false;
        public virtual bool PlayerCanShoot => true;
        /// <summary>Played on a campaign level's data (the roguelike's rooms), not on the mode's own spawns.</summary>
        public virtual bool UsesLevelData => false;
        /// <summary>Flags taken or zones filled this run (achievements).</summary>
        public int Completed { get; protected set; }

        public static EndlessMode For(GameMode mode)
        {
            switch (mode)
            {
                case GameMode.CaptureFlag: // Capture The Flag plays the Rush rules
                case GameMode.Rush: return new RushMode(); // Rush is no longer in the menu
                case GameMode.PeaceWalker: return new PeaceWalkerMode();
                case GameMode.Roguelike: return new Roguelike.RoguelikeRoom();
                default: return null;
            }
        }

        public virtual void Begin(CampaignLevelRunner r) => runner = r;
        public abstract void Tick(float dt);
        /// <summary>The run is over: remove the objective.</summary>
        public virtual void End() { }

        /// <summary>The middle of the arena: 2013's "screen centre" (winSize / 2) was a fixed world point, the arena centre.</summary>
        protected static Vector2 ArenaCentre => Arena.Current != null ? Arena.Current.Bounds.center : Vector2.zero;

        /// <summary>A point a given distance from the arena centre, inside the arena (SpawnManager getRandomPointAroundPoint).
        /// 2013 measured from winSize / 2, a fixed point, not from the camera, which follows the player.</summary>
        protected static Vector2 AroundScreenCentre(float minPoints, float maxPoints, float marginUnits)
        {
            Vector2 centre = ArenaCentre;
            float a = Random.Range(0f, Mathf.PI * 2f);
            Vector2 p = centre + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * Legacy.Units(Random.Range(minPoints, maxPoints));
            return Arena.Current != null ? Arena.Current.Clamp(p, marginUnits) : p;
        }
    }

    /// <summary>
    /// Capture the Flag and Rush share this: waves on a timer, one objective at a time, and a ramp counted per objective.
    /// Fixes: bug 29 (the pool index ran one past the 19-entry table; enemies per wave grew without limit) and
    /// bug 20 (the volley "cap" didn't cap).
    /// </summary>
    public abstract class ObjectiveMode : EndlessMode
    {
        private float waveTimer, interval, objectiveTimer;
        private int perWave = 1, unlocked, objectives;
        protected bool HasObjective { get; set; }

        public override void Begin(CampaignLevelRunner r)
        {
            base.Begin(r);
            interval = S.waveIntervalSeconds;
        }

        public override void Tick(float dt)
        {
            waveTimer += dt;
            if (waveTimer >= interval)
            {
                waveTimer = 0f;
                int last = Mathf.Clamp(unlocked, 0, S.enemyPool.Length - 1);
                for (int i = 0; i < perWave; i++) runner.SpawnAtBorder(S.enemyPool[Random.Range(0, last + 1)]);
            }
            if (HasObjective) return;
            objectiveTimer += dt;
            if (objectiveTimer < S.objectiveDelaySeconds) return;
            objectiveTimer = 0f;
            objectives++;
            HasObjective = true;
            SpawnObjective();
            Ramp(objectives);
            waveTimer = 0f; // 2013: a new objective also delayed the next wave
        }

        private void Ramp(int n)
        {
            if (n % S.unlockEvery == 0) unlocked = Mathf.Min(unlocked + 1, S.enemyPool.Length - 1);
            if (n % S.speedUpEvery == 0)
            {
                interval = Mathf.Max(S.minWaveIntervalSeconds, interval - S.waveIntervalStepSeconds);
                perWave = Mathf.Min(S.maxEnemiesPerWave, perWave + 1);
            }
            if (n % S.levelUpEvery == 0)
            {
                PlayerController p = PlayerController.Current;
                if (p != null) p.VolleySize = Mathf.Min(WeaponSettings.Current.maxVolley, p.VolleySize + 1);
                BonusSpawner.Spawn(false); // the type is fixed outside the campaign
            }
        }

        protected abstract void SpawnObjective();

        protected void ObjectiveDone(int points)
        {
            GameSession.AddScore(points);
            HasObjective = false;
            Completed++;
        }
    }

    /// <summary>
    /// Rush rules: stand in the ring around a flag until it fills. Shown to the player as "Capture The Flag"
    /// (GameMode.CaptureFlag): the old touch-a-flag rules played almost the same and were dropped.
    /// </summary>
    public class RushMode : ObjectiveMode
    {
        private RushZone zone;

        protected override void SpawnObjective()
        {
            float margin = Legacy.Units(S.zoneRadiusPoints) + 0.5f;
            zone = RushZone.Spawn(AroundScreenCentre(S.zoneDistanceMinPoints, S.zoneDistanceMaxPoints, margin));
            zone.Completed += () =>
            {
                Sound.Play(Sfx.LevelWin);
                ObjectiveDone(S.zonePoints); // only a completed zone pays (bug 50)
            };
        }

        public override void End()
        {
            if (zone != null) Object.Destroy(zone.gameObject);
        }
    }

    /// <summary>
    /// No shooting: reach each white flag before the clock runs out while Tentacles pile up. Every flag resets the
    /// clock, adds a Tentacle and (after the first) drops vitamins near the centre; every 8th brings a +1 Life.
    /// After the first flag, chasers come in from the walls and hunt the player; they can't be shot, so each pops after
    /// a few seconds. More, faster and longer-lived on Hard and Impossible, and more often with every flag.
    /// </summary>
    public class PeaceWalkerMode : EndlessMode
    {
        private FlagObjective flag;
        private float flagTimer, chaserTimer;
        private bool chasersStarted;
        private int flags;
        private readonly System.Collections.Generic.List<GameObject> chasers = new System.Collections.Generic.List<GameObject>();

        public override bool ShowsTimer => true;
        public override bool PlayerCanShoot => false;

        public override void Begin(CampaignLevelRunner r)
        {
            base.Begin(r);
            runner.TimeLeft = S.secondsPerFlag;
            for (int i = 0; i < S.startTentacles; i++) runner.SpawnAtBorder(S.tentacleId);
        }

        public override void Tick(float dt)
        {
            runner.TimeLeft = Mathf.Max(0f, runner.TimeLeft - dt);
            if (runner.TimeLeft <= 0f) { runner.LoseByTimeOut(); return; }
            TickChasers(dt);
            if (flag != null) return;
            flagTimer += dt;
            if (flagTimer < S.objectiveDelaySeconds) return;
            flagTimer = 0f;
            flags++;
            Vector2 centre = ArenaCentre;
            flag = FlagObjective.Spawn(S.peaceFlag, AroundScreenCentre(S.flagDistanceMinPoints, S.flagDistanceMaxPoints, 1f));
            flag.Taken += () =>
            {
                Sound.Play(Sfx.BonusPickup);
                GameSession.AddScore(S.peaceFlagPoints);
                runner.TimeLeft = S.secondsPerFlag;
                flag = null;
                Completed++;
            };
            if (flags > 1 && RewardSpawner.Instance != null) RewardSpawner.Instance.DropVitamins(centre, S.vitaminsPerFlag);
            runner.SpawnAtBorder(S.tentacleId);
            if (flags % S.lifeEvery == 0)
            {
                PlayerController p = PlayerController.Current;
                Vector2 from = p != null ? (Vector2)p.transform.position : centre;
                BonusPickup.Spawn(BonusType.Life, BonusSpawner.SpawnPoint(from, BonusSettings.Current), false);
            }
        }

        private void TickChasers(float dt)
        {
            if (Completed < S.chasersFromFlag) return;
            if (!chasersStarted) { chasersStarted = true; chaserTimer = S.firstChaserSeconds; }
            chasers.RemoveAll(c => c == null);
            chaserTimer -= dt;
            if (chaserTimer > 0f) return;
            float every = ModeSettings.ByDifficulty(S.chaserEverySeconds, 6f) - S.chaserEveryStepSeconds * (Completed - S.chasersFromFlag);
            chaserTimer = Mathf.Max(ModeSettings.ByDifficulty(S.chaserEveryMinSeconds, 3f), every);
            if (chasers.Count >= ModeSettings.ByDifficulty(S.chaserMax, 3)) return;
            GameObject go = runner.SpawnAtBorder(S.chaserId);
            if (go == null) return;
            if (go.GetComponent<Enemies.Enemy>() is Enemies.Enemy e) e.SpeedOverridePoints = ModeSettings.ByDifficulty(S.chaserSpeedPoints, 200f);
            go.AddComponent<Expire>().Seconds = ModeSettings.ByDifficulty(S.chaserLifeSeconds, 8f);
            chasers.Add(go);
        }

        public override void End()
        {
            if (flag != null) Object.Destroy(flag.gameObject);
        }
    }
}
