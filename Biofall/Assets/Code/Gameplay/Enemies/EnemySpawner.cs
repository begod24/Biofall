using System.Collections;
using UnityEngine;
using Biofall.Core;

namespace Biofall.Gameplay
{
    /// <summary>
    /// Test driver: spawns zombies through <see cref="EnemyManager"/> (pooled) at random points
    /// around the player that are OUTSIDE the camera's view, so the horde appears off-screen and
    /// walks in — like Zombie Shooter. Raise <see cref="count"/> to stress-test toward 100–150.
    /// Not the final wave system.
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
        [SerializeField] private int placementTries = 10;

        private Camera _camera;

        private void Start()
        {
            if (spawnOnStart) StartCoroutine(SpawnBatch());
        }

        public void SpawnNow() => StartCoroutine(SpawnBatch());

        private IEnumerator SpawnBatch()
        {
            yield return null; // let PlayerRegistry / EnemyManager initialise

            _camera = Camera.main;
            var wait = new WaitForSeconds(spawnInterval);

            for (int i = 0; i < count; i++)
            {
                Vector3 center = PlayerRegistry.HasPlayer ? PlayerRegistry.Player.position : transform.position;
                EnemyManager.Instance?.Spawn(PickOffscreenPoint(center), Quaternion.identity);
                if (spawnInterval > 0f) yield return wait;
            }
        }

        /// <summary>A point around the player at [min,max] radius that isn't on screen (best-effort).</summary>
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
            return pos; // give up after a few tries — spawn anyway
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
