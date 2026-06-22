using UnityEngine;
using Biofall.Core;

namespace Biofall.Gameplay
{
    [RequireComponent(typeof(ParticleSystem))]
    public sealed class PooledParticle : MonoBehaviour, IPoolable
    {
        private ParticleSystem _ps;
        private float _life;
        private float _timer;

        private void Awake()
        {
            _ps = GetComponent<ParticleSystem>();
            ParticleSystem.MainModule main = _ps.main;
            _life = main.duration + main.startLifetime.constantMax;
        }

        public void OnSpawned()
        {
            _ps.Clear(true);
            _ps.Play(true);
            _timer = _life;
        }

        public void OnDespawned() { }

        private void Update()
        {
            _timer -= Time.deltaTime;
            if (_timer <= 0f)
            {
                if (PoolService.Instance != null) PoolService.Instance.Despawn(gameObject);
                else gameObject.SetActive(false);
            }
        }
    }
}
