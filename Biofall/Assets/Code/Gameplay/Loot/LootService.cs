using UnityEngine;
using Biofall.Core;
using Biofall.Net;

namespace Biofall.Gameplay
{
    public sealed class LootService : MonoBehaviour
    {
        [SerializeField] private LootConfig config;
        [Tooltip("Pickups land lifted off the ground a touch so they don't clip into it.")]
        [SerializeField] private float dropHeight = 0.3f;
        [Tooltip("Random horizontal scatter so multiple drops don't stack on one point.")]
        [SerializeField] private float scatter = 0.5f;
        [Tooltip("Turn OFF in WaveMode: enemies won't drop Bio Samples (entries marked 'isBioSample'). " +
                 "WaveMode is pure arcade — no currency farming, nothing is banked toward upgrades.")]
        [SerializeField] private bool dropBioSamples = true;

        private void OnEnable() => EventBus.Subscribe<TargetDied>(OnTargetDied);
        private void OnDisable() => EventBus.Unsubscribe<TargetDied>(OnTargetDied);

        private void OnTargetDied(TargetDied e)
        {
            if (NetSession.InCoop) return;
            if (config == null || config.entries == null || e.Target == null) return;
            if (PoolService.Instance == null) return;

            var enemy = e.Target.GetComponent<Enemy>();
            EnemyData data = enemy != null ? enemy.Data : null;

            Vector3 origin = e.Target.transform.position + Vector3.up * dropHeight;

            foreach (var entry in config.entries)
            {
                if (entry == null || entry.prefab == null) continue;
                if (!dropBioSamples && entry.isBioSample) continue;
                if (entry.onlyFor != null && entry.onlyFor != data) continue;
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
