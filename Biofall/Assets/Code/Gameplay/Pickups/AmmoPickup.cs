using UnityEngine;
using Biofall.Core;

namespace Biofall.Gameplay
{
    /// <summary>Ammo pickup (the dark-green cube) — tops up the player's finite weapons (the M4).
    /// The pistol is infinite, so picked-up rounds always go to the rifle, whatever is equipped.</summary>
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
