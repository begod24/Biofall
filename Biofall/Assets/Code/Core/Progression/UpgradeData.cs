using UnityEngine;

namespace Biofall.Core
{
    /// <summary>Which player stat an upgrade affects (read by <see cref="PlayerProgression"/>).</summary>
    public enum UpgradeStat
    {
        MaxHealth,       // flat +HP
        MoveSpeed,       // percent (+% move speed)
        HealthRegen,     // flat HP/sec out-of-combat regen
        ReviveSpeed,     // percent (faster teammate revive — co-op)
        GrenadeCapacity, // flat +max grenades
        PickupRadius     // flat +metres on the loot collect radius
    }

    /// <summary>How a tier's value combines into the final stat.</summary>
    public enum UpgradeApply
    {
        Flat,    // value is added (e.g. +25 HP)
        Percent  // value is a fraction added to a multiplier (e.g. 0.10 = +10%)
    }

    /// <summary>
    /// One purchasable upgrade LINE (data-driven, like <see cref="WeaponData"/>/EnemyData). Each
    /// <see cref="Tier"/> is a level the player buys up to with Bio Samples; <see cref="value"/> at a
    /// tier is the TOTAL bonus at that level (cumulative, not per-step). Owned levels + spending live in
    /// the persistent <see cref="PlayerProgression"/> save; this asset is pure config.
    /// </summary>
    [CreateAssetMenu(menuName = "Biofall/Upgrade Data", fileName = "UPG_New")]
    public sealed class UpgradeData : ScriptableObject
    {
        [Tooltip("Stable save key — NEVER rename once players have saves (e.g. \"max_health\").")]
        public string id;
        public string displayName;
        [TextArea] public string description;
        public Sprite icon;

        public UpgradeStat stat;
        public UpgradeApply apply = UpgradeApply.Flat;

        [Tooltip("One entry per level (index 0 = level 1). cost = Bio Samples to reach it; value = TOTAL bonus at that level.")]
        public Tier[] tiers;

        [System.Serializable]
        public struct Tier
        {
            [Min(0)] public int cost;
            public float value;
        }

        /// <summary>Highest level this line can reach (= number of tiers).</summary>
        public int MaxLevel => tiers != null ? tiers.Length : 0;

        /// <summary>Total bonus value at <paramref name="level"/> (0 = not bought yet).</summary>
        public float ValueAtLevel(int level)
        {
            if (tiers == null || level <= 0) return 0f;
            return tiers[Mathf.Clamp(level - 1, 0, tiers.Length - 1)].value;
        }

        /// <summary>Bio Samples needed to buy the NEXT level from <paramref name="currentLevel"/>, or -1 if maxed.</summary>
        public int CostForNext(int currentLevel)
        {
            if (tiers == null || currentLevel >= tiers.Length) return -1;
            return tiers[Mathf.Max(0, currentLevel)].cost;
        }
    }
}
