using System;
using System.Collections.Generic;
using AntiBacteria.Core;
using AntiBacteria.Rendering;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AntiBacteria.UI.Menu
{
    /// <summary>
    /// Three perk cards side by side on the level-up screen: rank pips, icon, name, rarity, what it does.
    /// The frame tells the rarity at a glance: Common a plain border, Rare a double border, Epic accent corner
    /// blocks too, Legendary an accent inner border with glints running round it. Buddy perks carry a small
    /// buddy badge. Left / right moves between them, Confirm takes the highlighted one; the mouse hovers and clicks.
    /// </summary>
    public class PerkCards : MenuItem
    {
        public const float CardW = 98f, CardH = 110f, Gap = 5f;
        private const float GlintW = 5f;

        private static readonly string[] BuddyBadge = { ".###.", "#R.R#", "#...#", "#.R.#", ".###." };

        private class Card
        {
            public RectTransform rt;
            public Image icon, badge, glintTop, glintBottom;
            public TextMeshProUGUI name, rarity, desc;
            public PaletteGraphic[] frame, inner;
            public GameObject innerRoot, cornersRoot;
            public PaletteGraphic[] corners;
            public Image[] pips;
            public PerkRarity shown;
        }

        private readonly List<Card> cards = new List<Card>();
        private readonly List<PerkId> offer = new List<PerkId>();
        private int index;
        private Action<PerkId> onPick;

        public static PerkCards Create(UISkin skin, Transform parent, float y, int count, Action<PerkId> onPick)
        {
            float total = count * CardW + (count - 1) * Gap;
            RectTransform row = UIFactory.Rect(parent, "PerkCards", 160f - total / 2f, y, total, CardH, new Vector2(0f, 1f));
            var p = row.gameObject.AddComponent<PerkCards>();
            p.Hit = row;
            p.onPick = onPick;
            for (int i = 0; i < count; i++)
            {
                float x = i * (CardW + Gap);
                RectTransform rt = UIFactory.Rect(row, "Card" + i, x, 0f, CardW, CardH, new Vector2(0f, 1f));
                UIFactory.Image(rt, "Fill", skin.pixel, 0f, 0f, CardW, CardH, PaletteRole.Background);
                var c = new Card { rt = rt };

                // Outer border (selection colour).
                c.frame = FrameIn(skin, rt, "Outer", 0f, 0f, CardW - 1f, CardH - 1f);
                // Inner border, 2 px in (Rare and up).
                RectTransform inner = UIFactory.Rect(rt, "Inner", 0f, 0f, CardW, CardH, new Vector2(0f, 1f));
                c.innerRoot = inner.gameObject;
                c.inner = FrameIn(skin, inner, "Inner", 2f, 2f, CardW - 3f, CardH - 3f);
                c.glintTop = UIFactory.Image(inner, "GlintTop", skin.pixel, 4f, 2f, GlintW, 1f, PaletteRole.Foreground);
                c.glintBottom = UIFactory.Image(inner, "GlintBottom", skin.pixel, 4f, CardH - 3f, GlintW, 1f, PaletteRole.Foreground);
                // Accent blocks on the four outer corners (Epic and up).
                RectTransform corners = UIFactory.Rect(rt, "Corners", 0f, 0f, CardW, CardH, new Vector2(0f, 1f));
                c.cornersRoot = corners.gameObject;
                c.corners = new[]
                {
                    Block(skin, corners, 0f, 0f), Block(skin, corners, CardW - 3f, 0f),
                    Block(skin, corners, 0f, CardH - 3f), Block(skin, corners, CardW - 3f, CardH - 3f),
                };

                // Rank pips, top left: owned = white, the one this card gives = accent, the rest dim.
                c.pips = new Image[4];
                for (int k = 0; k < 4; k++)
                    c.pips[k] = UIFactory.Image(rt, "Pip" + k, skin.pixel, 5f + k * 4f, 5f, 3f, 3f, PaletteRole.Foreground);
                // Buddy badge, top right.
                RectTransform badgeRt = UIFactory.Rect(rt, "Badge", CardW - 10f, 5f, 5f, 5f, new Vector2(0f, 1f));
                c.badge = badgeRt.gameObject.AddComponent<Image>();
                c.badge.sprite = PixelShapes.FromRows("buddy_badge", BuddyBadge);
                c.badge.material = skin.paletteMaterial;
                c.badge.raycastTarget = false;

                // The 9 px icon at 2x, palette-indexed (accent pixels stay accent).
                RectTransform iconRt = UIFactory.Rect(rt, "Icon", CardW / 2f - 9f, 6f, 18f, 18f, new Vector2(0f, 1f));
                c.icon = iconRt.gameObject.AddComponent<Image>();
                c.icon.material = skin.paletteMaterial;
                c.icon.raycastTarget = false;
                c.name = UIFactory.Text(skin, rt, "Name", CardW / 2f, 28f, HorizontalAlignmentOptions.Center);
                c.rarity = UIFactory.Text(skin, rt, "Rarity", CardW / 2f, 40f, HorizontalAlignmentOptions.Center, 1, PaletteRole.Foreground, true);
                c.desc = UIFactory.Text(skin, rt, "Desc", CardW / 2f, 53f, HorizontalAlignmentOptions.Center);
                c.desc.rectTransform.sizeDelta = new Vector2(CardW - 8f, 56f); // up to 4 lines, inside the inner border
                c.desc.textWrappingMode = TextWrappingModes.Normal;
                // TMP line spacing is in hundredths of the font size: -18.75 x 16 px = -3 px, so 13 px lines
                // (m6x11 glyphs are up to 11 px tall with accents and descenders, so 2 px stay clear between lines).
                c.desc.lineSpacing = -18.75f;
                c.desc.overflowMode = TextOverflowModes.Truncate; // never spill out of the card
                p.cards.Add(c);
            }
            p.Refresh();
            return p;
        }

        /// <summary>A 1 px border whose pieces can be recoloured together.</summary>
        private static PaletteGraphic[] FrameIn(UISkin skin, RectTransform parent, string name, float x0, float y0, float x1, float y1)
        {
            var before = new HashSet<Transform>();
            foreach (Transform t in parent) before.Add(t);
            UIFactory.Frame(skin, parent, x0, y0, x1, y1, PaletteRole.Foreground, true);
            var list = new List<PaletteGraphic>();
            foreach (Transform t in parent)
            {
                if (before.Contains(t)) continue;
                t.name = name + t.name;
                PaletteGraphic g = t.GetComponent<PaletteGraphic>();
                if (g != null) list.Add(g);
            }
            return list.ToArray();
        }

        private static PaletteGraphic Block(UISkin skin, RectTransform parent, float x, float y) =>
            UIFactory.Image(parent, "Corner", skin.pixel, x, y, 3f, 3f, PaletteRole.Accent).GetComponent<PaletteGraphic>();

        /// <summary>Show a new offer (perks that can still rank up).</summary>
        public void Show(List<PerkId> perks)
        {
            offer.Clear();
            offer.AddRange(perks);
            index = 0;
            Refresh();
        }

        public override bool Step(int direction)
        {
            int n = Mathf.Clamp(index + direction, 0, Mathf.Max(0, offer.Count - 1));
            if (n == index) return true;
            index = n;
            Refresh();
            return true;
        }

        public override void Submit()
        {
            if (index < offer.Count) onPick?.Invoke(offer[index]);
        }

        public override void Hover(Vector2 screenPosition)
        {
            int i = CardAt(screenPosition);
            if (i >= 0 && i != index) { index = i; Refresh(); }
        }

        public override void Click(Vector2 screenPosition)
        {
            int i = CardAt(screenPosition);
            if (i < 0) return;
            index = i;
            Submit();
        }

        private int CardAt(Vector2 screenPosition)
        {
            for (int i = 0; i < cards.Count && i < offer.Count; i++)
                if (RectTransformUtility.RectangleContainsScreenPoint(cards[i].rt, screenPosition, null)) return i;
            return -1;
        }

        public override void Refresh()
        {
            for (int i = 0; i < cards.Count; i++)
            {
                Card c = cards[i];
                bool has = i < offer.Count;
                c.rt.gameObject.SetActive(has);
                if (!has) continue;
                PerkId id = offer[i];
                PerkInfo info = Perks.Get(id);
                bool on = Selected && i == index;
                int owned = RoguelikeRun.Rank(id);
                c.shown = info.rarity;
                c.icon.sprite = PerkIcons.Get(id);
                c.name.text = Perks.Name(id);
                c.rarity.text = Perks.RarityName(info.rarity);
                c.desc.text = Perks.Description(id);
                c.name.GetComponent<PaletteGraphic>().Set(on ? PaletteRole.Accent : PaletteRole.Foreground, false);
                c.desc.GetComponent<PaletteGraphic>().Set(PaletteRole.Foreground, !on);
                // Rarity word: Common dim, Rare white, Epic and Legendary accent.
                c.rarity.GetComponent<PaletteGraphic>().Set(info.rarity >= PerkRarity.Epic ? PaletteRole.Accent : PaletteRole.Foreground,
                    info.rarity == PerkRarity.Common);
                foreach (PaletteGraphic f in c.frame) f.Set(on ? PaletteRole.Accent : PaletteRole.Foreground, !on);

                c.innerRoot.SetActive(info.rarity >= PerkRarity.Rare);
                bool legendary = info.rarity == PerkRarity.Legendary;
                foreach (PaletteGraphic f in c.inner) f.Set(legendary ? PaletteRole.Accent : PaletteRole.Foreground, !legendary && !on);
                c.glintTop.gameObject.SetActive(legendary);
                c.glintBottom.gameObject.SetActive(legendary);
                c.cornersRoot.SetActive(info.rarity >= PerkRarity.Epic);

                for (int k = 0; k < c.pips.Length; k++)
                {
                    bool show = k < info.maxRank;
                    c.pips[k].gameObject.SetActive(show);
                    if (!show) continue;
                    PaletteGraphic g = c.pips[k].GetComponent<PaletteGraphic>();
                    if (k < owned) g.Set(PaletteRole.Foreground, false);
                    else if (k == owned) g.Set(PaletteRole.Accent, false);
                    else g.Set(PaletteRole.Foreground, true);
                }
                c.badge.gameObject.SetActive(info.IsBuddyPerk);
            }
        }

        private void Update()
        {
            // Legendary glints run round the inner border (unscaled: the game is paused on this screen).
            float span = CardW - 8f - GlintW;
            float t = Time.unscaledTime * 45f % (span * 2f);
            float x = 4f + (t < span ? t : span * 2f - t);
            foreach (Card c in cards)
            {
                if (c.shown != PerkRarity.Legendary || !c.glintTop.gameObject.activeInHierarchy) continue;
                c.glintTop.rectTransform.anchoredPosition = new Vector2(Mathf.Round(x), c.glintTop.rectTransform.anchoredPosition.y);
                c.glintBottom.rectTransform.anchoredPosition = new Vector2(Mathf.Round(4f + span - (x - 4f)), c.glintBottom.rectTransform.anchoredPosition.y);
            }
        }
    }

    /// <summary>Perk icons as sprites, made once from the 9x9 rows in the perk table.</summary>
    public static class PerkIcons
    {
        public static Sprite Get(PerkId id) => PixelShapes.FromRows("perk_" + id, Perks.Get(id).icon);
    }
}
