using UnityEngine;
using Biofall.Core;

namespace Biofall.Gameplay
{
    // A lobbed acid glob the Spitter spits at the player. Flies in a parabolic arc (so you clearly see
    // it coming) and bursts into a lingering AcidPool on impact. Pooled and purely time-driven — no
    // physics/collider, so it never interferes with movement or hitscan. Coop-safe: the glob and pool
    // are visual on every client; only the pool applies damage, and only on the server.
    public sealed class AcidProjectile : MonoBehaviour, IPoolable
    {
        [Tooltip("Visual child that spins in flight (left null = this transform).")]
        [SerializeField] private Transform visual;
        [SerializeField] private float spinSpeed = 540f;

        private Transform _tf;
        private Vector3 _start;
        private Vector3 _target;
        private float _flightTime;
        private float _arcHeight;
        private float _timer;
        private bool _flying;
        private SpitterData _data;

        private void Awake()
        {
            _tf = transform;
            if (visual == null) visual = _tf;
        }

        public void Launch(Vector3 start, Vector3 target, SpitterData data)
        {
            _start = start;
            _target = target;
            _data = data;
            _flightTime = Mathf.Max(0.1f, data.spitFlightTime);
            _arcHeight = Mathf.Max(0f, data.spitArcHeight);
            _timer = 0f;
            _flying = true;
            _tf.position = start;
        }

        public void OnSpawned() { /* Launch() is called right after spawning. */ }

        public void OnDespawned() => _flying = false;

        private void Update()
        {
            if (!_flying) return;

            _timer += Time.deltaTime;
            float t = Mathf.Clamp01(_timer / _flightTime);

            Vector3 pos = Vector3.Lerp(_start, _target, t);
            pos.y += _arcHeight * 4f * t * (1f - t);   // parabola, peaks at mid-flight
            _tf.position = pos;

            if (visual != null && spinSpeed != 0f)
                visual.Rotate(Vector3.one, spinSpeed * Time.deltaTime, Space.Self);

            if (t >= 1f) Impact();
        }

        private void Impact()
        {
            _flying = false;

            if (PoolService.Instance != null && _data != null)
            {
                if (_data.spitVfxPrefab != null)
                    PoolService.Instance.Spawn(_data.spitVfxPrefab, _target + Vector3.up * 0.05f, Quaternion.identity);

                if (_data.acidPoolPrefab != null)
                {
                    GameObject go = PoolService.Instance.Spawn(_data.acidPoolPrefab, _target + Vector3.up * 0.02f, Quaternion.identity);
                    if (go != null && go.TryGetComponent(out AcidPool pool))
                        pool.Configure(_data.poolRadius, _data.poolLifetime, _data.acidDamagePerTick, _data.acidTickInterval);
                }
            }

            if (PoolService.Instance != null) PoolService.Instance.Despawn(gameObject);
            else gameObject.SetActive(false);
        }
    }
}
