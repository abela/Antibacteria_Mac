using System;
using AntiBacteria.Gameplay.Weapons;
using UnityEngine;

namespace AntiBacteria.Core
{
    /// <summary>What the Upgrade Character screen sells (CharacterUpgradeLayer.mm, Docs/DesignReference/01 section 5).</summary>
    public enum UpgradeItem { WaterShot, WindmillShot, CircleShot, TornadoShot, DoubleSpeed, Immortality }

    [Serializable]
    public class ShopEntry
    {
        public UpgradeItem item;
        public WordIDs nameId;
        [Tooltip("Price in vitamins.")]
        [Min(0)] public int price;
    }

    /// <summary>
    /// Upgrade shop prices and effects. Edit in Project Settings > AntiBacteria > Upgrade Shop.
    /// One-off unlocks paid with vitamins (2013: no multi-level upgrades). Lives in Resources so builds read the same values.
    /// </summary>
    [CreateAssetMenu(menuName = "AntiBacteria/Shop Settings", fileName = "ShopSettings")]
    public class ShopSettings : ScriptableObject
    {
        public const string ResourcePath = "ShopSettings";

        [Tooltip("In the order shown. 2013 prices: shots 20 000, Double Speed 40 000, Immortality 1 000 000.")]
        public ShopEntry[] entries = Defaults();

        [Tooltip("Movement multiplier of the bought Double Speed (the bonus of the same name adds nothing on top, as in 2013).")]
        public float doubleSpeedMultiplier = 2f;

        private static ShopSettings current;

        public static ShopSettings Current
        {
            get
            {
                if (current != null) return current;
                current = Resources.Load<ShopSettings>(ResourcePath);
                if (current == null)
                {
                    current = CreateInstance<ShopSettings>();
                    current.hideFlags = HideFlags.HideAndDontSave;
                }
                return current;
            }
        }

        public static void Use(ShopSettings settings) => current = settings;

        public ShopEntry Get(UpgradeItem item)
        {
            foreach (ShopEntry e in entries) if (e != null && e.item == item) return e;
            return null;
        }

        /// <summary>The shot type a shot item unlocks (null for the others).</summary>
        public static ShotKind? ShotFor(UpgradeItem item)
        {
            switch (item)
            {
                case UpgradeItem.WaterShot: return ShotKind.Water;
                case UpgradeItem.WindmillShot: return ShotKind.Windmill;
                case UpgradeItem.CircleShot: return ShotKind.Circle;
                case UpgradeItem.TornadoShot: return ShotKind.Tornado;
                default: return null;
            }
        }

        public static UpgradeItem? ItemFor(ShotKind shot)
        {
            switch (shot)
            {
                case ShotKind.Water: return UpgradeItem.WaterShot;
                case ShotKind.Windmill: return UpgradeItem.WindmillShot;
                case ShotKind.Circle: return UpgradeItem.CircleShot;
                case ShotKind.Tornado: return UpgradeItem.TornadoShot;
                default: return null;
            }
        }

        public static ShopEntry[] Defaults() => new[]
        {
            new ShopEntry { item = UpgradeItem.WaterShot, nameId = WordIDs.WaterShoot, price = 20000 },
            new ShopEntry { item = UpgradeItem.WindmillShot, nameId = WordIDs.WindmillShoot, price = 20000 },
            new ShopEntry { item = UpgradeItem.CircleShot, nameId = WordIDs.CircleShoot, price = 20000 },
            new ShopEntry { item = UpgradeItem.TornadoShot, nameId = WordIDs.TornadoShoot, price = 20000 },
            new ShopEntry { item = UpgradeItem.DoubleSpeed, nameId = WordIDs.DoubleSpeed, price = 40000 },
            new ShopEntry { item = UpgradeItem.Immortality, nameId = WordIDs.Immortality, price = 1000000 },
        };
    }
}
