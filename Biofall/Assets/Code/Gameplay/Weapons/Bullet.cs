using UnityEngine;
using Biofall.Core;

namespace Biofall.Gameplay
{
    public sealed class Bullet : MonoBehaviour, IPoolable
    {
        [SerializeField] private float speed = 80f;
        [SerializeField] private float maxLifetime = 1.2f;

        private float _life;

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
