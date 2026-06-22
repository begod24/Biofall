using UnityEngine;

namespace Biofall.Core
{
    [CreateAssetMenu(menuName = "Biofall/Upgrade Catalog", fileName = "UpgradeCatalog")]
    public sealed class UpgradeCatalog : ScriptableObject
    {
        [SerializeField] private UpgradeData[] upgrades;

        public UpgradeData[] Upgrades => upgrades;

        public UpgradeData Find(string id)
        {
            if (upgrades == null || string.IsNullOrEmpty(id)) return null;
            foreach (var u in upgrades)
                if (u != null && u.id == id) return u;
            return null;
        }
    }
}
