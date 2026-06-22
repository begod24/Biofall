using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using Biofall.Core;

namespace Biofall.Gameplay
{
    /// <summary>
    /// Spawns zombies through <see cref="EnemyManager"/> (pooled) at points around the player that
    /// are off-screen AND on the NavMesh AND have a valid path to the player — so the horde appears
    /// off-screen and actually walks in (it can never spawn outside the level / behind a wall where
    /// it would get stuck). Spawned enemies aggro immediately so they head for the player.
    /// </summary>
    public sealed class EnemySpawner : MonoBehaviour
    {
        [SerializeField] private int count = 12;
        [Tooltip("Spawn ring distance from the player (random within the range).")]
        [SerializeField] private float minRadius = 16f;
        [SerializeField] private float maxRadius = 28f;
        [SerializeField] private float spawnInterval = 0.05f;
        [SerializeField] private bool spawnOnStart = true;
        [Tooltip("Viewport margin: a point is treated as visible a bit beyond the screen edges.")]
        [SerializeField] private float viewportMargin = 0.08f;
        [SerializeField] private int placementTries = 14;
        [Tooltip("Aggro spawned enemies immediately so they march toward the player.")]
        [SerializeField] private bool aggroOnSpawn = true;

        [Header("Screamer variant")]
        [Tooltip("Screamer prefab — leave empty to spawn none.")]
        [SerializeField] private GameObject screamerPrefab;
        [Tooltip("A random count in [min,max] is spawned off-screen alongside the zombies.")]
        [SerializeField] private int screamerMin = 2;
        [SerializeField] private int screamerMax = 5;

        // How far a chosen ring point may be snapped onto the NavMesh. Small, so points that land
        // well outside the walls (no mesh nearby) are rejected instead of dragged in.
        private const float SnapRadius = 4f;

        private Camera _camera;
        private NavMeshPath _path;

        private void Start()
        {
            if (spawnOnStart) StartCoroutine(SpawnBatch());
        }

        public void SpawnNow() => StartCoroutine(SpawnBatch());

        private IEnumerator SpawnBatch()
        {
            yield return null; // let PlayerRegistry / EnemyManager / NavMesh initialise

            _camera = Camera.main;
            _path ??= new NavMeshPath();
            var wait = new WaitForSeconds(spawnInterval);

            for (int i = 0; i < count; i++)
            {
                if (TrySpawn(null)) { if (spawnInterval > 0f) yield return wait; }
            }

            // A handful of Screamers mixed into the horde.
            if (screamerPrefab != null)
            {
                int screamers = Random.Range(screamerMin, screamerMax + 1);
                for (int i = 0; i < screamers; i++)
                    if (TrySpawn(screamerPrefab) && spawnInterval > 0f) yield return wait;
            }
        }

        /// <summary>Find a valid reachable point and spawn there. Returns false (and skips) if none found.</summary>
        private bool TrySpawn(GameObject prefab)
        {
            Vector3 center = PlayerRegistry.HasPlayer ? PlayerRegistry.Player.position : transform.position;
            if (!TryFindSpawnPoint(center, out Vector3 pos)) return false;

            Enemy e = prefab == null
                ? EnemyManager.Instance?.Spawn(pos, Quaternion.identity)
                : EnemyManager.Instance?.Spawn(prefab, pos, Quaternion.identity);

            if (aggroOnSpawn && e != null) e.Aggro();
            return e != null;
        }

        /// <summary>
        /// A point on the NavMesh, around the player, that the enemy can actually path to. Prefers
        /// off-screen; falls back to an on-screen reachable point rather than spawning off-mesh.
        /// </summary>
        private bool TryFindSpawnPoint(Vector3 center, out Vector3 result)
        {
            bool hasFallback = false;
            Vector3 fallback = center;

            for (int t = 0; t < placementTries; t++)
            {
                float angle = Random.value * Mathf.PI * 2f;
                float radius = Random.Range(minRadius, maxRadius);
                Vector3 ring = center + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;

                // Must be on the NavMesh near the chosen point...
                if (!NavMesh.SamplePosition(ring, out NavMeshHit hit, SnapRadius, NavMesh.AllAreas))
                    continue;
                // ...and actually able to reach the player (rejects disconnected outside-wall ground).
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
