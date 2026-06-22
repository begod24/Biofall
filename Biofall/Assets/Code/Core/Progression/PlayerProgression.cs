using System;
using UnityEngine;

namespace Biofall.Core
{
    /// <summary>
    /// PERSISTENT meta-progression (static, PlayerPrefs-backed, like <see cref="GameSettings"/>). Bio
    /// Samples earned in a run are banked on extraction (<see cref="DepositRunSamples"/>) into a
    /// permanent <see cref="BankedSamples"/> pool the player spends in the upgrade shop between missions
    /// (<see cref="TryPurchase"/>). Purchased levels persist forever, so the gameplay systems read the
    /// computed stat bonuses at spawn (see PlayerLoadout).
    ///
    /// CO-OP: each process keeps its own bank + levels (this is static = one per machine), so progression
    /// stays PER-PLAYER for free — exactly like <see cref="CurrencyWallet"/>. Solo and co-op share this.
    ///
    /// The upgrade catalog is loaded once from <c>Resources/UpgradeCatalog</c> (no scene wiring).
    /// </summary>
    public static class PlayerProgression
    {
        private const string CatalogResource = "UpgradeCatalog";
        private const string BankKey = "bf_bank_samples";
        private const string LevelKeyPrefix = "bf_upg_";

        private static UpgradeCatalog _catalog;
        private static bool _loaded;

        /// <summary>Raised whenever the bank or any level changes — shop UI / HUD refresh hook.</summary>
        public static event Action Changed;

        /// <summary>Permanent Bio Samples available to spend on upgrades.</summary>
        public static int BankedSamples { get; private set; }

        /// <summary>The loaded catalog (may be null if the asset is missing).</summary>
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

        // ---- bank ----

        /// <summary>Bank the samples collected during a run (call on mission success / extraction).</summary>
        public static void DepositRunSamples(int amount)
        {
            if (amount <= 0) return;
            EnsureLoaded();
            BankedSamples += amount;
            PlayerPrefs.SetInt(BankKey, BankedSamples);
            PlayerPrefs.Save();
            Changed?.Invoke();
        }

        // ---- levels / purchase ----

        /// <summary>Current owned level of an upgrade (0 = not bought).</summary>
        public static int GetLevel(string id)
        {
            if (string.IsNullOrEmpty(id)) return 0;
            return PlayerPrefs.GetInt(LevelKeyPrefix + id, 0);
        }

        public static int GetLevel(UpgradeData data) => data != null ? GetLevel(data.id) : 0;

        /// <summary>True if there's a next tier AND the bank can afford it.</summary>
        public static bool CanPurchase(UpgradeData data)
        {
            if (data == null) return false;
            EnsureLoaded();
            int cost = data.CostForNext(GetLevel(data.id));
            return cost >= 0 && BankedSamples >= cost;
        }

        /// <summary>Buy the next level of <paramref name="data"/>; returns true on success.</summary>
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

        /// <summary>Wipe all upgrades + bank (new-game / debug). Keeps it explicit — never auto-called.</summary>
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

        // ---- aggregated stat bonuses (read by the player systems at spawn) ----

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

        /// <summary>Flat +HP added to the player's max health.</summary>
        public static float MaxHealthBonus => Sum(UpgradeStat.MaxHealth, UpgradeApply.Flat);

        /// <summary>Multiplier (1.0 = none) applied to move speed.</summary>
        public static float MoveSpeedMultiplier => 1f + Sum(UpgradeStat.MoveSpeed, UpgradeApply.Percent);

        /// <summary>HP regenerated per second (0 = none).</summary>
        public static float HealthRegenPerSecond => Sum(UpgradeStat.HealthRegen, UpgradeApply.Flat);

        /// <summary>Multiplier on the revive hold time (less = faster); clamped so it never trivialises.</summary>
        public static float ReviveHoldMultiplier => Mathf.Clamp(1f - Sum(UpgradeStat.ReviveSpeed, UpgradeApply.Percent), 0.25f, 1f);

        /// <summary>Flat extra grenade carry capacity.</summary>
        public static int GrenadeCapacityBonus => Mathf.RoundToInt(Sum(UpgradeStat.GrenadeCapacity, UpgradeApply.Flat));

        /// <summary>Flat extra metres on the pickup collect radius.</summary>
        public static float PickupRadiusBonus => Sum(UpgradeStat.PickupRadius, UpgradeApply.Flat);
    }
}
