using AntiBacteria.Core;
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

        private void OnEnable() => GameSession.Changed += RefreshSession;
        private void OnDisable() => GameSession.Changed -= RefreshSession;

        private void Start()
        {
            RefreshSession();
            if (levelText != null && runner != null && runner.Level != null) levelText.SetText(runner.Level.Label);
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
