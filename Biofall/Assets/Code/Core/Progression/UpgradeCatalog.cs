using UnityEngine;

namespace Biofall.Core
{
    /// <summary>
    /// The full list of purchasable upgrades. <see cref="PlayerProgression"/> loads ONE of these from
    /// <c>Resources/UpgradeCatalog</c> (zero scene wiring) to resolve ids → data and aggregate stat
    /// bonuses. The upgrade shop UI iterates <see cref="Upgrades"/> to draw rows. Add content by
    /// dropping new <see cref="UpgradeData"/> assets in here.
    /// </summary>
    [CreateAssetMenu(menuName = "Biofall/Upgrade Catalog", fileName = "UpgradeCatalog")]
    public sealed class UpgradeCatalog : ScriptableObject
    {
        [SerializeField] private UpgradeData[] upgrades;

        public UpgradeData[] Upgrades => upgrades;

        /// <summary>Find an upgrade by its stable id (null if none).</summary>
        public UpgradeData Find(string id)
        {
            if (upgrades == null || string.IsNullOrEmpty(id)) return null;
            foreach (var u in upgrades)
                if (u != null && u.id == id) return u;
            return null;
        }
    }
}
