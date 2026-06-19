using UnityEngine;
using Biofall.Core;

namespace Biofall.Gameplay
{
    /// <summary>
    /// Player ammo: rounds currently in the magazine + spare rounds in reserve.
    /// A player-only concept, so it publishes <see cref="AmmoChanged"/> directly (Observer).
    /// The weapon (Phase 3) calls <see cref="TryConsume"/> / <see cref="Reload"/>;
    /// pickups call <see cref="AddRounds"/>. UI only listens — it never calls these.
    /// </summary>
    public sealed class AmmoSystem : MonoBehaviour
    {
        [SerializeField] private int magazineSize = 12;
        [SerializeField] private int rounds = 12;   // currently in the magazine
        [SerializeField] private int reserve = 48;  // spare rounds

        public int Rounds => rounds;
        public int Reserve => reserve;
        public int MagazineSize => magazineSize;

        private bool _infinite;

        /// <summary>Set by the owning <see cref="Weapon"/> from its data — HUD then shows ∞.</summary>
        public void SetInfinite(bool infinite)
        {
            _infinite = infinite;
            Broadcast();
        }

        private void Start()
        {
            Broadcast(); // initial sync for the HUD
        }

        private void OnEnable()
        {
            // When this weapon becomes the active one, refresh the HUD with ITS counts.
            Broadcast();
        }

        /// <summary>Spend rounds to fire. False if the magazine doesn't have enough.</summary>
        public bool TryConsume(int amount = 1)
        {
            if (amount <= 0 || rounds < amount) return false;

            rounds -= amount;
            Broadcast();
            return true;
        }

        /// <summary>Refill the magazine from reserve.</summary>
        public void Reload()
        {
            int need = magazineSize - rounds;
            if (need <= 0 || reserve <= 0) return;

            int moved = Mathf.Min(need, reserve);
            rounds += moved;
            reserve -= moved;
            Broadcast();
        }

        /// <summary>Pickups add spare rounds.</summary>
        public void AddRounds(int amount)
        {
            if (amount <= 0) return;

            reserve += amount;
            Broadcast();
        }

        private void Broadcast()
        {
            EventBus.Publish(new AmmoChanged(rounds, reserve, _infinite));
        }
    }
}
