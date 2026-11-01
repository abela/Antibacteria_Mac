using System;
using UnityEngine;

namespace AntiBacteria.Gameplay.Enemies.Bosses
{
    /// <summary>What the level and the boss bar need to know about a boss.</summary>
    public interface IBoss
    {
        /// <summary>1 = full health, 0 = dead.</summary>
        float Health01 { get; }
        /// <summary>True while it can't be hurt (BigRedNoise spawning a wave): the bar shows it.</summary>
        bool Shielded { get; }
        bool IsDefeated { get; }
        event Action Defeated;
    }

    /// <summary>
    /// A single-body boss. Every way it can die goes through OnLethalHit, so the level always ends
    /// (2013: an explosion kill skipped the win and soft-locked the level). BigBoss (ID 22) uses this as is:
    /// it dashes between points, a third of them where the player stands (BigBoss.mm, Docs/DesignReference/03 6.1).
    /// </summary>
    public class BossEnemy : Enemy, IBoss
    {
        public float Health01 => IsDefeated ? 0f : Mathf.Clamp01((float)HitsLeft / Mathf.Max(1, MaxHits));
        public virtual bool Shielded => Invulnerable;
        public bool IsDefeated { get; private set; }
        public event Action Defeated;

        protected override void OnLethalHit()
        {
            if (IsDefeated) return;
            IsDefeated = true;
            OnDefeated();
            Die(true);
            Defeated?.Invoke();
        }

        /// <summary>Clean up minions before the boss is removed.</summary>
        protected virtual void OnDefeated() { }
    }
}
