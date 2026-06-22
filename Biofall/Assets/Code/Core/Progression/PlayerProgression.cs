using System;
using UnityEngine;

namespace Biofall.Core
{
    public static class PlayerProgression
    {
        private const string CatalogResource = "UpgradeCatalog";
        private const string BankKey = "bf_bank_samples";
        private const string LevelKeyPrefix = "bf_upg_";

        private static UpgradeCatalog _catalog;
        private static bool _loaded;

        public static event Action Changed;

        public static int BankedSamples { get; private set; }

        public static UpgradeCatalog Catalog { get { EnsureLoaded(); return _catalog; } }

        private static void EnsureLoaded()
        {
            if (_loaded) return;
            _loaded = true;
            _catalog = Resources.Load<UpgradeCatalog>(CatalogResource);
            if (_catalog == null)
                Debug.LogWarning($"[PlayerProgression] No UpgradeCatalog at Resources/{CatalogResource} — upgrades inert.");
            BankedSamples = Mathf.Max(0, PlayerPrefs.GetInt(BankKey, 0));
        }

        public static void DepositRunSamples(int amount)
        {
            if (amount <= 0) return;
            EnsureLoaded();
            BankedSamples += amount;
            PlayerPrefs.SetInt(BankKey, BankedSamples);
            PlayerPrefs.Save();
            Changed?.Invoke();
        }

        public static int GetLevel(string id)
        {
            if (string.IsNullOrEmpty(id)) return 0;
            return PlayerPrefs.GetInt(LevelKeyPrefix + id, 0);
        }

        public static int GetLevel(UpgradeData data) => data != null ? GetLevel(data.id) : 0;

        public static bool CanPurchase(UpgradeData data)
        {
            if (data == null) return false;
            EnsureLoaded();
            int cost = data.CostForNext(GetLevel(data.id));
            return cost >= 0 && BankedSamples >= cost;
        }

        public static bool TryPurchase(UpgradeData data)
        {
            if (!CanPurchase(data)) return false;
            int level = GetLevel(data.id);
            int cost = data.CostForNext(level);

            BankedSamples -= cost;
            PlayerPrefs.SetInt(BankKey, BankedSamples);
            PlayerPrefs.SetInt(LevelKeyPrefix + data.id, level + 1);
            PlayerPrefs.Save();
            Changed?.Invoke();
            return true;
        }

        public static void ResetProgress()
        {
            EnsureLoaded();
            if (_catalog != null && _catalog.Upgrades != null)
                foreach (var u in _catalog.Upgrades)
                    if (u != null && !string.IsNullOrEmpty(u.id))
                        PlayerPrefs.DeleteKey(LevelKeyPrefix + u.id);
            BankedSamples = 0;
            PlayerPrefs.SetInt(BankKey, 0);
            PlayerPrefs.Save();
            Changed?.Invoke();
        }

        private static float Sum(UpgradeStat stat, UpgradeApply apply)
        {
            EnsureLoaded();
            if (_catalog == null || _catalog.Upgrades == null) return 0f;
            float total = 0f;
            foreach (var u in _catalog.Upgrades)
            {
                if (u == null || u.stat != stat || u.apply != apply) continue;
                total += u.ValueAtLevel(GetLevel(u.id));
            }
            return total;
        }

        public static float MaxHealthBonus => Sum(UpgradeStat.MaxHealth, UpgradeApply.Flat);

        public static float MoveSpeedMultiplier => 1f + Sum(UpgradeStat.MoveSpeed, UpgradeApply.Percent);

        public static float HealthRegenPerSecond => Sum(UpgradeStat.HealthRegen, UpgradeApply.Flat);

        public static float ReviveHoldMultiplier => Mathf.Clamp(1f - Sum(UpgradeStat.ReviveSpeed, UpgradeApply.Percent), 0.25f, 1f);

        public static int GrenadeCapacityBonus => Mathf.RoundToInt(Sum(UpgradeStat.GrenadeCapacity, UpgradeApply.Flat));

        public static float PickupRadiusBonus => Sum(UpgradeStat.PickupRadius, UpgradeApply.Flat);
    }
}
