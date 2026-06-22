using UnityEngine;
using Biofall.Core;

namespace Biofall.Gameplay
{
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
