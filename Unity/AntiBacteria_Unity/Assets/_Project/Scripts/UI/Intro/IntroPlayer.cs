using System;
using System.IO;
using System.IO.Compression;
using AntiBacteria.Audio;
using AntiBacteria.Core;
using AntiBacteria.Input;
using AntiBacteria.Rendering;
using AntiBacteria.UI.Menu;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AntiBacteria.UI.Intro
{
    /// <summary>
    /// Plays the opening story over everything on the menu canvas: 320x180 palette-index frames (crisp at any
    /// integer scale, in the current palette), the music and effects tracks, captions typed out in the game's font
    /// (translated), and "Hold to skip". Frames stream out of a deflated file one at a time; the music is the clock.
    /// </summary>
    public class IntroPlayer : MonoBehaviour
    {
        public static bool Playing { get; private set; }

        private const int HeaderSize = 13;

        private UISkin skin;
        private Action<bool> onDone; // true = skipped
        private IntroSettings cfg;

        private Texture2D tex;
        private Color32[] pixels;
        private readonly Color32[] lut = new Color32[4];
        private Stream stream;
        private byte[] packed;
        private int w, h, fps, frameCount, frame = -1;
        private float clock, held;
        private bool finished;

        private AudioSource music, effects;
        private GameObject audioGo; // survives the scene change so the flash's tail rings out
        private RawImage image;
        private Image band, skipBar;
        private TextMeshProUGUI caption, skipText;
        private int captionIndex = -1, lastBlip = -1;
        private RectTransform frameRt;
        private Canvas canvas;
        private float hintLeft = 4f; // "Hold to skip" shows at the start, then only while a button is held

        /// <summary>Cover the canvas that holds 'frameRoot' (the 320x180 layout) and play. onDone(skipped).</summary>
        public static IntroPlayer Play(UISkin skin, RectTransform frameRoot, Action<bool> onDone)
        {
            Transform canvas = frameRoot.parent;
            RectTransform overlay = UIFactory.Stretch(canvas, "Intro");
            overlay.SetAsLastSibling();
            UIFactory.Tint(overlay.gameObject.AddComponent<Image>().gameObject, PaletteRole.Background, false);
            overlay.GetComponent<Image>().sprite = skin.pixel;
            overlay.GetComponent<Image>().raycastTarget = true; // nothing underneath can be clicked

            // A 320x180 frame in the middle of the screen, scaled up by whole numbers (see FitToScreen).
            var frame = new GameObject("Frame", typeof(RectTransform)).GetComponent<RectTransform>();
            frame.SetParent(overlay, false);
            frame.anchorMin = frame.anchorMax = frame.pivot = new Vector2(0.5f, 0.5f);
            frame.sizeDelta = new Vector2(320f, 180f);

            var p = overlay.gameObject.AddComponent<IntroPlayer>();
            p.skin = skin;
            p.onDone = onDone;
            p.frameRt = frame;
            p.canvas = canvas.GetComponentInParent<Canvas>();
            p.Build(frame);
            p.FitToScreen();
            return p;
        }

        private void Build(RectTransform frame)
        {
            cfg = IntroSettings.Current;
            TextAsset file = cfg.Frames;
            byte[] bytes = file != null ? file.bytes : null;
            if (bytes == null || bytes.Length < HeaderSize || bytes[0] != 'A' || bytes[1] != 'B' || bytes[2] != 'I' || bytes[3] != 'N')
            {
                Debug.LogWarning("Intro: no frames (Resources/Intro/intro_frames.bytes) - skipping the intro.");
                finished = true;
                return;
            }
            w = BitConverter.ToUInt16(bytes, 6);
            h = BitConverter.ToUInt16(bytes, 8);
            fps = bytes[10];
            frameCount = BitConverter.ToUInt16(bytes, 11);
            packed = new byte[w * h / 4];
            pixels = new Color32[w * h];
            stream = new DeflateStream(new MemoryStream(bytes, HeaderSize, bytes.Length - HeaderSize), CompressionMode.Decompress);

            tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            RectTransform imgRt = UIFactory.Rect(frame, "Picture", 0f, 0f, w, h, new Vector2(0f, 1f));
            image = imgRt.gameObject.AddComponent<RawImage>();
            image.texture = tex;
            image.raycastTarget = false;

            band = UIFactory.Image(frame, "CaptionBand", skin.pixel, 0f, 160f, 320f, 15f, PaletteRole.Background);
            caption = UIFactory.Text(skin, frame, "Caption", 160f, 163f, HorizontalAlignmentOptions.Center);
            band.gameObject.SetActive(false);
            caption.gameObject.SetActive(false);
            skipText = UIFactory.Text(skin, frame, "Skip", 314f, 2f, HorizontalAlignmentOptions.Right, 1, PaletteRole.Foreground, true);
            skipText.text = Loc.Get(WordIDs.IntroHoldToSkip);
            skipBar = UIFactory.Image(frame, "SkipBar", skin.pixel, 314f, 14f, 1f, 1f, PaletteRole.Accent, false, new Vector2(1f, 1f));
            skipBar.gameObject.SetActive(false);

            ReadPalette(PaletteManager.Current);
            PaletteManager.Changed += ReadPalette;

            // Music and effects, each following its own volume setting.
            Sound.StopMusic();
            audioGo = new GameObject("IntroAudio");
            DontDestroyOnLoad(audioGo);
            music = audioGo.AddComponent<AudioSource>();
            effects = audioGo.AddComponent<AudioSource>();
            foreach (AudioSource s in new[] { music, effects })
            {
                s.playOnAwake = false; s.loop = false; s.spatialBlend = 0f; s.ignoreListenerPause = true;
            }
            music.clip = cfg.Music;
            effects.clip = cfg.Effects;
            UpdateVolumes();
            if (music.clip != null) music.Play();
            if (effects.clip != null) effects.Play();

            Playing = true;
            NextFrame();
        }

        private void OnDestroy()
        {
            PaletteManager.Changed -= ReadPalette;
            stream?.Dispose();
            if (tex != null) Destroy(tex);
            if (!finished && audioGo != null) Destroy(audioGo); // torn down mid-play
            Playing = false;
        }

        private void ReadPalette(Palette p)
        {
            if (p == null) return;
            lut[0] = p.background;
            lut[1] = PaletteGraphic.Resolve(p, PaletteRole.Foreground, true);
            lut[2] = p.foreground;
            lut[3] = p.accent;
            if (frame >= 0) Paint();
        }

        private void UpdateVolumes()
        {
            SoundSettings s = SoundSettings.Current;
            if (music != null) music.volume = s.musicMaster * GameSettings.MusicVolume / 10f;
            if (effects != null) effects.volume = Mathf.Clamp01(s.sfxMaster * GameSettings.SfxVolume / 10f * cfg.effectsBalance);
        }

        /// <summary>
        /// Fill the screen at the biggest whole-number scale (6x at 1080p, 12x at 4K), so every pixel of the
        /// 320x180 picture stays square and sharp.
        /// </summary>
        private void FitToScreen()
        {
            if (frameRt == null) return;
            float sf = canvas != null ? canvas.scaleFactor : 1f;
            int k = Mathf.Max(1, Mathf.FloorToInt(Mathf.Min(Screen.width / 320f, Screen.height / 180f)));
            float s = k / Mathf.Max(0.0001f, sf);
            frameRt.localScale = new Vector3(s, s, 1f);
            // Centre on a whole screen pixel.
            float ox = (Screen.width - k * 320) % 2 == 0 ? 0f : 0.5f / sf, oy = (Screen.height - k * 180) % 2 == 0 ? 0f : 0.5f / sf;
            frameRt.anchoredPosition = new Vector2(ox, oy);
        }

        private void Update()
        {
            if (finished || stream == null) return;
            float dt = Time.unscaledDeltaTime;
            FitToScreen();
            UpdateVolumes();

            // The music is the clock (keeps picture and sound together); without it, real time.
            clock = music != null && music.isPlaying ? music.time : clock + dt;
            int target = Mathf.Min(frameCount - 1, Mathf.FloorToInt(clock * fps));
            while (frame < target) NextFrame();
            UpdateCaption();

            // Hold any button or the mouse to skip; a small bar fills under the hint.
            bool pressing = GameInput.AnyButtonHeld;
            if (pressing) { held += dt; hintLeft = 2f; } else held = Mathf.Max(0f, held - dt * 2f);
            hintLeft -= dt;
            skipText.gameObject.SetActive(hintLeft > 0f);
            float k = Mathf.Clamp01(held / Mathf.Max(0.05f, cfg.holdToSkipSeconds));
            skipBar.gameObject.SetActive(k > 0f);
            skipBar.rectTransform.sizeDelta = new Vector2(Mathf.Round(UIFactory.Width(skipText, skipText.text) * k), 1f);
            if (k >= 1f) { Finish(true); return; }

            if (frame >= frameCount - 1 && clock >= frameCount / (float)fps) Finish(false);
        }

        private void NextFrame()
        {
            int read = 0;
            while (read < packed.Length)
            {
                int n = stream.Read(packed, read, packed.Length - read);
                if (n <= 0) break;
                read += n;
            }
            frame++;
            Paint();
        }

        /// <summary>Unpack 2-bit indices (top row first) into the texture (bottom row first).</summary>
        private void Paint()
        {
            for (int y = 0; y < h; y++)
            {
                int src = y * w, dst = (h - 1 - y) * w;
                for (int x = 0; x < w; x += 4)
                {
                    byte b = packed[(src + x) >> 2];
                    pixels[dst + x] = lut[b & 3];
                    pixels[dst + x + 1] = lut[(b >> 2) & 3];
                    pixels[dst + x + 2] = lut[(b >> 4) & 3];
                    pixels[dst + x + 3] = lut[(b >> 6) & 3];
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply(false);
        }

        private void UpdateCaption()
        {
            int idx = -1;
            for (int i = 0; i < cfg.captions.Count; i++)
                if (frame >= cfg.captions[i].startFrame && frame < cfg.captions[i].endFrame) { idx = i; break; }
            if (idx != captionIndex)
            {
                captionIndex = idx;
                lastBlip = -1;
                bool on = idx >= 0;
                band.gameObject.SetActive(on);
                caption.gameObject.SetActive(on);
                if (on)
                {
                    float y = cfg.captions[idx].y;
                    caption.text = Loc.Get(cfg.captions[idx].line);
                    // A long translation wraps onto a second line, growing upward.
                    caption.textWrappingMode = TextWrappingModes.Normal;
                    caption.rectTransform.sizeDelta = new Vector2(308f, caption.rectTransform.sizeDelta.y);
                    caption.lineSpacing = -18.75f;
                    bool two = UIFactory.Width(caption, caption.text) > 308f;
                    float top = two ? y - 13f : y;
                    caption.rectTransform.anchoredPosition = new Vector2(caption.rectTransform.anchoredPosition.x, -top);
                    caption.rectTransform.sizeDelta = new Vector2(308f, two ? 28f : 16f);
                    band.rectTransform.anchoredPosition = new Vector2(0f, -(top - 3f));
                    band.rectTransform.sizeDelta = new Vector2(320f, two ? 30f : 17f);
                }
            }
            if (idx < 0) return;
            IntroCaption c = cfg.captions[idx];
            string full = Loc.Get(c.line);
            float t = (frame - c.startFrame) / (float)fps + (clock - frame / (float)fps);
            int shown = Mathf.Clamp(Mathf.CeilToInt(t / Mathf.Max(0.001f, cfg.secondsPerLetter)), 0, full.Length);
            caption.maxVisibleCharacters = shown;
            bool dim = (c.endFrame - frame) / (float)fps <= cfg.dimBeforeEnd;
            caption.GetComponent<PaletteGraphic>().Set(PaletteRole.Foreground, dim);
            if (cfg.blipEveryLetters > 0 && shown < full.Length && shown / cfg.blipEveryLetters != lastBlip)
            {
                lastBlip = shown / cfg.blipEveryLetters;
                if (shown > 0 && !char.IsWhiteSpace(full[shown - 1])) Sound.Play(Sfx.UiMove, 0.35f, 1.7f);
            }
        }

        private void Finish(bool skipped)
        {
            if (finished) return;
            finished = true;
            Playing = false;
            // The tracks ring out across the scene change (the flash's tail), or fade fast on a skip.
            if (audioGo != null) audioGo.AddComponent<IntroAudioTail>().Begin(skipped ? 0.3f : 2.0f);
            onDone?.Invoke(skipped);
            Destroy(gameObject);
        }
    }

    /// <summary>Lets the intro's sound ring out through the scene load, fading, then removes it.</summary>
    public class IntroAudioTail : MonoBehaviour
    {
        private AudioSource[] sources;
        private float[] start;
        private float left, total;

        public void Begin(float fadeSeconds)
        {
            sources = GetComponents<AudioSource>();
            start = new float[sources.Length];
            for (int i = 0; i < sources.Length; i++) start[i] = sources[i].volume;
            left = total = Mathf.Max(0.05f, fadeSeconds);
        }

        private void Update()
        {
            left -= Time.unscaledDeltaTime;
            for (int i = 0; i < sources.Length; i++) if (sources[i] != null) sources[i].volume = start[i] * Mathf.Clamp01(left / total);
            if (left <= 0f) Destroy(gameObject);
        }
    }

    /// <summary>
    /// Level 1 straight after the intro: the screen fades in from the white flash, "YOU ARE THE CURE" stands over the
    /// opening seconds, and the closing hit plays.
    /// </summary>
    public class IntroHandoff : MonoBehaviour
    {
        private Image white;
        private GameObject title;
        private float age;
        private IntroSettings cfg;

        public static void Create(UISkin skin, RectTransform frameRoot)
        {
            IntroFlow.ConsumeHandoff();
            RectTransform overlay = UIFactory.Stretch(frameRoot.parent, "IntroHandoff");
            overlay.SetAsLastSibling();
            var h = overlay.gameObject.AddComponent<IntroHandoff>();
            h.cfg = IntroSettings.Current;
            h.white = overlay.gameObject.AddComponent<Image>();
            h.white.sprite = skin.pixel;
            h.white.raycastTarget = false;
            UIFactory.Tint(overlay.gameObject, PaletteRole.Foreground, false);
            h.title = LiveText.Create(skin, frameRoot, "YouAreTheCure", 160f, 34f, () => Loc.Get(WordIDs.IntroCure).ToUpperInvariant(),
                scale: 2, role: PaletteRole.Accent, maxWidth: 300f).gameObject;
            AudioClip sting = h.cfg.Sting;
            if (sting != null)
            {
                AudioSource src = overlay.gameObject.AddComponent<AudioSource>();
                src.clip = sting; src.spatialBlend = 0f; src.ignoreListenerPause = true;
                src.volume = Mathf.Clamp01(SoundSettings.Current.sfxMaster * GameSettings.SfxVolume / 10f * h.cfg.effectsBalance * 1.2f);
                src.Play();
            }
        }

        private void Update()
        {
            age += Time.unscaledDeltaTime;
            // The flash fades in a few hard steps, like the rest of the pixel art.
            float k = Mathf.Clamp01(age / Mathf.Max(0.05f, cfg.whiteFadeSeconds));
            float stepped = Mathf.Floor((1f - k) * 4f) / 4f;
            Color c = white.color; c.a = stepped; white.color = c;
            white.enabled = stepped > 0f;
            if (title != null && age > cfg.titleSeconds) { Destroy(title); title = null; }
            if (title == null && !white.enabled && age > cfg.titleSeconds + 1f) Destroy(gameObject, 2.5f);
        }

        private void OnDestroy()
        {
            if (title != null) Destroy(title);
        }
    }
}
