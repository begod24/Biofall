using UnityEngine;
using Biofall.Core;

namespace Biofall.Gameplay
{
    /// <summary>
    /// Visual tracer (Object Pooling). Flies straight along its forward at a set speed for a
    /// short lifetime, then returns to the pool. Damage is resolved by the weapon's hitscan,
    /// so the tracer needs no collider — it's purely cosmetic.
    /// </summary>
    public sealed class Bullet : MonoBehaviour, IPoolable
    {
        [SerializeField] private float speed = 80f;
        [SerializeField] private float maxLifetime = 1.2f;

        private float _life;

        /// <summary>Set per-shot motion before/after spawning.</summary>
        public void Launch(float bulletSpeed, float lifetime)
        {
            speed = bulletSpeed;
            maxLifetime = lifetime;
            _life = lifetime;
        }

        public void OnSpawned() => _life = maxLifetime;
        public void OnDespawned() { }

        private void Update()
        {
            transform.position += transform.forward * (speed * Time.deltaTime);

            _life -= Time.deltaTime;
            if (_life <= 0f)
            {
                if (PoolService.Instance != null) PoolService.Instance.Despawn(gameObject);
                else gameObject.SetActive(false);
            }
        }
    }
}
