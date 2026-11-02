using AntiBacteria.Audio;
using System;
using AntiBacteria.Core;
using AntiBacteria.Gameplay.Effects;
using AntiBacteria.Gameplay.Enemies;
using AntiBacteria.Gameplay.Player;
using AntiBacteria.Gameplay.Weapons;
using UnityEngine;

namespace AntiBacteria.Gameplay.Bonuses
{
    /// <summary>
    /// What the player has picked up. Timed effects each have their own slot and clock, so they never interfere
    /// (2013 bugs fixed: 13 a pickup cancelled post-hit invulnerability, 17 Life/Boom during a weapon bonus made it
    /// permanent, 18 a new bonus inherited the old one's remaining time). Clocks run in game time.
    /// Built to take more sources later (upgrade shop, roguelike perks).
    /// </summary>
    public class PlayerBuffs : MonoBehaviour
    {
        public enum Slot { Weapon, Immortality, Speed }
        public const int SlotCount = 3;

        private readonly float[] left = new float[SlotCount];
        private ShotKind weapon;
        private BonusType weaponBonus;

        /// <summary>The bonus weapon while it lasts, otherwise null (the player's own shot is used).</summary>
        public ShotKind? Weapon => left[(int)Slot.Weapon] > 0f ? weapon : (ShotKind?)null;
        public bool IsImmortal => left[(int)Slot.Immortality] > 0f;
        public float SpeedMultiplier => left[(int)Slot.Speed] > 0f ? BonusSettings.Current.speedMultiplier : 1f;

        public float TimeLeft(Slot slot) => left[(int)slot];

        /// <summary>The icon to show for a slot.</summary>
        public BonusType IconFor(Slot slot) =>
            slot == Slot.Weapon ? weaponBonus : slot == Slot.Immortality ? BonusType.Immortality : BonusType.DoubleSpeed;

        /// <summary>Raised on every pickup (HUD message).</summary>
        public event Action<BonusType> Picked;
        /// <summary>Raised when a slot starts or ends.</summary>
        public event Action Changed;

        private void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return; // paused
            bool ended = false;
            for (int i = 0; i < SlotCount; i++)
            {
                if (left[i] <= 0f) continue;
                left[i] -= dt;
                if (left[i] <= 0f) { left[i] = 0f; ended = true; }
            }
            if (ended) Changed?.Invoke();
        }

        public void Apply(BonusType type)
        {
            BonusSettings s = BonusSettings.Current;
            ShotKind? shot = BonusSettings.ShotFor(type);
            if (shot.HasValue)
            {
                weapon = shot.Value;
                weaponBonus = type;
                left[(int)Slot.Weapon] = s.durationSeconds * RunModifiers.BonusDurationMultiplier; // a new weapon replaces the old one, full time
            }
            else
            {
                switch (type)
                {
                    case BonusType.Life: GetComponent<PlayerController>()?.Heal(1); break;
                    case BonusType.Boom: Boom(s); break;
                    case BonusType.Immortality: left[(int)Slot.Immortality] = s.durationSeconds * RunModifiers.BonusDurationMultiplier; break;
                    case BonusType.DoubleSpeed: left[(int)Slot.Speed] = s.durationSeconds * RunModifiers.BonusDurationMultiplier; break;
                }
            }
            Picked?.Invoke(type);
            Changed?.Invoke();
        }

        /// <summary>
        /// Every enemy within the radius loses boomHits hits; kills pay their score (2013: no score, bug 27).
        /// Each chain segment is measured on its own (2013 bug 35 measured from the world origin and deleted whole chains).
        /// </summary>
        private void Boom(BonusSettings s)
        {
            Vector2 at = transform.position;
            float radius = Legacy.Units(s.boomRadiusPoints);
            foreach (Enemy e in FindObjectsByType<Enemy>(FindObjectsSortMode.None))
            {
                if (e == null || !e.CanBeHit) continue;
                if (Vector2.Distance(at, e.transform.position) > radius) continue;
                e.TakeHits(s.boomHits);
            }
            Fx.Sparks(at, Vector2.zero, 0f, s.boomSparks, 0.5f, 0.3f, 3.5f);
            CameraFollow.Shake(s.boomShakePixels, s.boomShakeSeconds);
            Sound.Play(Sfx.Boom);
        }

        /// <summary>Level over or restart.</summary>
        public void ClearAll()
        {
            for (int i = 0; i < SlotCount; i++) left[i] = 0f;
            Changed?.Invoke();
        }
    }
}
