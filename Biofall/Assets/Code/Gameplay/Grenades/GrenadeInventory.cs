using UnityEngine;
using Biofall.Core;

namespace Biofall.Gameplay
{
    /// <summary>
    /// Player's grenade pouch. Owns the count, enforces the cap, and announces changes via
    /// <see cref="GrenadeCountChanged"/> (Observer) so the HUD just listens. Thrower consumes,
    /// pickups add.
    /// </summary>
    public sealed class GrenadeInventory : MonoBehaviour
    {
        [SerializeField] private int startCount = 3;
        [SerializeField] private int maxCount = 5;

        public int Count { get; private set; }
        public int Max => maxCount;

        private bool _init;

        private void Start() => EnsureInit();

        // Idempotent base init. Guarded so it runs once regardless of whether Start or a persistent
        // upgrade (ApplyCapacityBonus) lands first — otherwise a late Start would wipe the bonus top-up.
        private void EnsureInit()
        {
            if (_init) return;
            _init = true;
            Count = Mathf.Clamp(startCount, 0, maxCount);
            Broadcast();
        }

        /// <summary>Spend one grenade. False if the pouch is empty.</summary>
        public bool TryConsume()
        {
            if (Count <= 0) return false;
            Count--;
            Broadcast();
            return true;
        }

        /// <summary>Raise the carry cap by <paramref name="bonus"/> (persistent upgrade — see PlayerLoadout)
        /// and top up the pouch by the same amount so the player starts the run with more.</summary>
        public void ApplyCapacityBonus(int bonus)
        {
            EnsureInit();           // make sure the base count is set before we top up
            if (bonus <= 0) return;
            maxCount += bonus;
            Count = Mathf.Min(maxCount, Count + bonus);
            Broadcast();
        }

        /// <summary>Add grenades (clamped to the cap). Returns true if at least one was added.</summary>
        public bool Add(int amount = 1)
        {
            if (amount <= 0 || Count >= maxCount) return false;
            Count = Mathf.Min(maxCount, Count + amount);
            Broadcast();
            return true;
        }

        private void Broadcast() => EventBus.Publish(new GrenadeCountChanged(Count, maxCount));
    }
}
