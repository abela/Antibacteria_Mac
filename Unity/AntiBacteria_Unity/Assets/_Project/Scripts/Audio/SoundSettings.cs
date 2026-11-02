using System;
using UnityEngine;

namespace AntiBacteria.Audio
{
    /// <summary>Every sound effect the game plays. Files: Assets/_Project/Audio/Sfx/sfx_&lt;snake_case&gt;.wav (+ _2, _3 ... variants).</summary>
    public enum Sfx
    {
        Shoot, ShootHeavy, EnemyHit, ShieldHit, EnemyKill, BossKill, WallHit, BulletCancel, EnemyShoot,
        PlayerHurt, PlayerDie, Vitamin, BonusPickup, Boom, SlowmoOn, SlowmoOff,
        GetReady, LevelWin, GameOver, BossAppear, BossShield,
        UiMove, UiConfirm, UiBack, UiError,
    }

    public enum MusicTrack { None, Menu, Level, Boss }

    [Serializable]
    public class SfxEntry
    {
        public Sfx id;
        [Tooltip("One is picked at random each time.")]
        public AudioClip[] clips = new AudioClip[0];
        [Range(0f, 1f)] public float volume = 1f;
        [Tooltip("Random pitch change, + or -, so repeated sounds don't machine-gun.")]
        [Range(0f, 0.5f)] public float pitchJitter = 0.05f;
        [Tooltip("The same sound never starts again sooner than this (a Tornado fires 33 volleys a second).")]
        [Range(0f, 0.5f)] public float minIntervalSeconds = 0.03f;
        [Tooltip("Menu sounds: keep playing while the game is paused and ignore slow motion.")]
        public bool ui;
    }

    /// <summary>
    /// Sound effects, music and how they behave. Edit in Project Settings > AntiBacteria > Audio.
    /// The 2013 game shipped silent (its SoundManager was never called); everything here is new.
    /// Lives in Resources so builds read the same values.
    /// </summary>
    [CreateAssetMenu(menuName = "AntiBacteria/Sound Settings", fileName = "SoundSettings")]
    public class SoundSettings : ScriptableObject
    {
        public const string ResourcePath = "SoundSettings";

        [Header("Mix")]
        [Tooltip("Overall level of all sound effects before the player's Sound volume.")]
        [Range(0f, 1f)] public float sfxMaster = 0.8f;
        [Tooltip("Overall level of the music before the player's Music volume.")]
        [Range(0f, 1f)] public float musicMaster = 0.6f;
        [Tooltip("Most sound effects playing at once.")]
        [Range(4, 64)] public int voices = 24;

        [Header("Music")]
        public AudioClip menuMusic;
        public AudioClip levelMusic;
        public AudioClip bossMusic;
        [Tooltip("Crossfade between tracks.")]
        [Range(0f, 4f)] public float fadeSeconds = 1f;
        [Tooltip("Music level while a menu is open over the game (pause, results).")]
        [Range(0f, 1f)] public float pausedMusicLevel = 0.4f;

        [Header("Slow motion")]
        [Tooltip("Music pitch while slow motion is on.")]
        [Range(0.5f, 1f)] public float slowMotionMusicPitch = 0.8f;
        [Tooltip("Sound-effect pitch while slow motion is on.")]
        [Range(0.5f, 1f)] public float slowMotionSfxPitch = 0.8f;

        [Header("Sound effects (clips filled in automatically from Audio/Sfx)")]
        public SfxEntry[] effects = Defaults();

        private static SoundSettings current;

        public static SoundSettings Current
        {
            get
            {
                if (current != null) return current;
                current = Resources.Load<SoundSettings>(ResourcePath);
                if (current == null)
                {
                    current = CreateInstance<SoundSettings>();
                    current.hideFlags = HideFlags.HideAndDontSave;
                }
                return current;
            }
        }

        public static void Use(SoundSettings settings) => current = settings;

        public SfxEntry Get(Sfx id)
        {
            foreach (SfxEntry e in effects) if (e != null && e.id == id) return e;
            return null;
        }

        public AudioClip Music(MusicTrack track) =>
            track == MusicTrack.Menu ? menuMusic : track == MusicTrack.Level ? levelMusic : track == MusicTrack.Boss ? bossMusic : null;

        private static SfxEntry E(Sfx id, float volume, float jitter = 0.05f, float interval = 0.03f, bool ui = false) =>
            new SfxEntry { id = id, volume = volume, pitchJitter = jitter, minIntervalSeconds = interval, ui = ui };

        public static SfxEntry[] Defaults() => new[]
        {
            E(Sfx.Shoot, 0.35f, 0.08f, 0.06f), E(Sfx.ShootHeavy, 0.6f, 0.05f, 0.1f),
            E(Sfx.EnemyHit, 0.5f, 0.12f, 0.04f), E(Sfx.ShieldHit, 0.45f, 0.1f, 0.06f),
            E(Sfx.EnemyKill, 0.8f, 0.1f, 0.03f), E(Sfx.BossKill, 1f, 0f, 0.5f),
            E(Sfx.WallHit, 0.25f, 0.15f, 0.07f), E(Sfx.BulletCancel, 0.4f, 0.1f, 0.04f),
            E(Sfx.EnemyShoot, 0.35f, 0.08f, 0.08f),
            E(Sfx.PlayerHurt, 1f, 0f, 0.2f), E(Sfx.PlayerDie, 1f, 0f, 0.5f),
            E(Sfx.Vitamin, 0.35f, 0f, 0.035f), E(Sfx.BonusPickup, 0.8f, 0f, 0.1f), E(Sfx.Boom, 1f, 0f, 0.2f),
            E(Sfx.SlowmoOn, 0.7f, 0f, 0.2f), E(Sfx.SlowmoOff, 0.7f, 0f, 0.2f),
            E(Sfx.GetReady, 0.7f, 0f, 0.5f, true), E(Sfx.LevelWin, 0.8f, 0f, 0.5f, true), E(Sfx.GameOver, 0.8f, 0f, 0.5f, true),
            E(Sfx.BossAppear, 0.9f, 0f, 0.5f), E(Sfx.BossShield, 0.8f, 0f, 0.3f),
            E(Sfx.UiMove, 0.5f, 0f, 0.03f, true), E(Sfx.UiConfirm, 0.6f, 0f, 0.05f, true),
            E(Sfx.UiBack, 0.6f, 0f, 0.05f, true), E(Sfx.UiError, 0.6f, 0f, 0.1f, true),
        };
    }
}
