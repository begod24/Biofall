using UnityEngine;
using Biofall.Core;

namespace Biofall.Gameplay
{
    /// <summary>Bio Sample pickup (the glowing red sphere) — adds currency.</summary>
    public sealed class CurrencyPickup : Pickup
    {
        [SerializeField] private int amount = 1;

        protected override void OnCollected() => CurrencyWallet.Add(amount);
    }
}
