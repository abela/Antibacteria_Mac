using System;
using AntiBacteria.Core;
using AntiBacteria.Input;
using UnityEngine;

namespace AntiBacteria.Gameplay.Player
{
    /// <summary>
    /// Space / right mouse / left trigger: everything runs at 0.35x for 10 s (real time), then 5 s to recharge.
    /// Original: GameManager.goBulletinTine + LevelOverLayer meter (Docs 01, section 2).
    /// </summary>
    public class SlowMotionAbility : MonoBehaviour
    {
        [SerializeField] private float slowTimeScale = 0.35f;
        [SerializeField] private float durationSeconds = 10f;
        [SerializeField] private float rechargeSeconds = 5f;

        /// <summary>1 = ready, drains while active, refills while recharging.</summary>
        public float Meter01 { get; private set; } = 1f;
        public bool IsActive { get; private set; }
        public bool IsReady => !IsActive && Meter01 >= 1f;
        public bool Enabled { get; set; } = true;

        public event Action<bool> ActiveChanged;

        private const float BaseFixedDelta = 1f / 60f; // constant, so a reload during slow motion can't capture the slowed step

        private void Update()
        {
            if (PauseController.IsPaused || PauseController.JustResumed) return; // Space = Confirm in menus too
            float dt = Time.unscaledDeltaTime;
            PlayerController player = PlayerController.Current;

            if (IsActive)
            {
                Meter01 -= dt / durationSeconds;
                if (Meter01 <= 0f || player == null || !player.IsAlive) SetActive(false);
            }
            else if (Meter01 < 1f)
            {
                Meter01 = Mathf.Min(1f, Meter01 + dt / rechargeSeconds);
            }
            else if (Enabled && GameInput.SlowMotionPressed && player != null && player.IsAlive)
            {
                SetActive(true);
            }
        }

        private void SetActive(bool active)
        {
            IsActive = active;
            if (!active) Meter01 = Mathf.Max(0f, Meter01);
            Time.timeScale = active ? slowTimeScale : 1f;
            Time.fixedDeltaTime = BaseFixedDelta * Time.timeScale; // keep physics smooth in slow motion
            ActiveChanged?.Invoke(active);
        }

        /// <summary>Ends slow motion now and blocks re-activation (level over).</summary>
        public void ForceEnd()
        {
            Enabled = false;
            if (IsActive) SetActive(false);
        }

        private void OnDisable()
        {
            if (IsActive) SetActive(false);
        }
    }
}
