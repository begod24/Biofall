using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using Biofall.Core;
using Biofall.Gameplay;

namespace Biofall.Net
{
    public sealed class CoopEnemySpawner : MonoBehaviour
    {
        [System.Serializable]
        public struct WeightedEnemy
        {
            public GameObject prefab;
            [Min(0f)] public float weight;
        }

        [Tooltip("Networked enemy prefab (NetworkObject + CoopEnemy). Used when 'Enemy Mix' is empty (legacy single-type spawn).")]
        [SerializeField] private GameObject coopEnemyPrefab;
        [Tooltip("Weighted roster: zombie common, tough types rarer. Empty = spawn only 'coopEnemyPrefab'. " +
                 "All prefabs must be registered in NetworkRoot's NetworkPrefabs.")]
        [SerializeField] private WeightedEnemy[] enemyMix;
        [SerializeField] private int count = 12;
        [SerializeField] private float minRadius = 16f;
        [SerializeField] private float maxRadius = 28f;
        [SerializeField] private float spawnInterval = 0.1f;
        [SerializeField] private bool spawnOnStart = true;
        [Tooltip("Seconds between auto top-up waves (0 = a single batch on start).")]
        [SerializeField] private float waveInterval = 12f;
        [Tooltip("Stop topping up above this many alive (0 = unlimited).")]
        [SerializeField] private int maxAlive = 40;
        [SerializeField] private float viewportMargin = 0.08f;
        [SerializeField] private int placementTries = 14;

        private const float SnapRadius = 4f;

        private Camera _camera;
        private NavMeshPath _path;
        private bool _started;

        private void Update()
        {
            if (_started || !NetSession.IsServer) return;
            _started = true;
            if (spawnOnStart) StartCoroutine(Run());
        }

        public void SpawnWaveNow()
        {
            if (NetSession.IsServer) StartCoroutine(SpawnBatch());
        }

        private IEnumerator Run()
        {
            _path ??= new NavMeshPath();
            yield return null;

            do
            {
                yield return SpawnBatch();
                if (waveInterval > 0f) yield return new WaitForSeconds(waveInterval);
            }
            while (waveInterval > 0f && NetSession.IsServer);
        }

        private IEnumerator SpawnBatch()
        {
            _camera = Camera.main;
            _path ??= new NavMeshPath();
            var wait = spawnInterval > 0f ? new WaitForSeconds(spawnInterval) : null;

            for (int i = 0; i < count; i++)
            {
                if (maxAlive > 0 && AliveCount() >= maxAlive) yield break;
                if (TrySpawn() && wait != null) yield return wait;
            }
        }

        private static int AliveCount() =>
            EnemyManager.Instance != null ? EnemyManager.Instance.ActiveCount : 0;

        private bool TrySpawn()
        {
            GameObject prefab = PickPrefab();
            if (prefab == null || NetworkManager.Singleton == null) return false;

            Vector3 center = PlayerRegistry.HasPlayer ? PlayerRegistry.Player.position : transform.position;
            if (!TryFindSpawnPoint(center, out Vector3 pos)) return false;

            GameObject go = Instantiate(prefab, pos, Quaternion.identity);
            var no = go.GetComponent<NetworkObject>();
            if (no == null) { Destroy(go); return false; }
            no.Spawn(true);
            return true;
        }

        private GameObject PickPrefab()
        {
            if (enemyMix == null || enemyMix.Length == 0) return coopEnemyPrefab;

            float total = 0f;
            for (int i = 0; i < enemyMix.Length; i++)
                if (enemyMix[i].prefab != null && enemyMix[i].weight > 0f) total += enemyMix[i].weight;

            if (total <= 0f) return coopEnemyPrefab;

            float roll = Random.value * total;
            for (int i = 0; i < enemyMix.Length; i++)
            {
                if (enemyMix[i].prefab == null || enemyMix[i].weight <= 0f) continue;
                roll -= enemyMix[i].weight;
                if (roll <= 0f) return enemyMix[i].prefab;
            }
            return coopEnemyPrefab;
        }

        private bool TryFindSpawnPoint(Vector3 center, out Vector3 result)
        {
            bool hasFallback = false;
            Vector3 fallback = center;

            for (int t = 0; t < placementTries; t++)
            {
                float angle = Random.value * Mathf.PI * 2f;
                float radius = Random.Range(minRadius, maxRadius);
                Vector3 ring = center + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;

                if (!NavMesh.SamplePosition(ring, out NavMeshHit hit, SnapRadius, NavMesh.AllAreas))
                    continue;
                if (!NavMesh.CalculatePath(hit.position, center, NavMesh.AllAreas, _path) ||
                    _path.status != NavMeshPathStatus.PathComplete)
                    continue;

                if (!IsVisible(hit.position))
                {
                    result = hit.position;
                    return true;
                }
                fallback = hit.position;
                hasFallback = true;
            }

            result = fallback;
            return hasFallback;
        }

        private bool IsVisible(Vector3 worldPos)
        {
            if (_camera == null) return false;
            Vector3 vp = _camera.WorldToViewportPoint(worldPos);
            return vp.z > 0f
                && vp.x > -viewportMargin && vp.x < 1f + viewportMargin
                && vp.y > -viewportMargin && vp.y < 1f + viewportMargin;
        }
    }
}
