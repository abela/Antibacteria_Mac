using System.Collections.Generic;
using AntiBacteria.Core;
using AntiBacteria.Gameplay.Roguelike;
using AntiBacteria.Rendering;
using UnityEngine;
using UnityEngine.UI;

namespace AntiBacteria.UI.Menu
{
    /// <summary>
    /// Roguelike HUD on the menu canvas: "LV 3" and the XP bar at the bottom centre, the perks taken (icon
    /// + rank pips) along the bottom left, and "ROOM CLEAR!" / "Go through the gate" when a room is done.
    /// Pinned to the screen edges like the HUD, not to the 320x180 menu layout.
    /// </summary>
    public class RoguelikeHud : MonoBehaviour
    {
        private const float BarW = 80f;

        private UISkin skin;
        private RectTransform top, perksRow;
        private LiveText levelLabel, clearTitle, clearHint;
        private Image barFill;
        private readonly List<GameObject> perkIcons = new List<GameObject>();
        private float clearLeft, flashLeft;
        private PaletteGraphic barTint;
        private const int PerIconRow = 16;

        public static RoguelikeHud Create(UISkin skin, RectTransform root)
        {
            var go = new GameObject("RoguelikeHud", typeof(RectTransform));
            go.transform.SetParent(root, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
            go.transform.SetAsFirstSibling(); // under every menu
            var h = go.AddComponent<RoguelikeHud>();
            h.skin = skin;

            // LV + XP bar, bottom centre of the screen (the top is the slow-motion meter and the room's exit gate),
            // just above where the menus' button prompts appear.
            h.top = UIFactory.Rect(rt, "XpBar", 160f, 0f, BarW + 40f, 16f, new Vector2(0.5f, 1f));
            h.levelLabel = LiveText.Create(skin, h.top, "Level", 0f, 0f, () => Loc.Get(WordIDs.Lv) + " " + RoguelikeRun.Level,
                TMPro.HorizontalAlignmentOptions.Left, role: PaletteRole.Accent);
            UIFactory.Frame(skin, h.top, 38f, 4f, 38f + BarW + 1f, 8f, PaletteRole.Foreground, false);
            h.barFill = UIFactory.Image(h.top, "Fill", skin.pixel, 39f, 5f, 1f, 3f, PaletteRole.Accent);
            PinToScreenCorner.Pin(h.top, new Vector2(0.5f, 0f), new Vector2(0f, 14f));
            h.barTint = h.barFill.GetComponent<PaletteGraphic>();

            // Perks taken, bottom left.
            h.perksRow = UIFactory.Rect(rt, "Perks", 0f, 0f, 200f, 14f, new Vector2(0f, 0f));
            PinToScreenCorner.Pin(h.perksRow, Vector2.zero, new Vector2(4f, 3f));

            h.clearTitle = LiveText.Create(skin, rt, "RoomClear", 160f, 52f, () => Loc.Get(WordIDs.RoomClear).ToUpperInvariant(),
                scale: 2, role: PaletteRole.Accent, maxWidth: 300f);
            h.clearHint = LiveText.Create(skin, rt, "GateHint", 160f, 80f, () => Loc.Get(WordIDs.GoThroughGate));
            h.clearTitle.gameObject.SetActive(false);
            h.clearHint.gameObject.SetActive(false);

            RoguelikeRun.Changed += h.Redraw;
            RoguelikeRoom.RoomCleared += h.OnRoomCleared;
            XpOrb.PickedUp += h.OnOrb;
            h.Redraw();
            return h;
        }

        private void OnDestroy()
        {
            RoguelikeRun.Changed -= Redraw;
            RoguelikeRoom.RoomCleared -= OnRoomCleared;
            XpOrb.PickedUp -= OnOrb;
        }

        /// <summary>An XP orb landed: the bar flashes white for a moment.</summary>
        private void OnOrb() => flashLeft = 0.12f;

        private void OnRoomCleared()
        {
            clearLeft = 2.5f;
            clearTitle.gameObject.SetActive(true);
            clearHint.gameObject.SetActive(true);
            clearTitle.Refresh();
            clearHint.Refresh();
        }

        private void Update()
        {
            if (barTint != null)
            {
                if (flashLeft > 0f) flashLeft -= Time.unscaledDeltaTime;
                barTint.Set(flashLeft > 0f ? PaletteRole.Foreground : PaletteRole.Accent, false);
            }
            if (clearLeft <= 0f) return;
            clearLeft -= Time.unscaledDeltaTime;
            bool show = clearLeft > 0f;
            clearTitle.gameObject.SetActive(show);
            clearHint.gameObject.SetActive(show);
        }

        public void Redraw()
        {
            levelLabel.Refresh();
            float w = Mathf.Round(BarW * RoguelikeRun.Xp01);
            barFill.enabled = w >= 1f;
            barFill.rectTransform.sizeDelta = new Vector2(Mathf.Max(1f, w), 3f);

            foreach (GameObject g in perkIcons) Destroy(g);
            perkIcons.Clear();
            int n = 0;
            foreach (PerkInfo p in Perks.All)
            {
                int r = RoguelikeRun.Rank(p.id);
                if (r == 0) continue;
                // 16 a row; later rows stack upward so the first row stays on the bottom edge.
                float x = (n % PerIconRow) * 12f, y = -(n / PerIconRow) * 15f;
                n++;
                var holder = UIFactory.Rect(perksRow, "Perk" + p.id, x, y, 9f, 13f, new Vector2(0f, 1f));
                RectTransform iconRt = UIFactory.Rect(holder, "Icon", 0f, 0f, 9f, 9f, new Vector2(0f, 1f));
                var icon = iconRt.gameObject.AddComponent<Image>();
                icon.sprite = PerkIcons.Get(p.id);
                icon.material = skin.paletteMaterial;
                icon.raycastTarget = false;
                for (int i = 0; i < r; i++) UIFactory.Image(holder, "Pip", skin.pixel, i * 2f, 11f, 1f, 1f, PaletteRole.Accent);
                perkIcons.Add(holder.gameObject);
            }
        }
    }
}
