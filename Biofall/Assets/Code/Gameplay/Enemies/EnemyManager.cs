using System.Collections.Generic;
using UnityEngine;
using Biofall.Core;

namespace Biofall.Gameplay
{
    public sealed class EnemyManager : MonoBehaviour
    {
        public static EnemyManager Instance { get; private set; }

        [SerializeField] private GameObject enemyPrefab;

        private readonly List<Enemy> _enemies = new(256);

        private Vector3[] _positions = new Vector3[256];
        private bool[] _alive = new bool[256];
        private bool[] _aggro = new bool[256];

        public int ActiveCount => _enemies.Count;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            int enemyLayer = LayerMask.NameToLayer("Enemy");
            if (enemyLayer >= 0)
            {
                Physics.IgnoreLayerCollision(enemyLayer, 0, true);
                Physics.IgnoreLayerCollision(enemyLayer, enemyLayer, true);
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

        public Enemy Spawn(Vector3 position, Quaternion rotation) => Spawn(enemyPrefab, position, rotation);

        public Enemy Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            if (prefab == null || PoolService.Instance == null) return null;
            GameObject go = PoolService.Instance.Spawn(prefab, position, rotation);
            return go != null ? go.GetComponent<Enemy>() : null;
        }

        private void Update()
        {
            float dt = Time.deltaTime;

            for (int i = _enemies.Count - 1; i >= 0; i--)
                if (_enemies[i] == null) _enemies.RemoveAt(i);

            int count = _enemies.Count;
            if (count == 0) return;

            if (_positions.Length < count)
            {
                int cap = Mathf.NextPowerOfTwo(count);
                _positions = new Vector3[cap];
                _alive = new bool[cap];
                _aggro = new bool[cap];
            }

            for (int i = 0; i < count; i++)
            {
                _positions[i] = _enemies[i].Position;
                _alive[i] = !_enemies[i].Dead;
                _aggro[i] = _enemies[i].Aggroed;
            }

            for (int i = 0; i < count; i++)
            {
                Enemy enemy = _enemies[i];
                Vector3 separation = Vector3.zero;

                if (_alive[i])
                {
                    float r = enemy.SeparationRadius;
                    float r2 = r * r;
                    Vector3 pi = _positions[i];

                    bool spreadAggro = !_aggro[i];
                    float ar = enemy.AggroRadius;
                    float ar2 = ar * ar;
                    bool caughtAggro = false;

                    for (int j = 0; j < count; j++)
                    {
                        if (j == i || !_alive[j]) continue;
                        Vector3 d = pi - _positions[j];
                        d.y = 0f;
                        float sq = d.sqrMagnitude;

                        if (sq > 0.0001f && sq < r2)
                        {
                            float dist = Mathf.Sqrt(sq);
                            separation += d / dist * (1f - dist / r);
                        }

                        if (spreadAggro && _aggro[j] && sq < ar2) caughtAggro = true;
                    }

                    if (separation.sqrMagnitude > 1f) separation.Normalize();
                    if (caughtAggro) enemy.Aggro();
                }

                enemy.Tick(dt, separation);
            }
        }
    }
}
