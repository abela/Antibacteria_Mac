using System.Collections.Generic;
using AntiBacteria.Gameplay.Player;
using AntiBacteria.Input;
using AntiBacteria.Rendering;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace AntiBacteria.UI.Menu
{
    /// <summary>
    /// The game's own mouse cursor, drawn in game pixels at the same whole-number scale as everything else and
    /// coloured by the current palette (it follows palette changes):
    /// - in menus, a syringe whose barrel fills with serum (accent) over anything clickable and presses in on click;
    /// - while playing, a crosshair: four brackets that breathe, kick outwards with every shot, and an accent centre dot.
    /// A small ring ripples out wherever the mouse is clicked. Hidden while a gamepad is in use and during the intro;
    /// the system cursor is hidden whenever this one is showing.
    /// </summary>
    public class PixelCursor : MonoBehaviour
    {
        // Shapes: '#' = main colour, '+' = inner colour, '.' = empty. A 1 px outline in the background colour is added
        // around every shape so the cursor reads on any background.
        // A syringe, needle tip on the hot spot: empty over the background, filled with serum (accent) over anything clickable.
        private static readonly string[] PointerMap =
        {
            "#............",
            ".#...........",
            "..#.#........",
            "...#+#.......",
            "..#+++#......",
            "...#+++#.....",
            "....#+++#.#..",
            ".....#+#.#...",
            "......#.#....",
            ".......#.#...",
            "......#...#.#",
            "...........#.",
            "..........#..",
        };
        private static readonly string[] BracketMap = { "###", "#..", "#.." }; // top-left; mirrored for the others

        private const float BreathSeconds = 0.9f;  // one in-out of the idle crosshair
        private const int Gap = 5;                 // centre to bracket corner, game pixels
        private const int KickPixels = 2;          // how far a shot throws the brackets out
        private const float KickSeconds = 0.12f;
        private const float PressSeconds = 0.08f;  // pointer pressed-in after a click
        private static readonly float RippleFrameSeconds = 0.035f;
        private static readonly int[] RippleRadii = { 2, 3, 4, 5, 6 };

        private static PixelCursor instance;

#if UNITY_EDITOR
        /// <summary>Editor only: pretend the mouse is here (screen pixels) and the window has focus. For checking the look.</summary>
        public static Vector2? EditorTestPosition;
#endif

        private RectTransform canvasRect, pointer, crosshair, ripple;
        private Image pointerInner;
        private RectTransform[] brackets = new RectTransform[4];
        private Image[] bracketMain = new Image[4];
        private Image[] rippleFrames;
        private float kickTime = -1f, pressTime = -1f, rippleTime = -1f;
        private readonly List<MenuNavigator> navigators = new List<MenuNavigator>();
        private PlayerController player;
        private bool sceneScanned;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            if (instance != null || Application.isBatchMode) return;
            var go = new GameObject("PixelCursor");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<PixelCursor>();
        }

        private void Awake()
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32000; // above every menu, the intro and fades
            canvas.pixelPerfect = true;
            gameObject.AddComponent<CanvasScaler>();
            gameObject.AddComponent<PixelCanvasScaler>(); // same whole-number scale as the game
            canvasRect = (RectTransform)transform;

            pointer = Holder("Pointer");
            Layered(pointer, PointerMap, 0, 0, out _, out pointerInner);

            crosshair = Holder("Crosshair");
            for (int i = 0; i < 4; i++)
            {
                brackets[i] = Holder("Bracket" + i, crosshair);
                Layered(brackets[i], Mirror(BracketMap, i % 2 == 1, i >= 2), 0, 0, out bracketMain[i], out _);
            }
            Layered(crosshair, new[] { "+" }, 0, 0, out _, out Image dot);
            UIFactory.Tint(dot.gameObject, PaletteRole.Accent, false);

            ripple = Holder("Ripple");
            rippleFrames = new Image[RippleRadii.Length];
            for (int i = 0; i < RippleRadii.Length; i++)
            {
                int r = RippleRadii[i];
                rippleFrames[i] = Layer(ripple, "Ring" + r, Ring(r), -r, -r, PaletteRole.Accent, false);
            }

            SceneManager.sceneLoaded += (s, m) => sceneScanned = false;
        }

        private void OnDestroy()
        {
            if (instance == this) instance = null;
            Cursor.visible = true;
        }

        private void LateUpdate()
        {
            if (!sceneScanned) Scan();

            Vector2 mouse = GameInput.MouseScreenPosition;
            bool focused = Application.isFocused;
#if UNITY_EDITOR
            if (EditorTestPosition is Vector2 test) { mouse = test; focused = true; }
#endif
            bool inWindow = mouse.x >= 0f && mouse.y >= 0f && mouse.x < Screen.width && mouse.y < Screen.height;
            bool show = !MenuNavigator.Blocked && !GameInput.UsingGamepad && inWindow && focused;
            Cursor.visible = !show && !MenuNavigator.Blocked && !GameInput.UsingGamepad;

            MenuNavigator menu = OpenMenu();
            bool aiming = show && menu == null && player != null && player.isActiveAndEnabled && Time.timeScale > 0f;
            pointer.gameObject.SetActive(show && !aiming);
            crosshair.gameObject.SetActive(aiming);

            float scale = Mathf.Max(1f, canvasRect.localScale.x);
            Vector2 at = new Vector2(Mathf.Round(mouse.x), Mathf.Round(mouse.y)) / scale;
            float now = Time.unscaledTime;

            if (show && GameInput.MouseLeftPressed)
            {
                pressTime = now;
                rippleTime = now;
                ripple.anchoredPosition = at;
            }

            if (pointer.gameObject.activeSelf)
            {
                MenuItem hover = menu != null && menu.Top != null ? menu.Top.ItemAt(mouse) : null;
                bool lit = hover != null && hover.Interactable;
                pointerInner.GetComponent<PaletteGraphic>().Set(lit ? PaletteRole.Accent : PaletteRole.Foreground, !lit);
                bool pressed = now - pressTime < PressSeconds || GameInput.MouseLeftHeld;
                pointer.anchoredPosition = at + (pressed ? new Vector2(1f, -1f) : Vector2.zero);
            }

            if (crosshair.gameObject.activeSelf)
            {
                crosshair.anchoredPosition = at;
                float k = Mathf.Clamp01(1f - (now - kickTime) / KickSeconds);
                int breath = Mathf.Repeat(now, BreathSeconds) < BreathSeconds * 0.5f ? 0 : 1;
                int gap = Gap + breath + Mathf.CeilToInt(k * KickPixels);
                for (int i = 0; i < 4; i++)
                {
                    bool right = i % 2 == 1, bottom = i >= 2;
                    // The bracket's corner pixel sits `gap` from the centre on both axes.
                    float x = right ? gap - 2 : -gap, y = bottom ? gap - 2 : -gap;
                    brackets[i].anchoredPosition = new Vector2(x, -y);
                    bracketMain[i].GetComponent<PaletteGraphic>().Set(k > 0.6f ? PaletteRole.Accent : PaletteRole.Foreground, false); // a flash, not a constant red under auto-shoot
                }
            }

            int frame = rippleTime < 0f ? -1 : Mathf.FloorToInt((now - rippleTime) / RippleFrameSeconds);
            bool rippling = show && frame >= 0 && frame < rippleFrames.Length;
            ripple.gameObject.SetActive(rippling);
            for (int i = 0; i < rippleFrames.Length; i++) rippleFrames[i].enabled = rippling && i == frame;
        }

        /// <summary>A shot was fired: throw the crosshair brackets out.</summary>
        private static void Kick()
        {
            if (instance != null) instance.kickTime = Time.unscaledTime;
        }

        private void OnEnable() => PlayerController.Fired += Kick;
        private void OnDisable() => PlayerController.Fired -= Kick;

        private void Scan()
        {
            sceneScanned = true;
            navigators.Clear();
            navigators.AddRange(FindObjectsByType<MenuNavigator>(FindObjectsSortMode.None));
            player = FindFirstObjectByType<PlayerController>();
        }

        private MenuNavigator OpenMenu()
        {
            for (int i = navigators.Count - 1; i >= 0; i--)
            {
                MenuNavigator n = navigators[i];
                if (n == null) { navigators.RemoveAt(i); continue; }
                if (n.isActiveAndEnabled && n.IsOpen) return n;
            }
            return null;
        }

        // ---------- building ----------

        private RectTransform Holder(string name, Transform parent = null)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent != null ? parent : transform, false);
            rt.anchorMin = rt.anchorMax = Vector2.zero; // bottom-left of the screen: positions are mouse / scale
            rt.pivot = Vector2.zero;
            rt.sizeDelta = Vector2.zero;
            return rt;
        }

        /// <summary>Outline (background), '#' (foreground) and '+' (dim foreground) layers of a map whose cell (0,0) sits at (ox, oy) game pixels from the holder, y down.</summary>
        private void Layered(RectTransform holder, string[] map, int ox, int oy, out Image main, out Image inner)
        {
            int w = map[0].Length + 2, h = map.Length + 2;
            bool[,] outline = new bool[w, h], hash = new bool[w, h], plus = new bool[w, h];
            for (int y = 0; y < map.Length; y++)
            for (int x = 0; x < map[y].Length; x++)
            {
                char c = map[y][x];
                if (c == '.') continue;
                (c == '+' ? plus : hash)[x + 1, y + 1] = true;
                for (int dy = -1; dy <= 1; dy++)
                for (int dx = -1; dx <= 1; dx++) outline[x + 1 + dx, y + 1 + dy] = true;
            }
            Layer(holder, "Outline", outline, ox - 1, oy - 1, PaletteRole.Background, false);
            main = Layer(holder, "Main", hash, ox - 1, oy - 1, PaletteRole.Foreground, false);
            inner = Layer(holder, "Inner", plus, ox - 1, oy - 1, PaletteRole.Foreground, true);
        }

        private static Image Layer(RectTransform holder, string name, bool[,] on, int ox, int oy, PaletteRole role, bool dim)
        {
            int w = on.GetLength(0), h = on.GetLength(1);
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                px[(h - 1 - y) * w + x] = on[x, y] ? new Color32(255, 255, 255, 255) : new Color32(255, 255, 255, 0);
            tex.SetPixels32(px);
            tex.Apply(false, true);

            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(holder, false);
            rt.anchorMin = rt.anchorMax = Vector2.zero;
            rt.pivot = new Vector2(0f, 1f);
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = new Vector2(ox, -oy);
            var img = go.AddComponent<Image>();
            img.sprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0f, 1f), 1f);
            img.raycastTarget = false;
            UIFactory.Tint(go, role, dim);
            return img;
        }

        private static string[] Mirror(string[] map, bool flipX, bool flipY)
        {
            var result = new string[map.Length];
            for (int y = 0; y < map.Length; y++)
            {
                char[] row = map[flipY ? map.Length - 1 - y : y].ToCharArray();
                if (flipX) System.Array.Reverse(row);
                result[y] = new string(row);
            }
            return result;
        }

        /// <summary>A 1 px circle of radius r (midpoint circle), (2r+1) square.</summary>
        private static bool[,] Ring(int r)
        {
            int s = 2 * r + 1;
            var on = new bool[s, s];
            int x = r, y = 0, err = 1 - r;
            while (x >= y)
            {
                foreach (var (a, b) in new[] { (x, y), (y, x), (-y, x), (-x, y), (-x, -y), (-y, -x), (y, -x), (x, -y) })
                    on[r + a, r + b] = true;
                y++;
                if (err < 0) err += 2 * y + 1;
                else { x--; err += 2 * (y - x) + 1; }
            }
            return on;
        }
    }
}
