using AntiBacteria.Core;
using AntiBacteria.Gameplay.Bonuses;
using AntiBacteria.Gameplay.Level;
using AntiBacteria.Gameplay.Player;
using UnityEngine;
using UnityEngine.UI;

namespace AntiBacteria.UI
{
    /// <summary>Gameplay HUD (layout: Docs/ArtDirection/StyleGuide.md, section 4).</summary>
    public class HudController : MonoBehaviour
    {
        [SerializeField] private Image[] hearts = new Image[0];
        [SerializeField] private Sprite heartFull;
        [SerializeField] private Sprite heartEmpty;
        [SerializeField] private RectTransform meterFill;
        [SerializeField] private float meterFillMaxPixels = 57f;
        [SerializeField] private PixelText vitaminsText;
        [SerializeField] private PixelText timerText;
        [SerializeField] private PixelText levelText;
        [SerializeField] private SlowMotionAbility slowMotion;
        [SerializeField] private CampaignLevelRunner runner;

        private PlayerController player;

        // Capture the Flag: flags taken, with a small flag drawn left of the number, where the clock is elsewhere.
        private bool showFlags;
        private RectTransform flagIcon;

        // Active timed bonuses: an icon and a draining 1 px bar per slot, under the hearts.
        private const float BuffIconPixels = 7f;
        private const float BuffBlinkPeriod = 0.15f;
        private Image[] buffIcons, buffBars;

        // Armor upgrade: a small shield right of the hearts while this level's blocked hit is still unused.
        private RectTransform shieldIcon;
        private int shownArmor = -1;

        private void OnEnable() => GameSession.Changed += RefreshSession;
        private void OnDisable() => GameSession.Changed -= RefreshSession;

        private void Start()
        {
            BuildBuffSlots();
            RefreshSession();
            if (levelText != null && RoguelikeRun.Active) levelText.SetText(Loc.Get(WordIDs.Room) + " " + RoguelikeRun.Room);
            else if (levelText != null && runner != null && runner.IsEndless) levelText.SetText(Loc.Get((WordIDs)System.Enum.Parse(typeof(WordIDs), GameSession.Mode.ToString())));
            else if (levelText != null && runner != null && runner.Level != null) levelText.SetText(runner.Level.Label);
            showFlags = runner != null && runner.IsEndless && GameSession.Mode == GameMode.CaptureFlag;
            if (showFlags) BuildFlagIcon();
            else if (runner != null && !runner.IsBossLevel && !runner.ShowsTimer && transform.Find("Timer") is Transform clock)
                clock.gameObject.SetActive(false); // Rush and the roguelike: no clock
            // Boss levels: no clock and no slow motion; the boss bar (GameplayMenuController) takes the meter's place.
            if (runner != null && runner.IsBossLevel)
                foreach (string n in new[] { "MeterFrame", "MeterFill", "Timer" })
                {
                    Transform t = transform.Find(n);
                    if (t != null) t.gameObject.SetActive(false);
                }
        }

        private void Update()
        {
            if (player == null)
            {
                player = PlayerController.Current;
                if (player != null) { player.LivesChanged += RefreshLives; RefreshLives(); }
            }
            if (slowMotion != null && meterFill != null)
            {
                float w = Mathf.Round(meterFillMaxPixels * Mathf.Clamp01(slowMotion.Meter01));
                meterFill.sizeDelta = new Vector2(w, meterFill.sizeDelta.y);
            }
            if (timerText != null && runner != null)
            {
                if (showFlags)
                {
                    string n = (runner.Mode != null ? runner.Mode.Completed : 0).ToString();
                    timerText.SetText(n);
                    // 3 px glyphs and 1 px gaps, centred: the flag goes 2 px left of the first digit.
                    float half = Mathf.Floor((n.Length * 4 - 1) / 2f);
                    if (flagIcon != null) flagIcon.anchoredPosition = new Vector2(timerText.GetComponent<RectTransform>().anchoredPosition.x - half - 7f,
                        timerText.GetComponent<RectTransform>().anchoredPosition.y + 1f);
                }
                else timerText.SetText(Mathf.CeilToInt(runner.TimeLeft).ToString());
            }
            UpdateBuffSlots();
            UpdateShield();
        }

        private static readonly string[] ShieldRows = { "#######", "#.....#", "#.....#", "#.....#", ".#...#.", ".#...#.", "..#.#..", "...#..." };

