using System;
using UnityEngine;
using Biofall.Core;

namespace Biofall.Gameplay
{
    /// <summary>
    /// Generic combat health (Encapsulation). Implements <see cref="IHealth"/> and
    /// <see cref="IDamageable"/> and is reused by BOTH the player and enemies/targets.
    /// To stay reusable it does NOT touch the EventBus — it raises local C# events and
    /// lets a reporter translate them into the right global event (PlayerDamaged vs
    /// TargetDamaged). One responsibility: own the HP number and announce changes.
    /// </summary>
    public sealed class Health : MonoBehaviour, IHealth, IDamageable
    {
        [SerializeField] private float maxHealth = 100f;

        public float Current { get; private set; }
        public float Max => maxHealth;
        public bool IsAlive => Current > 0f && !_dead;

        /// <summary>(hit info, current HP after the hit)</summary>
        public event Action<DamageInfo, float> Damaged;
        /// <summary>(current HP after heal, max)</summary>
        public event Action<float, float> Healed;
        public event Action Died;

        private bool _dead;

        private void Awake()
        {
            Current = maxHealth;
        }

        public void TakeDamage(in DamageInfo info)
        {
            if (_dead || info.Amount <= 0f) return;

            Current = Mathf.Max(0f, Current - info.Amount);
            Damaged?.Invoke(info, Current);

            if (Current <= 0f)
            {
                _dead = true;
                Died?.Invoke();
            }
        }

        public void Heal(float amount)
        {
            if (_dead || amount <= 0f) return;

            Current = Mathf.Min(maxHealth, Current + amount);
            Healed?.Invoke(Current, maxHealth);
        }

        public void SetMax(float max, bool refill)
        {
            maxHealth = Mathf.Max(1f, max);
            Current = refill ? maxHealth : Mathf.Min(Current, maxHealth);
        }
    }
}
