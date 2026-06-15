using UnityEngine;
using Biofall.Core;

namespace Biofall.Gameplay
{
    /// <summary>Ammo pickup (the dark-green cube) — tops up the player's reserve.</summary>
    public sealed class AmmoPickup : Pickup
    {
        [SerializeField] private int amount = 12;

        protected override void OnCollected()
        {
            if (!PlayerRegistry.HasPlayer) return;

            var controller = PlayerRegistry.Player.GetComponent<WeaponController>();
            var ammo = controller != null ? controller.ActiveAmmo : null;
            if (ammo == null) ammo = PlayerRegistry.Player.GetComponentInChildren<AmmoSystem>(); // fallback
            if (ammo != null) ammo.AddRounds(amount);
        }
    }
}
