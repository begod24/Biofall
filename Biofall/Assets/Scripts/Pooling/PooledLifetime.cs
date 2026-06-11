using UnityEngine;

namespace Biofall.Pooling
{
    /// <summary>
    /// Put on transient pooled prefabs (muzzle flashes, impact VFX, decals). Returns the
    /// object to the pool after <see cref="_lifetime"/> seconds of being active.
    /// </summary>
    public class PooledLifetime : MonoBehaviour, IPoolable
    {
        [SerializeField] private float _lifetime = 1f;

        private float _releaseAt;

        public void OnSpawned() => _releaseAt = Time.time + _lifetime;
        public void OnDespawned() { }

        private void Update()
        {
            if (Time.time >= _releaseAt && PoolManager.Exists)
            {
                PoolManager.Instance.Release(gameObject);
            }
        }
    }
}