        /// <summary>A 7x8 px shield (white outline, accent inside) built from 1 px images, like the flag.</summary>
        private void BuildShieldIcon()
        {
            Sprite pixel = BonusSettings.Current.pixel;
            var go = new GameObject("ArmorIcon", typeof(RectTransform));
            go.layer = gameObject.layer;
            go.transform.SetParent(transform, false);
            shieldIcon = (RectTransform)go.transform;
            shieldIcon.anchorMin = shieldIcon.anchorMax = shieldIcon.pivot = new Vector2(0f, 1f);
            shieldIcon.sizeDelta = new Vector2(7f, 8f);
            for (int y = 0; y < ShieldRows.Length; y++)
            {
                string row = ShieldRows[y];
                int first = row.IndexOf('#'), last = row.LastIndexOf('#');
                for (int x = first; x <= last; x++)
                {
                    bool edge = row[x] == '#';
                    // One image per outline pixel; the inside of the row as one accent run.
                    if (!edge) { int e = x; while (e + 1 < last && row[e + 1] != '#') e++; Pixel(x, y, e - x + 1, Rendering.PaletteRole.Accent); x = e; }
                    else Pixel(x, y, 1, Rendering.PaletteRole.Foreground);
                }
            }

            void Pixel(int x, int y, int w, Rendering.PaletteRole role)
            {
                var p = new GameObject("P", typeof(RectTransform), typeof(Image));
                p.layer = gameObject.layer;
                p.transform.SetParent(shieldIcon, false);
                var img = p.GetComponent<Image>();
                img.sprite = pixel;
                img.raycastTarget = false;
                RectTransform rt = img.rectTransform;
                rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
                rt.anchoredPosition = new Vector2(x, -y);
                rt.sizeDelta = new Vector2(w, 1f);
                p.AddComponent<Rendering.PaletteGraphic>().Set(role, false);
            }
        }

        private void UpdateShield()
        {
            int armor = player != null && Upgrades.On ? Upgrades.ArmorLeft : 0;
            if (armor == shownArmor) return;
            shownArmor = armor;
            if (armor > 0 && shieldIcon == null) BuildShieldIcon();
            if (shieldIcon == null) return;
            shieldIcon.gameObject.SetActive(armor > 0);
            PlaceShield();
        }

        /// <summary>One heart step right of the last heart in use.</summary>
        private void PlaceShield()
        {
            if (shieldIcon == null || player == null || hearts.Length == 0) return;
            int last = Mathf.Clamp(player.MaxLives - 1, 0, hearts.Length - 1);
            if (hearts[last] == null) return;
            RectTransform h = hearts[last].rectTransform;
            float step = hearts.Length > 1 && hearts[1] != null && hearts[0] != null
                ? hearts[1].rectTransform.anchoredPosition.x - hearts[0].rectTransform.anchoredPosition.x : h.rect.width + 2f;
            // Hearts may be anchored differently; work in the HUD's own space through world positions.
            Vector3[] c = new Vector3[4];
            h.GetWorldCorners(c); // 1 = top-left
            Vector3 topLeft = ((RectTransform)transform).InverseTransformPoint(c[1]);
            Rect r = ((RectTransform)transform).rect;
            shieldIcon.anchoredPosition = new Vector2(Mathf.Round(topLeft.x - r.xMin + step), Mathf.Round(topLeft.y - r.yMax));
        }

        /// <summary>A 5x7 px flag (white pole, accent cloth) for the Capture the Flag counter, on a background box.</summary>
        private void BuildFlagIcon()
        {
            Sprite pixel = BonusSettings.Current.pixel;
            var go = new GameObject("FlagIcon", typeof(RectTransform));
            go.layer = gameObject.layer;
            go.transform.SetParent(transform, false);
            flagIcon = (RectTransform)go.transform;
            flagIcon.anchorMin = flagIcon.anchorMax = new Vector2(0.5f, 1f);
            flagIcon.pivot = new Vector2(0f, 1f);
            flagIcon.sizeDelta = new Vector2(5f, 7f);
            Part("Backing", 0f, 0f, 6f, 8f, Rendering.PaletteRole.Background);
            Part("Pole", 1f, -1f, 1f, 6f, Rendering.PaletteRole.Foreground);
            Part("Cloth", 2f, -1f, 3f, 3f, Rendering.PaletteRole.Accent);

            void Part(string n, float x, float y, float w, float h, Rendering.PaletteRole role)
            {
                var p = new GameObject(n, typeof(RectTransform), typeof(Image));
                p.layer = gameObject.layer;
                p.transform.SetParent(flagIcon, false);
                var img = p.GetComponent<Image>();
                img.sprite = pixel;
                img.raycastTarget = false;
                RectTransform rt = img.rectTransform;
                rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
                rt.anchoredPosition = new Vector2(x, y);
                rt.sizeDelta = new Vector2(w, h);
                p.AddComponent<Rendering.PaletteGraphic>().Set(role, false);
            }
        }

