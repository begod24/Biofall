using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Biofall.Enemies;

namespace Biofall.Spawning
{
    public class WaveDirector : MonoBehaviour
    {
        [SerializeField] private List<WaveDefinition> waves = new();
        [SerializeField] private List<EnemySpawner> spawners = new();
        [SerializeField] private Transform playerTarget;
        [SerializeField] private float timeBetweenWaves = 5f;
        [SerializeField] private bool autoStart = true;
        [SerializeField] private Transform poolParent;

        private readonly Dictionary<EnemyBase, Stack<EnemyBase>> pools = new();
        private readonly Dictionary<EnemyBase, EnemyBase> instanceToPrefab = new();
        private int currentWaveIndex = -1;
        private int aliveCount;

        public int CurrentWave => currentWaveIndex + 1;
        public int TotalWaves => waves.Count;
        public int AliveCount => aliveCount;

        public event System.Action<int> OnWaveStarted;
        public event System.Action<int> OnWaveCompleted;
        public event System.Action OnAllWavesCompleted;

        private void Start()
        {
            if (poolParent == null) poolParent = transform;
            if (autoStart) StartCoroutine(RunWaves());
        }

        public void StartWaves()
        {
            StopAllCoroutines();
            StartCoroutine(RunWaves());
        }

        private IEnumerator RunWaves()
        {
            for (int i = 0; i < waves.Count; i++)
            {
                currentWaveIndex = i;
                var wave = waves[i];
                yield return new WaitForSeconds(wave.startDelay);
                OnWaveStarted?.Invoke(i + 1);
                yield return SpawnWave(wave);
                while (aliveCount > 0) yield return null;
                OnWaveCompleted?.Invoke(i + 1);
                yield return new WaitForSeconds(timeBetweenWaves);
            }
            OnAllWavesCompleted?.Invoke();
        }

        private IEnumerator SpawnWave(WaveDefinition wave)
        {
            if (spawners.Count == 0) yield break;
            foreach (var entry in wave.entries)
            {
                if (entry.enemyPrefab == null) continue;
                for (int i = 0; i < entry.count; i++)
                {
                    SpawnOne(entry.enemyPrefab);
                    yield return new WaitForSeconds(entry.spawnInterval);
                }
            }
        }

        private void SpawnOne(EnemyBase prefab)
        {
            var spawner = spawners[Random.Range(0, spawners.Count)];
            EnemyBase inst = Acquire(prefab);
            inst.transform.SetPositionAndRotation(spawner.GetSpawnPoint(), Quaternion.identity);
            inst.gameObject.SetActive(true);
            inst.Initialize(playerTarget);
            inst.OnDespawned += HandleEnemyDespawned;
            instanceToPrefab[inst] = prefab;
            aliveCount++;
        }

        private EnemyBase Acquire(EnemyBase prefab)
        {
            if (!pools.TryGetValue(prefab, out var stack))
            {
                stack = new Stack<EnemyBase>();
                pools[prefab] = stack;
            }
            return stack.Count > 0 ? stack.Pop() : Instantiate(prefab, poolParent);
        }

        private void HandleEnemyDespawned(EnemyBase enemy)
        {
            enemy.OnDespawned -= HandleEnemyDespawned;
            aliveCount = Mathf.Max(0, aliveCount - 1);
            if (instanceToPrefab.TryGetValue(enemy, out var prefab))
            {
                instanceToPrefab.Remove(enemy);
                if (!pools.TryGetValue(prefab, out var stack))
                {
                    stack = new Stack<EnemyBase>();
                    pools[prefab] = stack;
                }
                enemy.transform.SetParent(poolParent);
                stack.Push(enemy);
            }
        }
    }
}
