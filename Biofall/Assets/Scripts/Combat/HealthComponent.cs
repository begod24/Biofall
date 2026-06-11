using System;
using UnityEngine;

namespace Biofall.Combat
{
    /// <summary>
    /// Shared health/damage handler for any entity (player and enemies alike).
    /// Centralizes death so other systems only need to listen, not re-implement.
    /// </summary>
    public class HealthComponent : MonoBehaviour, IDamageable
    {
        [SerializeField] private float _maxHealth = 100f;

        private float _current;

        public float Max => _maxHealth;
        public float Current => _current;
        public float Normalized => _maxHealth > 0f ? _current / _maxHealth : 0f;
        public bool IsAlive => _current > 0f;

        /// <summary>Fired on every damage application that lands on a living target. (damageInfo)</summary>
        public event Action<DamageInfo> OnDamaged;
        /// <summary>Fired once when health reaches zero. (lastHit)</summary>
        public event Action<DamageInfo> OnDied;
        /// <summary>Fired whenever current/max changes (incl. heal/reset). (current, max)</summary>
        public event Action<float, float> OnHealthChanged;

        private void Awake() => ResetHealth();

        public void ResetHealth()
        {
            _current = _maxHealth;
            OnHealthChanged?.Invoke(_current, _maxHealth);
        }

        /// <summary>Used by pooled enemies that vary HP per spawn.</summary>
        public void Configure(float maxHealth)
        {
            _maxHealth = maxHealth;
            ResetHealth();
        }

        public void ApplyDamage(in DamageInfo damageInfo)
        {
            if (!IsAlive) return;

            _current = Mathf.Max(0f, _current - damageInfo.Amount);
            OnHealthChanged?.Invoke(_current, _maxHealth);
            OnDamaged?.Invoke(damageInfo);

            if (_current <= 0f)
            {
                OnDied?.Invoke(damageInfo);
            }
        }

        public void Heal(float amount)
        {
            if (!IsAlive || amount <= 0f) return;
            _current = Mathf.Min(_maxHealth, _current + amount);
            OnHealthChanged?.Invoke(_current, _maxHealth);
        }
    }
}
