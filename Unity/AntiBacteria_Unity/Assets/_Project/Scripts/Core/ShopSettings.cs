using System;
using UnityEngine;

namespace AntiBacteria.Core
{
    /// <summary>The Upgrade Character shop: permanent upgrades with levels, bought with vitamins.</summary>
    public enum CharacterUpgrade { ExtraHeart, Armor, SwiftCell, BiggerBullets, RapidFire, Magnet, BonusTime }

    /// <summary>The 2013 shop's items. Only read when an old save is converted (they are refunded); not sold any more.</summary>
    public enum UpgradeItem { WaterShot, WindmillShot, CircleShot, TornadoShot, DoubleSpeed, Immortality }

    [Serializable]
    public class UpgradeDef
    {
        public CharacterUpgrade upgrade;
        public WordIDs nameId;
        public WordIDs descId;
        [Tooltip("Price of each level in vitamins; the number of entries is the number of levels.")]
        public int[] prices = new int[0];
        [Tooltip("Effect per level: extra hearts, blocked hits, % faster, bullet size, % faster fire, % range, % longer.")]
        public float perLevel;

        public int MaxLevel => prices != null ? prices.Length : 0;
    }

    /// <summary>
    /// Upgrade shop prices and effects. Edit in Project Settings > AntiBacteria > Upgrade Shop (the asset holds the values; Defaults() is only for a new or empty asset). Upgrades work in the
    /// campaign and the arcade modes, never in the roguelike (every run starts the same: perks are its only power).
    /// Lives in Resources so builds read the same values.
    /// </summary>
    [CreateAssetMenu(menuName = "AntiBacteria/Shop Settings", fileName = "ShopSettings")]
    public class ShopSettings : ScriptableObject
    {
        public const string ResourcePath = "ShopSettings";

        [Tooltip("In the order shown.")]
        public UpgradeDef[] upgrades = Defaults();

        [Tooltip("What the 2013 items cost (Water, Windmill, Circle, Tornado, Double Speed, Immortality): refunded to old saves.")]
        public int[] legacyPrices = { 20000, 20000, 20000, 20000, 40000, 1000000 };

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
                // An asset saved before the upgrades existed: use the defaults.
                if (current.upgrades == null || current.upgrades.Length == 0) current.upgrades = Defaults();
                return current;
            }
        }

        public static void Use(ShopSettings settings) => current = settings;

        public UpgradeDef Get(CharacterUpgrade u)
        {
            foreach (UpgradeDef d in upgrades) if (d != null && d.upgrade == u) return d;
            return null;
        }

        /// <summary>Total levels on sale (the All Upgrades achievement).</summary>
        public int TotalLevels
        {
            get
            {
                int n = 0;
                foreach (UpgradeDef d in upgrades) if (d != null) n += d.MaxLevel;
                return n;
            }
        }

        /// <summary>
        /// Long-term goals (7 Nov 2026: three times the first prices): about 230,000 vitamins for everything, so the
        /// shop keeps paying back replays of the campaign, the arcade modes and the higher difficulties.
        /// </summary>
        public static UpgradeDef[] Defaults() => new[]
        {
            new UpgradeDef { upgrade = CharacterUpgrade.ExtraHeart, nameId = WordIDs.UpgExtraHeart, descId = WordIDs.UpgExtraHeartDesc, prices = new[] { 12000, 36000 }, perLevel = 1f },
            new UpgradeDef { upgrade = CharacterUpgrade.Armor, nameId = WordIDs.UpgArmor, descId = WordIDs.UpgArmorDesc, prices = new[] { 18000 }, perLevel = 1f },
            new UpgradeDef { upgrade = CharacterUpgrade.SwiftCell, nameId = WordIDs.UpgSwiftCell, descId = WordIDs.UpgSwiftCellDesc, prices = new[] { 4500, 12000, 24000 }, perLevel = 8f },
            new UpgradeDef { upgrade = CharacterUpgrade.BiggerBullets, nameId = WordIDs.UpgBiggerBullets, descId = WordIDs.UpgBiggerBulletsDesc, prices = new[] { 27000 }, perLevel = 1f },
            new UpgradeDef { upgrade = CharacterUpgrade.RapidFire, nameId = WordIDs.UpgRapidFire, descId = WordIDs.UpgRapidFireDesc, prices = new[] { 6000, 15000, 30000 }, perLevel = 10f },
            new UpgradeDef { upgrade = CharacterUpgrade.Magnet, nameId = WordIDs.UpgMagnet, descId = WordIDs.UpgMagnetDesc, prices = new[] { 2500, 6000, 12000 }, perLevel = 40f },
            new UpgradeDef { upgrade = CharacterUpgrade.BonusTime, nameId = WordIDs.UpgBonusTime, descId = WordIDs.UpgBonusTimeDesc, prices = new[] { 3000, 7500, 15000 }, perLevel = 20f },
        };
    }

    /// <summary>What the bought upgrades do, read by gameplay code. Neutral in the roguelike.</summary>
    public static class Upgrades
    {
        public static bool On => !RoguelikeRun.Active;

        public static int Level(CharacterUpgrade u) => On ? Progress.UpgradeLevel(u) : 0;

        private static float Per(CharacterUpgrade u)
        {
            UpgradeDef d = ShopSettings.Current.Get(u);
            return d != null ? d.perLevel : 0f;
        }

        public static int ExtraLives => Mathf.RoundToInt(Level(CharacterUpgrade.ExtraHeart) * Per(CharacterUpgrade.ExtraHeart));
        /// <summary>Hits the Armor blocks in each level or run.</summary>
        public static int ArmorPerLevel => Mathf.RoundToInt(Level(CharacterUpgrade.Armor) * Per(CharacterUpgrade.Armor));
        public static float SpeedMultiplier => 1f + Level(CharacterUpgrade.SwiftCell) * Per(CharacterUpgrade.SwiftCell) / 100f;
        public static float FireIntervalMultiplier => Mathf.Pow(1f - Per(CharacterUpgrade.RapidFire) / 100f, Level(CharacterUpgrade.RapidFire));
        /// <summary>Whole-number bullet scale (pixel art stays crisp): 2 with Bigger Bullets.</summary>
        public static int ShotScale => 1 + Mathf.RoundToInt(Level(CharacterUpgrade.BiggerBullets) * Per(CharacterUpgrade.BiggerBullets));
        public static float MagnetMultiplier => 1f + Level(CharacterUpgrade.Magnet) * Per(CharacterUpgrade.Magnet) / 100f;
        public static float BonusDurationMultiplier => 1f + Level(CharacterUpgrade.BonusTime) * Per(CharacterUpgrade.BonusTime) / 100f;

        /// <summary>Armor hits left in the current level (set when it starts).</summary>
        public static int ArmorLeft { get; set; }
    }
}
