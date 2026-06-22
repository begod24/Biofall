using UnityEngine;
using Biofall.Core;

namespace Biofall.Gameplay
{
    public sealed class HealthPickup : Pickup
    {
        [SerializeField] private float healAmount = 25f;

        protected override void OnCollected()
        {
            if (!PlayerRegistry.HasPlayer) return;

            var health = PlayerRegistry.Player.GetComponentInParent<Health>();
            if (health != null) health.Heal(healAmount);
        }
    }
}
