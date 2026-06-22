using Unity.Netcode;
using UnityEngine;
using Biofall.Core;
using Biofall.Gameplay;

namespace Biofall.Net
{
    /// <summary>
    /// CO-OP server-only loot dropper. Mirrors the solo <see cref="LootService"/> roll logic but, on
    /// the SERVER only, spawns the NETWORKED pickup variant of each entry (<see cref="LootEntry.coopPrefab"/>)
    /// through NGO so every client sees and can collect the drop. Collection + per-player crediting is
    /// handled by <see cref="CoopPickup"/>. The object lives in the co-op scene on all peers but is inert
    /// off the server; the solo <see cref="LootService"/> self-disables in co-op so the two never both fire.
    /// Shares the same <see cref="LootConfig"/> asset as solo — one balance source, just a second prefab ref.
    /// </summary>
    public sealed class CoopLootService : MonoBehaviour
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
            if (!NetSession.IsServer) return;
            if (config == null || config.entries == null || e.Target == null) return;
            if (NetworkManager.Singleton == null) return;

            // The enemy archetype that just died (its EnemyData asset) keys type-specific drops.
            var enemy = e.Target.GetComponent<Enemy>();
            EnemyData data = enemy != null ? enemy.Data : null;

            Vector3 origin = e.Target.transform.position + Vector3.up * dropHeight;

            foreach (var entry in config.entries)
            {
                if (entry == null || entry.coopPrefab == null) continue;     // no networked variant → skip in co-op
                if (entry.onlyFor != null && entry.onlyFor != data) continue; // type-specific entry, wrong type
                if (Random.value >= entry.chance) continue;

                int n = Mathf.Max(1, Random.Range(entry.minCount, entry.maxCount + 1));
                for (int i = 0; i < n; i++)
                {
                    Vector3 off = new Vector3(Random.Range(-scatter, scatter), 0f, Random.Range(-scatter, scatter));
                    GameObject go = Instantiate(entry.coopPrefab, origin + off, Quaternion.identity);
                    var no = go.GetComponent<NetworkObject>();
                    if (no == null) { Destroy(go); continue; }
                    no.Spawn(true); // server-spawn → replicate to all clients
                }
            }
        }
    }
}
