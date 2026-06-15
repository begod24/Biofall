using System.Collections.Generic;
using UnityEngine;
using Biofall.Core;

namespace Biofall.Gameplay
{
    /// <summary>
    /// Central tick + pooled spawning for all zombies. One Update loop calls Tick on every active
    /// enemy instead of 150 separate MonoBehaviour.Update calls — the main scalability win. Also the
    /// single owner of the enemy prefab. Enemies register on spawn / unregister on despawn.
    /// </summary>
    public sealed class EnemyManager : MonoBehaviour
    {
        public static EnemyManager Instance { get; private set; }

        [SerializeField] private GameObject enemyPrefab;

        private readonly List<Enemy> _enemies = new(256);

        // Reusable buffers for the boids separation pass (no per-frame allocation).
        private Vector3[] _positions = new Vector3[256];
        private bool[] _alive = new bool[256];

        public int ActiveCount => _enemies.Count;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            // Zombies live on the Enemy layer and move by transform; stop their colliders from
            // physically shoving the player / ground (they attack at range). Bullets still hit
            // them because the weapon raycast uses an explicit layer mask, not this matrix.
            int enemyLayer = LayerMask.NameToLayer("Enemy");
            if (enemyLayer >= 0)
            {
                Physics.IgnoreLayerCollision(enemyLayer, 0, true);          // 0 = Default (player + ground)
                Physics.IgnoreLayerCollision(enemyLayer, enemyLayer, true); // zombies don't shove each other
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Register(Enemy enemy)
        {
            if (enemy != null && !_enemies.Contains(enemy)) _enemies.Add(enemy);
        }

        public void Unregister(Enemy enemy)
        {
            _enemies.Remove(enemy);
        }

        public Enemy Spawn(Vector3 position, Quaternion rotation)
        {
            if (enemyPrefab == null || PoolService.Instance == null) return null;
            GameObject go = PoolService.Instance.Spawn(enemyPrefab, position, rotation);
            return go != null ? go.GetComponent<Enemy>() : null;
        }

        private void Update()
        {
            float dt = Time.deltaTime;

            // Drop any destroyed entries first.
            for (int i = _enemies.Count - 1; i >= 0; i--)
                if (_enemies[i] == null) _enemies.RemoveAt(i);

            int count = _enemies.Count;
            if (count == 0) return;

            if (_positions.Length < count)
            {
                int cap = Mathf.NextPowerOfTwo(count);
                _positions = new Vector3[cap];
                _alive = new bool[cap];
            }

            // Snapshot positions/alive once so separation reads a consistent frame.
            for (int i = 0; i < count; i++)
            {
                _positions[i] = _enemies[i].Position;
                _alive[i] = !_enemies[i].Dead;
            }

            // Boids separation (O(n²) — fine for ~150; swap to a spatial grid if it grows much larger),
            // then tick. Each zombie is pushed away from living neighbours inside its radius.
            for (int i = 0; i < count; i++)
            {
                Enemy enemy = _enemies[i];
                Vector3 separation = Vector3.zero;

                if (_alive[i])
                {
                    float r = enemy.SeparationRadius;
                    float r2 = r * r;
                    Vector3 pi = _positions[i];

                    for (int j = 0; j < count; j++)
                    {
                        if (j == i || !_alive[j]) continue;
                        Vector3 d = pi - _positions[j];
                        d.y = 0f;
                        float sq = d.sqrMagnitude;
                        if (sq > 0.0001f && sq < r2)
                        {
                            float dist = Mathf.Sqrt(sq);
                            separation += d / dist * (1f - dist / r); // closer neighbour = stronger push
                        }
                    }

                    if (separation.sqrMagnitude > 1f) separation.Normalize();
                }

                enemy.Tick(dt, separation);
            }
        }
    }
}
