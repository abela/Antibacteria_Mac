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

        // Active timed bonuses: an icon and a draining 1 px bar per slot, under the hearts.
        private const float BuffIconPixels = 7f;
        private const float BuffBlinkPeriod = 0.15f;
        private Image[] buffIcons, buffBars;

        private void OnEnable() => GameSession.Changed += RefreshSession;
        private void OnDisable() => GameSession.Changed -= RefreshSession;

        private void Start()
        {
            BuildBuffSlots();
            RefreshSession();
            if (levelText != null && runner != null && runner.IsEndless) levelText.SetText(Loc.Get((WordIDs)System.Enum.Parse(typeof(WordIDs), GameSession.Mode.ToString())));
            else if (levelText != null && runner != null && runner.Level != null) levelText.SetText(runner.Level.Label);
            if (runner != null && !runner.IsBossLevel && !runner.ShowsTimer && transform.Find("Timer") is Transform clock)
                clock.gameObject.SetActive(false); // Capture the Flag and Rush: no clock
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
            if (timerText != null && runner != null) timerText.SetText(Mathf.CeilToInt(runner.TimeLeft).ToString());
            UpdateBuffSlots();
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
                    float w = Mathf.Max(1f, Mathf.Ceil(BuffIconPixels * left / s.durationSeconds));
                    buffBars[n].rectTransform.sizeDelta = new Vector2(w, 1f);
                    n++;
                }
            }
            for (; n < buffIcons.Length; n++) buffIcons[n].enabled = buffBars[n].enabled = false;
        }

        private void RefreshLives()
        {
            if (player == null) return;
            for (int i = 0; i < hearts.Length; i++)
            {
                if (hearts[i] == null) continue;
                hearts[i].enabled = i < player.MaxLives;
                hearts[i].sprite = i < player.Lives ? heartFull : heartEmpty;
            }
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
