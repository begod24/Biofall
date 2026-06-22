using UnityEngine;
using Biofall.Core;

namespace Biofall.Gameplay
{
    public sealed class AmmoPickup : Pickup
    {
        [SerializeField] private int amount = 12;

        protected override void OnCollected()
        {
            if (!PlayerRegistry.HasPlayer) return;

            var controller = PlayerRegistry.Player.GetComponentInParent<WeaponController>();
            if (controller != null) controller.AddReserveAmmo(amount);
        }
    }
}
