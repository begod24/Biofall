using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using Biofall.Core;

namespace Biofall.Gameplay
{
    /// <summary>
    /// Sandbox wave director. Spawns discrete waves through the existing pooled <see cref="EnemyManager"/>
    /// (no changes to core enemy code): each wave is bigger than the last, screamers join from a later
    /// wave on, and the next wave only starts once the current one is fully cleared (after a short break).
    /// The number of zombies alive at once is capped at <see cref="maxConcurrent"/> (up to ~100). Spawns
    /// off-screen around the player and snaps points to the NavMesh. Exposes <see cref="CurrentWave"/> +
    /// <see cref="WaveStarted"/> for the HUD.
    /// </summary>
    public sealed class WaveSpawner : MonoBehaviour
    {
        [Header("Prefabs")]
        [Tooltip("Variant prefabs (basic zombies use the EnemyManager's default prefab). Leave any " +
                 "empty to disable that type.")]
        [SerializeField] private GameObject screamerPrefab;
        [SerializeField] private GameObject runnerPrefab;
        [SerializeField] private GameObject tankPrefab;

        [Header("Waves")]
        [SerializeField] private int baseZombies = 8;
        [SerializeField] private int zombieGrowth = 4;        // +per wave
        [Tooltip("Max enemies alive at once (hard cap — spawning throttles to keep under this).")]
        [SerializeField] private int maxConcurrent = 100;
        [SerializeField] private int screamerStartWave = 3;
        [SerializeField] private int maxScreamers = 12;
        [Tooltip("Seconds of calm after a wave is cleared before the next begins.")]
        [SerializeField] private float waveBreak = 5f;
        [Tooltip("Sandbox mode: enemies lock onto and hunt the player straight from spawn instead of " +
                 "wandering until the player gets close.")]
        [SerializeField] private bool chaseFromSpawn = true;

        [Header("Runners (fast, fragile)")]
        [SerializeField] private int runnerStartWave = 2;
        [Tooltip("Runners added per wave once they start appearing.")]
        [SerializeField] private int runnerGrowth = 2;
        [SerializeField] private int maxRunners = 30;

        [Header("Tanks (slow, tough)")]
        [SerializeField] private int tankStartWave = 4;
        [SerializeField] private int maxTanks = 6;

        [Header("Spawn placement")]
        [SerializeField] private float minRadius = 16f;
        [SerializeField] private float maxRadius = 28f;
        [SerializeField] private float spawnInterval = 0.15f;
        [SerializeField] private float viewportMargin = 0.08f;
        [SerializeField] private int placementTries = 12;

        /// <summary>Current wave number (1-based); 0 before the first wave.</summary>
        public static int CurrentWave { get; private set; }
        /// <summary>Fired when a new wave begins, with the wave number.</summary>
        public static event System.Action<int> WaveStarted;

        private Camera _camera;
        private NavMeshPath _path;

        private const float SnapRadius = 4f;

        private void OnEnable() => StartCoroutine(Run());
        private void OnDisable() => StopAllCoroutines();

        private IEnumerator Run()
        {
            CurrentWave = 0;
            yield return null;                 // let PlayerRegistry / EnemyManager initialise
            _camera = Camera.main;
            _path ??= new NavMeshPath();
            var wait = new WaitForSeconds(spawnInterval);

            while (true)
            {
                CurrentWave++;
                WaveStarted?.Invoke(CurrentWave);

                // Per-type counts for this wave (a variant is skipped if its prefab is missing).
                int remZ = baseZombies + (CurrentWave - 1) * zombieGrowth;
                int remS = (screamerPrefab != null && CurrentWave >= screamerStartWave)
                    ? Mathf.Min(CurrentWave - screamerStartWave + 1, maxScreamers) : 0;
                int remR = (runnerPrefab != null && CurrentWave >= runnerStartWave)
                    ? Mathf.Min((CurrentWave - runnerStartWave + 1) * runnerGrowth, maxRunners) : 0;
                int remT = (tankPrefab != null && CurrentWave >= tankStartWave)
                    ? Mathf.Min(CurrentWave - tankStartWave + 1, maxTanks) : 0;

                while (remZ + remS + remR + remT > 0)
                {
                    var mgr = EnemyManager.Instance;
                    if (mgr != null && mgr.ActiveCount < maxConcurrent)
                    {
                        Vector3 center = PlayerRegistry.HasPlayer ? PlayerRegistry.Player.position : transform.position;
                        if (!TryFindSpawnPoint(center, out Vector3 pos))
                        {
                            yield return wait;
                            continue;
                        }

                        // Pick a remaining type at random, weighted by how many are left.
                        int total = remZ + remS + remR + remT;
                        int r = Random.Range(0, total);
                        Enemy spawned;
                        if (r < remZ) { spawned = mgr.Spawn(pos, Quaternion.identity); remZ--; }
                        else if (r < remZ + remR) { spawned = mgr.Spawn(runnerPrefab, pos, Quaternion.identity); remR--; }
                        else if (r < remZ + remR + remT) { spawned = mgr.Spawn(tankPrefab, pos, Quaternion.identity); remT--; }
                        else { spawned = mgr.Spawn(screamerPrefab, pos, Quaternion.identity); remS--; }

                        // Sandbox: hunt the player from spawn instead of wandering.
                        if (chaseFromSpawn && spawned != null) spawned.Aggro();
                    }
                    yield return wait;
                }

                // Wait until everything from this wave is gone, then take a breather.
                while (EnemyManager.Instance != null && EnemyManager.Instance.ActiveCount > 0)
                    yield return null;

                if (waveBreak > 0f) yield return new WaitForSeconds(waveBreak);
            }
        }

        // ---- placement (mirrors EnemySpawner so core code is untouched) ----

        private Vector3 PickOffscreenPoint(Vector3 center)
        {
            Vector3 pos = center;
            for (int t = 0; t < placementTries; t++)
            {
                float angle = Random.value * Mathf.PI * 2f;
                float radius = Random.Range(minRadius, maxRadius);
                pos = center + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;
                if (!IsVisible(pos)) return pos;
            }
            return pos;
        }

        private bool TryFindSpawnPoint(Vector3 center, out Vector3 result)
        {
            if (!NavMesh.SamplePosition(center, out NavMeshHit centerHit, SnapRadius, NavMesh.AllAreas))
            {
                result = center;
                return false;
            }

            center = centerHit.position;
            bool hasFallback = false;
            Vector3 fallback = center;

            for (int t = 0; t < placementTries; t++)
            {
                Vector3 ring = PickOffscreenPoint(center);
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
