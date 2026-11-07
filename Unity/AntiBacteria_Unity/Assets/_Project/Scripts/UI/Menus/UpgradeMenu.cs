using AntiBacteria.Audio;
using System.Globalization;
using AntiBacteria.Core;
using AntiBacteria.Rendering;
using AntiBacteria.UI.Menu;
using UnityEngine;

namespace AntiBacteria.UI.Menus
{
    /// <summary>
    /// Upgrade Character: permanent upgrades with levels, bought with vitamins (ShopSettings). Each row shows a box per
    /// level and the next level's price; the line under the list explains the selected upgrade. Upgrades work in the
    /// campaign and the arcade modes, not in the roguelike.
    /// </summary>
    public static class UpgradeMenu
    {
        private const float RowX = 36f, RowWidth = 248f, FirstRowY = 38f, RowStep = 13f;

        public static MenuScreen Build(UISkin skin, Transform parent, MenuNavigator nav)
        {
            MenuScreen s = MenuScreen.Create(parent, "Upgrade");
            Transform t = s.transform;
            LiveText.Create(skin, t, "Title", 160f, 8f, () => Loc.Get(WordIDs.UpgradeCharacter).ToUpperInvariant(), role: PaletteRole.Accent);
            LiveText.Create(skin, t, "Wallet", 160f, 22f,
                () => Loc.Get(WordIDs.Vitamins) + ": " + Format(Progress.Vitamins));

            UpgradeDef[] defs = ShopSettings.Current.upgrades;
            float listEnd = FirstRowY + defs.Length * RowStep;
            UpgradeDef shown = defs.Length > 0 ? defs[0] : null;
            LiveText desc = LiveText.Create(skin, t, "Description", 160f, listEnd + 4f,
                () => shown != null ? Loc.Get(shown.descId) : "", dim: true, maxWidth: 300f);
            LiveText message = LiveText.Create(skin, t, "Message", 160f, listEnd + 17f,
                () => Loc.Get(WordIDs.NoVitamins), role: PaletteRole.Accent);
            HideAfter messageTimer = message.gameObject.AddComponent<HideAfter>();
            message.gameObject.SetActive(false);

            float y = FirstRowY;
            foreach (UpgradeDef def in defs)
            {
                if (def == null) continue;
                UpgradeDef d = def;
                UpgradeRow row = s.Add(UpgradeRow.Create(skin, t, d.upgrade.ToString(), RowX, y, RowWidth, d.MaxLevel,
                    () => Loc.Get(d.nameId),
                    () => Progress.UpgradeLevel(d.upgrade),
                    () => Progress.UpgradeLevel(d.upgrade) >= d.MaxLevel ? Loc.Get(WordIDs.Max) : Format(NextPrice(d)),
                    () => Progress.UpgradeLevel(d.upgrade) >= d.MaxLevel || Progress.Vitamins < NextPrice(d),
                    () =>
                    {
                        if (Progress.UpgradeLevel(d.upgrade) >= d.MaxLevel) return;
                        if (Progress.TryBuyLevel(d.upgrade)) Sound.Play(Sfx.UiConfirm);
                        else { messageTimer.Show(2f); Sound.Play(Sfx.UiError); }
                        s.RefreshAll();
                    }));
                row.OnSelected = () => { if (shown != d) { shown = d; desc.Refresh(); } };
                y += RowStep;
            }

            s.Add(MenuButton.Create(skin, t, "Back", 160f, 160f, () => Loc.Get(WordIDs.Back), nav.Back));
            return s;
        }

        private static int NextPrice(UpgradeDef d)
        {
            int l = Progress.UpgradeLevel(d.upgrade);
            return d.prices != null && l < d.prices.Length ? d.prices[l] : 0;
        }

        private static string Format(int n) => n.ToString("#,0", CultureInfo.InvariantCulture);
    }
}
