using UnityEngine;
using Biofall.Core;

namespace Biofall.Gameplay
{
    /// <summary>Health pickup (medkit) — restores player HP on collect. Heal amount set per-prefab
    /// (small medkit vs big medkit). Reuses all of <see cref="Pickup"/>'s pooling/spin/collect logic.</summary>
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
