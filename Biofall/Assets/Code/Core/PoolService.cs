using System.Collections.Generic;
using UnityEngine;

namespace Biofall.Core
{
    public sealed class PoolService : MonoBehaviour
    {
        public static PoolService Instance { get; private set; }

        private readonly Dictionary<GameObject, Queue<GameObject>> _pools = new();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Prewarm(GameObject prefab, int count)
        {
            if (prefab == null || count <= 0) return;
            var queue = GetQueue(prefab);
            for (int i = 0; i < count; i++)
            {
                var obj = CreateInstance(prefab);
                obj.SetActive(false);
                queue.Enqueue(obj);
            }
        }

        public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            if (prefab == null) return null;

            var queue = GetQueue(prefab);
            GameObject obj = queue.Count > 0 ? queue.Dequeue() : CreateInstance(prefab);

            obj.transform.SetPositionAndRotation(position, rotation);
            obj.SetActive(true);

            if (obj.TryGetComponent<IPoolable>(out var poolable)) poolable.OnSpawned();
            return obj;
        }

        public void Despawn(GameObject obj)
        {
            if (obj == null) return;

            if (obj.TryGetComponent<IPoolable>(out var poolable)) poolable.OnDespawned();
            obj.SetActive(false);

            if (obj.TryGetComponent<PooledObject>(out var tag) && tag.SourcePrefab != null)
            {
                GetQueue(tag.SourcePrefab).Enqueue(obj);
            }
            else
            {
                Destroy(obj);
            }
        }

        private Queue<GameObject> GetQueue(GameObject prefab)
        {
            if (!_pools.TryGetValue(prefab, out var queue))
            {
                queue = new Queue<GameObject>();
                _pools[prefab] = queue;
            }
            return queue;
        }

        private GameObject CreateInstance(GameObject prefab)
        {
            var obj = Instantiate(prefab, transform);
            var tag = obj.GetComponent<PooledObject>();
            if (tag == null) tag = obj.AddComponent<PooledObject>();
            tag.SourcePrefab = prefab;
            return obj;
        }
    }
}
