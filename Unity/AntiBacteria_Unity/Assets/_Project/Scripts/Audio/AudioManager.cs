using System.Collections.Generic;
using AntiBacteria.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AntiBacteria.Audio
{
    /// <summary>
    /// Plays sound effects from a pool of voices and crossfades music. Lives for the whole session (created
    /// automatically before the first scene). Follows the Sound / Music volumes in Options, pauses gameplay sounds
    /// with the game, and lowers the pitch in slow motion. Call <see cref="Sound.Play"/> from gameplay code.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        private AudioSource[] voices = new AudioSource[0];
        private bool[] voiceIsUi = new bool[0];
        private float[] voiceBasePitch = new float[0];
        private int nextVoice;
        private AudioSource musicA, musicB;
        private MusicTrack track = MusicTrack.None;
        private float fade = 1f; // 0..1, A fading out while B fades in after a swap
        private readonly Dictionary<Sfx, float> lastPlayed = new Dictionary<Sfx, float>();
        private float musicLevel = 1f;
        private AudioListener ears;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Create()
        {
            if (Instance != null) return;
            var go = new GameObject("AudioManager");
            DontDestroyOnLoad(go);
            Instance = go.AddComponent<AudioManager>();
        }

        private void Awake()
        {
            SoundSettings s = SoundSettings.Current;
            int n = Mathf.Max(4, s.voices);
            voices = new AudioSource[n];
            voiceIsUi = new bool[n];
            voiceBasePitch = new float[n];
            for (int i = 0; i < n; i++) voices[i] = NewSource("Sfx" + i, false);
            musicA = NewSource("MusicA", true);
            musicB = NewSource("MusicB", true);
            // The scenes have no AudioListener (the 2D cameras are built without one), and without a listener
            // Unity plays nothing at all. Every source here is 2D, so the listener can live on this object.
            ears = gameObject.AddComponent<AudioListener>();
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            if (Instance == this) Instance = null;
        }

        private AudioSource NewSource(string name, bool music)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.spatialBlend = 0f;
            src.loop = music;
            src.ignoreListenerPause = music; // music keeps going under the pause menu
            return src;
        }

        // ------------------------------------------------------------------ sound effects

        public void PlayEffect(Sfx id, float volumeScale, float pitch)
        {
            SoundSettings s = SoundSettings.Current;
            SfxEntry e = s.Get(id);
            if (e == null || e.clips == null || e.clips.Length == 0) return;
            float now = Time.unscaledTime;
            if (lastPlayed.TryGetValue(id, out float last) && now - last < e.minIntervalSeconds) return;
            lastPlayed[id] = now;

            AudioClip clip = e.clips[Random.Range(0, e.clips.Length)];
            if (clip == null) return;
            AudioSource src = TakeVoice();
            int i = System.Array.IndexOf(voices, src);
            voiceIsUi[i] = e.ui;
            voiceBasePitch[i] = pitch * (1f + Random.Range(-e.pitchJitter, e.pitchJitter));
            src.ignoreListenerPause = e.ui;
            src.clip = clip;
            src.volume = e.volume * volumeScale * s.sfxMaster * GameSettings.SfxVolume / 10f;
            src.pitch = voiceBasePitch[i] * (e.ui ? 1f : SlowPitch(s.slowMotionSfxPitch));
            src.Play();
        }

        /// <summary>A free voice, or the oldest one when all are busy.</summary>
        private AudioSource TakeVoice()
        {
            for (int k = 0; k < voices.Length; k++)
            {
                int i = (nextVoice + k) % voices.Length;
                if (!voices[i].isPlaying) { nextVoice = (i + 1) % voices.Length; return voices[i]; }
            }
            AudioSource oldest = voices[nextVoice];
            nextVoice = (nextVoice + 1) % voices.Length;
            return oldest;
        }

        private static float SlowPitch(float slowPitch) => Time.timeScale < 0.99f && Time.timeScale > 0f ? slowPitch : 1f;

        // ------------------------------------------------------------------ music

        public void PlayMusic(MusicTrack t)
        {
            if (t == track) return;
            track = t;
            AudioClip clip = SoundSettings.Current.Music(t);
            // Swap: the old track (now B) fades out, the new one (A) fades in.
            AudioSource old = musicA;
            musicA = musicB;
            musicB = old;
            musicA.clip = clip;
            if (clip != null) musicA.Play(); else musicA.Stop();
            fade = 0f;
        }

        /// <summary>Fade the music out (level over); the next scene starts its own track.</summary>
        public void StopMusic() => PlayMusic(MusicTrack.None);

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // Only one listener may be active: step aside if a scene brings its own.
            bool other = false;
            foreach (AudioListener l in FindObjectsByType<AudioListener>(FindObjectsSortMode.None))
                if (l != ears && l.enabled) other = true;
            if (ears != null) ears.enabled = !other;
            if (scene.name == SceneFlow.MainMenuScene) PlayMusic(MusicTrack.Menu);
            // Gameplay picks Level or Boss itself (CampaignLevelRunner knows which level it is).
        }

        private void Update()
        {
            SoundSettings s = SoundSettings.Current;
            AudioListener.pause = PauseController.IsPaused; // gameplay sounds stop with the game; UI and music don't

            float fadeTime = Mathf.Max(0.01f, s.fadeSeconds);
            fade = Mathf.MoveTowards(fade, 1f, Time.unscaledDeltaTime / fadeTime);
            float target = PauseController.IsPaused ? s.pausedMusicLevel : 1f;
            musicLevel = Mathf.MoveTowards(musicLevel, target, Time.unscaledDeltaTime * 2f);
            float vol = s.musicMaster * GameSettings.MusicVolume / 10f * musicLevel;
            musicA.volume = vol * fade;
            musicB.volume = vol * (1f - fade);
            if (fade >= 1f && musicB.isPlaying) musicB.Stop();
            float pitch = SlowPitch(s.slowMotionMusicPitch);
            musicA.pitch = Mathf.MoveTowards(musicA.pitch, pitch, Time.unscaledDeltaTime);
            musicB.pitch = musicA.pitch;

            // Voices already playing follow slow motion too.
            float sfxPitch = SlowPitch(s.slowMotionSfxPitch);
            for (int i = 0; i < voices.Length; i++)
                if (voices[i].isPlaying && !voiceIsUi[i]) voices[i].pitch = voiceBasePitch[i] * sfxPitch;
        }
    }

    /// <summary>The one call gameplay and menus use: <c>Sound.Play(Sfx.EnemyKill)</c>.</summary>
    public static class Sound
    {
        public static void Play(Sfx id, float volumeScale = 1f, float pitch = 1f)
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayEffect(id, volumeScale, pitch);
        }

        public static void Music(MusicTrack track)
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayMusic(track);
        }

        public static void StopMusic()
        {
            if (AudioManager.Instance != null) AudioManager.Instance.StopMusic();
        }
    }
}
