using AntiBacteria.Audio;
using System.Globalization;
using AntiBacteria.Core;
using AntiBacteria.Gameplay.Weapons;
using AntiBacteria.Rendering;
using AntiBacteria.UI.Menu;
using UnityEngine;

namespace AntiBacteria.UI.Menus
{
    /// <summary>
    /// Upgrade Character: spend vitamins on shot types, Double Speed and Immortality
    /// (port of CharacterUpgradeLayer, Docs/DesignReference/05 section 3.5; prices in ShopSettings).
    /// A shot is equipped when bought and can be switched freely afterwards; Double Speed and Immortality are switched
    /// on when bought and can be toggled. 2013's real-money Vitamin Store is not part of the Steam release.
    /// </summary>
    public static class UpgradeMenu
    {
        private const float RowX = 36f, RowWidth = 248f, FirstRowY = 42f, RowStep = 14f;

        public static MenuScreen Build(UISkin skin, Transform parent, MenuNavigator nav)
        {
            MenuScreen s = MenuScreen.Create(parent, "Upgrade");
            Transform t = s.transform;
            LiveText.Create(skin, t, "Title", 160f, 8f, () => Loc.Get(WordIDs.UpgradeCharacter).ToUpperInvariant(), role: PaletteRole.Accent);
            LiveText.Create(skin, t, "Wallet", 160f, 24f,
                () => Loc.Get(WordIDs.Vitamins) + ": " + Format(Progress.Vitamins));

            LiveText message = LiveText.Create(skin, t, "Message", 160f, FirstRowY + 7f * RowStep + 2f,
                () => Loc.Get(WordIDs.NoVitamins), role: PaletteRole.Accent);
            HideAfter messageTimer = message.gameObject.AddComponent<HideAfter>();
            message.gameObject.SetActive(false);

            float y = FirstRowY;
            // Normal Shot: free, always owned.
            s.Add(ShopRow.Create(skin, t, "Normal", RowX, y, RowWidth, () => Loc.Get(WordIDs.NormalShoot),
                () => Progress.SelectedShot == ShotKind.Normal ? Loc.Get(WordIDs.Equipped) : Loc.Get(WordIDs.Owned),
                () => Progress.SelectedShot == ShotKind.Normal ? PaletteRole.Accent : PaletteRole.Foreground,
                () => Progress.SelectedShot != ShotKind.Normal,
                () => { Progress.SelectShot(ShotKind.Normal); s.RefreshAll(); }));

            foreach (ShopEntry entry in ShopSettings.Current.entries)
            {
                if (entry == null) continue;
                y += RowStep;
                ShopEntry e = entry;
                s.Add(ShopRow.Create(skin, t, e.item.ToString(), RowX, y, RowWidth, () => Loc.Get(e.nameId),
                    () => Value(e), () => Role(e), () => Dim(e),
                    () =>
                    {
                        if (!Activate(e)) { messageTimer.Show(2f); Sound.Play(Sfx.UiError); }
                        s.RefreshAll();
                    }));
            }

            s.Add(MenuButton.Create(skin, t, "Back", 160f, 160f, () => Loc.Get(WordIDs.Back), nav.Back));
            return s;
        }

        /// <summary>Buy, equip or toggle. False when it couldn't be bought.</summary>
        private static bool Activate(ShopEntry e)
        {
            ShotKind? shot = ShopSettings.ShotFor(e.item);
            if (!Progress.Owns(e.item))
            {
                if (!Progress.TryBuy(e.item, e.price)) return false;
                // Bought: equip the shot / switch the ability on (2013 behaviour).
                if (shot.HasValue) Progress.SelectShot(shot.Value);
                else if (e.item == UpgradeItem.DoubleSpeed) Progress.SetDoubleSpeed(true);
                else if (e.item == UpgradeItem.Immortality) Progress.SetImmortality(true);
                return true;
            }
            if (shot.HasValue) Progress.SelectShot(shot.Value);
            else if (e.item == UpgradeItem.DoubleSpeed) Progress.SetDoubleSpeed(!Progress.DoubleSpeedOn);
            else if (e.item == UpgradeItem.Immortality) Progress.SetImmortality(!Progress.ImmortalityOn);
            return true;
        }

        private static string Value(ShopEntry e)
        {
            if (!Progress.Owns(e.item)) return Format(e.price);
            ShotKind? shot = ShopSettings.ShotFor(e.item);
            if (shot.HasValue) return Loc.Get(Progress.SelectedShot == shot.Value ? WordIDs.Equipped : WordIDs.Owned);
            return Loc.Get(IsOn(e.item) ? WordIDs.On : WordIDs.Off);
        }

        private static PaletteRole Role(ShopEntry e)
        {
            if (!Progress.Owns(e.item)) return PaletteRole.Foreground;
            ShotKind? shot = ShopSettings.ShotFor(e.item);
            bool active = shot.HasValue ? Progress.SelectedShot == shot.Value : IsOn(e.item);
            return active ? PaletteRole.Accent : PaletteRole.Foreground;
        }

        /// <summary>Dim: a price you can't pay yet, or an owned item that isn't in use.</summary>
        private static bool Dim(ShopEntry e)
        {
            if (!Progress.Owns(e.item)) return Progress.Vitamins < e.price;
            return Role(e) != PaletteRole.Accent;
        }

        private static bool IsOn(UpgradeItem item) =>
            item == UpgradeItem.DoubleSpeed ? Progress.DoubleSpeedOn : item == UpgradeItem.Immortality && Progress.ImmortalityOn;

        private static string Format(int n) => n.ToString("#,0", CultureInfo.InvariantCulture);
    }
}
