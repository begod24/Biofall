using UnityEngine;
using Biofall.Core;

namespace Biofall.Gameplay
{
    /// <summary>Grenade pickup — adds to the player's <see cref="GrenadeInventory"/> (clamped to its cap).
    /// Reuses all of <see cref="Pickup"/>'s pooling/spin/collect logic.</summary>
    public sealed class GrenadePickup : Pickup
    {
        [SerializeField] private int amount = 1;

        protected override void OnCollected()
        {
            if (!PlayerRegistry.HasPlayer) return;

            var inventory = PlayerRegistry.Player.GetComponentInParent<GrenadeInventory>();
            if (inventory != null) inventory.Add(amount);
        }
    }
}
