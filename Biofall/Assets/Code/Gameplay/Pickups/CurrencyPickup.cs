using UnityEngine;
using Biofall.Core;

namespace Biofall.Gameplay
{
    public sealed class CurrencyPickup : Pickup
    {
        [SerializeField] private int amount = 1;

        protected override void OnCollected() => CurrencyWallet.Add(amount);
    }
}
