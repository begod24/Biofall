using UnityEngine;
using Biofall.Core;

namespace Biofall.Gameplay
{
    public sealed class GrenadeInventory : MonoBehaviour
    {
        [SerializeField] private int startCount = 3;
        [SerializeField] private int maxCount = 5;

        public int Count { get; private set; }
        public int Max => maxCount;

        private bool _init;

        private void Start() => EnsureInit();

        private void EnsureInit()
        {
            if (_init) return;
            _init = true;
            Count = Mathf.Clamp(startCount, 0, maxCount);
            Broadcast();
        }

        public bool TryConsume()
        {
            if (Count <= 0) return false;
            Count--;
            Broadcast();
            return true;
        }

        public void ApplyCapacityBonus(int bonus)
        {
            EnsureInit();
            if (bonus <= 0) return;
            maxCount += bonus;
            Count = Mathf.Min(maxCount, Count + bonus);
            Broadcast();
        }

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