        /// <summary>More hearts when the maximum grows (roguelike Vitality perk), same spacing as the scene's.</summary>
        private void GrowHearts(int count)
        {
            if (hearts.Length >= count || hearts.Length < 2 || hearts[0] == null || hearts[1] == null) return;
            float step = hearts[1].rectTransform.anchoredPosition.x - hearts[0].rectTransform.anchoredPosition.x;
            var list = new System.Collections.Generic.List<Image>(hearts);
            while (list.Count < count)
            {
                Image last = list[list.Count - 1];
                Image h = Instantiate(last, last.transform.parent);
                h.rectTransform.anchoredPosition = last.rectTransform.anchoredPosition + new Vector2(step, 0f);
                list.Add(h);
            }
            hearts = list.ToArray();
        }

        private void BuildBuffSlots()
        {
            Material mat = hearts.Length > 0 && hearts[0] != null ? hearts[0].material : null;
            Sprite pixel = BonusSettings.Current.pixel;
            buffIcons = new Image[PlayerBuffs.SlotCount];
            buffBars = new Image[PlayerBuffs.SlotCount];
            for (int i = 0; i < PlayerBuffs.SlotCount; i++)
            {
                float x = 8f + i * (BuffIconPixels + 4f);
                buffIcons[i] = NewImage("BuffIcon" + i, null, mat, new Vector2(x, -17f), new Vector2(BuffIconPixels, BuffIconPixels));
                buffBars[i] = NewImage("BuffBar" + i, pixel, mat, new Vector2(x, -26f), new Vector2(BuffIconPixels, 1f));
            }
        }

        private Image NewImage(string name, Sprite sprite, Material mat, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.layer = gameObject.layer;
            go.transform.SetParent(transform, false);
            var img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.material = mat;
            img.raycastTarget = false;
            img.enabled = false;
            RectTransform rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return img;
        }

        private void UpdateBuffSlots()
        {
            if (buffIcons == null) return;
            PlayerBuffs buffs = player != null ? player.Buffs : null;
            BonusSettings s = BonusSettings.Current;
            int n = 0;
            if (buffs != null)
            {
                for (int slot = 0; slot < PlayerBuffs.SlotCount; slot++)
                {
                    float left = buffs.TimeLeft((PlayerBuffs.Slot)slot);
                    if (left <= 0f) continue;
                    BonusInfo info = s.Info(buffs.IconFor((PlayerBuffs.Slot)slot));
                    buffIcons[n].sprite = info != null ? info.icon : null;
                    bool warn = left < s.warnSeconds;
                    buffIcons[n].enabled = !warn || Mathf.FloorToInt(left / BuffBlinkPeriod) % 2 == 0;
                    buffBars[n].enabled = true;
                    float w = Mathf.Max(1f, Mathf.Ceil(BuffIconPixels * Mathf.Clamp01(left / (s.durationSeconds * RunModifiers.BonusDurationMultiplier))));
                    buffBars[n].rectTransform.sizeDelta = new Vector2(w, 1f);
                    n++;
                }
            }
            for (; n < buffIcons.Length; n++) buffIcons[n].enabled = buffBars[n].enabled = false;
        }

        private void RefreshLives()
        {
            if (player == null) return;
            GrowHearts(player.MaxLives);
            for (int i = 0; i < hearts.Length; i++)
            {
                if (hearts[i] == null) continue;
                hearts[i].enabled = i < player.MaxLives;
                hearts[i].sprite = i < player.Lives ? heartFull : heartEmpty;
            }
            PlaceShield();
        }

        private void RefreshSession()
        {
            if (vitaminsText != null) vitaminsText.SetText("x" + GameSession.Vitamins);
        }

        private void OnDestroy()
        {
            if (player != null) player.LivesChanged -= RefreshLives;
        }
    }
}
