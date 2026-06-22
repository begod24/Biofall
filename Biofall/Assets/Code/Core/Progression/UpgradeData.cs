using UnityEngine;

namespace Biofall.Core
{
    public enum UpgradeStat
    {
        MaxHealth,
        MoveSpeed,
        HealthRegen,
        ReviveSpeed,
        GrenadeCapacity,
        PickupRadius
    }

    public enum UpgradeApply
    {
        Flat,
        Percent
    }

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

        public int MaxLevel => tiers != null ? tiers.Length : 0;

        public float ValueAtLevel(int level)
        {
            if (tiers == null || level <= 0) return 0f;
            return tiers[Mathf.Clamp(level - 1, 0, tiers.Length - 1)].value;
        }

        public int CostForNext(int currentLevel)
        {
            if (tiers == null || currentLevel >= tiers.Length) return -1;
            return tiers[Mathf.Max(0, currentLevel)].cost;
        }
    }
}
