using UnityEngine;
using System;

namespace Biofall.Combat
{
    public class HealthComponent : MonoBehaviour, IDamageable
    {
        [SerializeField] private float maxHealth = 100f;
        private float current;

        public float Max => maxHealth;
        public float Current => current;
        public bool IsAlive => current > 0f;
        public float Normalized => maxHealth <= 0f ? 0f : current / maxHealth;

        public event Action<HealthComponent, DamageInfo> OnDamaged;
        public event Action<HealthComponent> OnDied;
        public event Action<HealthComponent> OnHealthChanged;

        private void Awake()
        {
            current = maxHealth;
        }

        public void TakeDamage(DamageInfo info)
        {
            if (!IsAlive) return;
            current = Mathf.Max(0f, current - info.Amount);
            OnDamaged?.Invoke(this, info);
            OnHealthChanged?.Invoke(this);
            if (current <= 0f) OnDied?.Invoke(this);
        }

        public void Heal(float amount)
        {
            if (!IsAlive) return;
            current = Mathf.Min(maxHealth, current + amount);
            OnHealthChanged?.Invoke(this);
        }

        public void ResetHealth()
        {
            current = maxHealth;
            OnHealthChanged?.Invoke(this);
        }
    }
}
