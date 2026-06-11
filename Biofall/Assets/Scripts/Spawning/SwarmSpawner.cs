using Biofall.Pooling;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Biofall.Spawning
{
    /// <summary>
    /// Stress-test / prototype spawner. Floods the arena with pooled zombies in a ring
    /// around the player so the 100+ swarm and separation can be profiled. The real
    /// pacing system (WaveDirector) replaces this later.
    /// </summary>
    public class SwarmSpawner : MonoBehaviour
    {
        [Header("Prefab")]
        [SerializeField] private GameObject _enemyPrefab;
        [SerializeField] private int _prewarm = 200;

        [Header("Spawn shape")]
        [SerializeField] private Transform _center;
        [SerializeField] private float _minRadius = 12f;
        [SerializeField] private float _maxRadius = 20f;

        [Header("Burst")]
        [SerializeField] private int _initialCount = 100;
        [Tooltip("Dev-only: press this key to spawn another burst for stress testing.")]
        [SerializeField] private Key _spawnMoreKey = Key.E;
        [SerializeField] private int _spawnMoreAmount = 25;

        private void Start()
        {
            if (_enemyPrefab != null && PoolManager.Exists)
                PoolManager.Instance.Prewarm(_enemyPrefab, _prewarm);

            SpawnBurst(_initialCount);
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard[_spawnMoreKey].wasPressedThisFrame)
                SpawnBurst(_spawnMoreAmount);
        }

        public void SpawnBurst(int amount)
        {
            if (_enemyPrefab == null || !PoolManager.Exists) return;

            Vector3 origin = _center != null ? _center.position : transform.position;
            for (int i = 0; i < amount; i++)
            {
                Vector2 disc = Random.insideUnitCircle.normalized * Random.Range(_minRadius, _maxRadius);
                Vector3 pos = origin + new Vector3(disc.x, 0f, disc.y);
                PoolManager.Instance.Get(_enemyPrefab, pos, Quaternion.identity);
            }
        }
    }
}
