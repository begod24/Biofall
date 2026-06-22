using System.Collections.Generic;
using UnityEngine;
using Biofall.Core;

namespace Biofall.Gameplay
{
    public sealed class BloodPoolService : MonoBehaviour
    {
        [SerializeField] private GameObject bloodPoolPrefab;
        [Tooltip("Max blood decals alive at once. Oldest is recycled past this — keeps it cheap.")]
        [SerializeField] private int maxPools = 40;
        [Tooltip("Lift above the ground to avoid z-fighting with the floor.")]
        [SerializeField] private float groundOffset = 0.02f;
        [Tooltip("Layers treated as ground for snapping the decal down. None = use the death position's Y.")]
        [SerializeField] private LayerMask groundMask = 0;
        [Range(0f, 1f)]
        [SerializeField] private float spawnChance = 1f;

        private readonly Queue<GameObject> _active = new Queue<GameObject>();

        private void OnEnable() => EventBus.Subscribe<TargetDied>(OnTargetDied);

        private void OnDisable()
        {
            EventBus.Unsubscribe<TargetDied>(OnTargetDied);
            _active.Clear();
        }

        private void OnTargetDied(TargetDied e)
        {
            if (bloodPoolPrefab == null || e.Target == null || PoolService.Instance == null) return;
            if (spawnChance < 1f && Random.value > spawnChance) return;

            Vector3 pos = e.Target.transform.position;
            if (groundMask.value != 0 &&
                Physics.Raycast(pos + Vector3.up * 0.6f, Vector3.down, out RaycastHit hit, 3f, groundMask))
                pos = hit.point;
            pos.y += groundOffset;

            Quaternion rot = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            GameObject pool = PoolService.Instance.Spawn(bloodPoolPrefab, pos, rot);
            if (pool == null) return;
            _active.Enqueue(pool);

            while (_active.Count > maxPools)
            {
                var oldest = _active.Dequeue();
                if (oldest != null && oldest.activeInHierarchy)
                    PoolService.Instance.Despawn(oldest);
            }
        }
    }
}
