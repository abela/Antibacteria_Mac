using System;
using System.Collections.Generic;
using UnityEngine;

namespace AntiBacteria.Core
{
    /// <summary>One caption line of the intro: shown from one frame to another, typed out letter by letter.</summary>
    [Serializable]
    public class IntroCaption
    {
        public int startFrame, endFrame;
        public WordIDs line;
        [Tooltip("Top of the line in the 320x180 frame.")]
        public float y = 166f;
    }

    /// <summary>
    /// The opening story: the doctor calls it a day and leaves a sample under the microscope, and in the dark one cell
    /// in it wakes up (the player). Frames are 320x180 palette indices made by Docs/Intro/export_frames4.py from the
    /// animatic (Docs/Intro/animatic4.py); captions are drawn by the game, translated. Edit in
    /// Project Settings > AntiBacteria > Intro.
    /// </summary>
    [CreateAssetMenu(menuName = "AntiBacteria/Intro Settings", fileName = "IntroSettings")]
    public class IntroSettings : ScriptableObject
    {
        public const string ResourcePath = "IntroSettings";

        [Header("When")]
        [Tooltip("Play before the main menu the first time the game starts, then go straight into Level 1.")]
        public bool playOnFirstLaunch = true;

        [Header("Media (empty = the files in Resources/Intro)")]
        public TextAsset frames;
        public AudioClip music, effects, sting;
        [Tooltip("Effects track loudness against the music track (the two follow the Sound and Music volumes).")]
        [Range(0f, 2f)] public float effectsBalance = 0.75f;

        [Header("Captions")]
        public List<IntroCaption> captions = new List<IntroCaption>
        {
            new IntroCaption { startFrame = 0, endFrame = 75, line = WordIDs.IntroLine1 },
            new IntroCaption { startFrame = 75, endFrame = 150, line = WordIDs.IntroLine2 },
            new IntroCaption { startFrame = 150, endFrame = 234, line = WordIDs.IntroLine3 },
            new IntroCaption { startFrame = 240, endFrame = 307, line = WordIDs.IntroLine4 },
            new IntroCaption { startFrame = 307, endFrame = 388, line = WordIDs.IntroLine5 },
        };
        [Tooltip("Seconds per letter as a caption types out.")]
        public float secondsPerLetter = 0.035f;
        [Tooltip("A soft blip every this many letters while typing (0 = silent).")]
        public int blipEveryLetters = 2;
        [Tooltip("The caption dims this long before its shot ends.")]
        public float dimBeforeEnd = 0.4f;

        [Header("Skip")]
        [Tooltip("Hold any button (or the mouse) this long to skip.")]
        public float holdToSkipSeconds = 1.0f;

        [Header("Into Level 1")]
        [Tooltip("The white flash fading into the level.")]
        public float whiteFadeSeconds = 0.9f;
        [Tooltip("\"YOU ARE THE CURE\" stays this long over the level's start.")]
        public float titleSeconds = 2.8f;

        public TextAsset Frames => frames != null ? frames : Resources.Load<TextAsset>("Intro/intro_frames");
        public AudioClip Music => music != null ? music : Resources.Load<AudioClip>("Intro/intro_music");
        public AudioClip Effects => effects != null ? effects : Resources.Load<AudioClip>("Intro/intro_sfx");
        public AudioClip Sting => sting != null ? sting : Resources.Load<AudioClip>("Intro/intro_sting");

        private static IntroSettings current;

        public static IntroSettings Current
        {
            get
            {
                if (current != null) return current;
                current = Resources.Load<IntroSettings>(ResourcePath);
                if (current == null)
                {
                    current = CreateInstance<IntroSettings>();
                    current.hideFlags = HideFlags.HideAndDontSave;
                }
                return current;
            }
        }

        public static void Use(IntroSettings settings) => current = settings;
    }

    /// <summary>When the intro plays and where it leads.</summary>
    public static class IntroFlow
    {
        private static bool shownThisSession;

        /// <summary>Level 1 was loaded straight from the intro: it fades in from the white flash.</summary>
        public static bool Handoff { get; private set; }

        /// <summary>First launch: the intro plays before the main menu.</summary>
        public static bool WantsLaunchIntro =>
            !shownThisSession && IntroSettings.Current.playOnFirstLaunch && !Progress.IntroSeen;

        public static void MarkShown()
        {
            shownThisSession = true;
            Progress.MarkIntroSeen();
        }

        /// <summary>The intro ended on first launch: straight into Level 1.</summary>
        public static void GoToLevelOne()
        {
            Handoff = true;
            SceneFlow.LoadLevel(1);
        }

        public static void ConsumeHandoff() => Handoff = false;
    }
}
