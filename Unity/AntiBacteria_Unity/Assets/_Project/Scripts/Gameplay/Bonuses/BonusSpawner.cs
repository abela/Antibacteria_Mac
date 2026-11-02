using AntiBacteria.Core;
using AntiBacteria.Gameplay.Level;
using AntiBacteria.Gameplay.Player;
using UnityEngine;

namespace AntiBacteria.Gameplay.Bonuses
{
    /// <summary>
    /// Campaign bonus drops: one every spawnIntervalSeconds of play, 700-710 pt from the player, never on boss levels
    /// (CompaignGameMode.mm, Docs/DesignReference/01 section 8). Added by CampaignLevelRunner on normal levels.
    /// Fix: the timer runs in game time while Playing, instead of a real-time timer that kept ticking around pauses.
    /// </summary>
    public class BonusSpawner : MonoBehaviour
    {
        private CampaignLevelRunner runner;
        private float timer;

        private void Awake() => runner = GetComponent<CampaignLevelRunner>();

        private void Update()
        {
            if (runner == null || runner.Current != CampaignLevelRunner.Phase.Playing || PauseController.IsPaused) return;
            BonusSettings s = BonusSettings.Current;
            timer += Time.deltaTime;
            if (timer < s.spawnIntervalSeconds) return;
            timer -= s.spawnIntervalSeconds;
            Spawn();
        }

        public static BonusPickup Spawn(bool cycle = true)
        {
            BonusSettings s = BonusSettings.Current;
            PlayerController p = PlayerController.Current;
            Vector2 from = p != null ? (Vector2)p.transform.position : Vector2.zero;
            return BonusPickup.Spawn(s.RandomType(), SpawnPoint(from, s), cycle);
        }

        /// <summary>A point at the spawn distance from the player, inside the arena, and really not too close (bug 24).</summary>
        public static Vector2 SpawnPoint(Vector2 from, BonusSettings s)
        {
            Arena arena = Arena.Current;
            float margin = Legacy.Units(s.edgeMarginPoints);
            float minDistance = Legacy.Units(s.minDistanceFromPlayerPoints);
            Vector2 best = from;
            float bestDistance = -1f;
            for (int tries = 0; tries < 16; tries++)
            {
                float a = Random.Range(0f, Mathf.PI * 2f);
                float r = Legacy.Units(Random.Range(s.spawnDistanceMinPoints, s.spawnDistanceMaxPoints));
                Vector2 c = from + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
                if (arena != null) c = arena.Clamp(c, margin);
                float d = Vector2.Distance(c, from);
                if (d >= minDistance) return c;
                if (d > bestDistance) { bestDistance = d; best = c; }
            }
            return best; // cornered: the farthest point found
        }
    }
}
