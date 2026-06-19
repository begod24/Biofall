using UnityEngine;
using Biofall.Core;

namespace Biofall.Gameplay
{
    /// <summary>
    /// Centralised, event-driven loot dropper (Observer). Listens to <see cref="TargetDied"/> and rolls
    /// drops from a single <see cref="LootConfig"/> — enemies no longer carry any drop data, so balancing
    /// the whole game (and future campaign missions) happens in one asset. Spawns via the PoolService.
    /// </summary>
    public sealed class LootService : MonoBehaviour
    {
        [SerializeField] private LootConfig config;
        [Tooltip("Pickups land lifted off the ground a touch so they don't clip into it.")]
        [SerializeField] private float dropHeight = 0.3f;
        [Tooltip("Random horizontal scatter so multiple drops don't stack on one point.")]
        [SerializeField] private float scatter = 0.5f;

        private void OnEnable() => EventBus.Subscribe<TargetDied>(OnTargetDied);
        private void OnDisable() => EventBus.Unsubscribe<TargetDied>(OnTargetDied);

        private void OnTargetDied(TargetDied e)
        {
            if (config == null || config.entries == null || e.Target == null) return;
            if (PoolService.Instance == null) return;

            // The enemy archetype that just died (its EnemyData asset) keys type-specific drops.
            var enemy = e.Target.GetComponent<Enemy>();
            EnemyData data = enemy != null ? enemy.Data : null;

            Vector3 origin = e.Target.transform.position + Vector3.up * dropHeight;

            foreach (var entry in config.entries)
            {
                if (entry == null || entry.prefab == null) continue;
                if (entry.onlyFor != null && entry.onlyFor != data) continue; // type-specific entry, wrong type
                if (Random.value >= entry.chance) continue;

                int n = Mathf.Max(1, Random.Range(entry.minCount, entry.maxCount + 1));
                for (int i = 0; i < n; i++)
                {
                    Vector3 off = new Vector3(Random.Range(-scatter, scatter), 0f, Random.Range(-scatter, scatter));
                    PoolService.Instance.Spawn(entry.prefab, origin + off, Quaternion.identity);
                }
            }
        }
    }
}
