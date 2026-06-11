using System.Collections.Generic;
using Biofall.Utilities;
using UnityEngine;

namespace Biofall.Pooling
{
    /// <summary>
    /// Prefab-keyed object pool. The single source of spawn/despawn for projectiles,
    /// VFX, decals, pickups, and enemies — so combat never calls Instantiate/Destroy
    /// during play (critical for the 100+ enemy swarm).
    ///
    /// Usage:
    ///   var go = PoolManager.Instance.Get(prefab, pos, rot);
    ///   PoolManager.Instance.Release(go);
    /// </summary>
    public class PoolManager : MonoSingleton<PoolManager>
    {
        private readonly Dictionary<GameObject, Stack<GameObject>> _available = new();
        // Maps a live instance back to the prefab it came from, so Release knows its pool.
        private readonly Dictionary<GameObject, GameObject> _instanceToPrefab = new();

        private Transform _root;

        protected override void OnSingletonAwake()
        {
            _root = transform;
        }

        /// <summary>Pre-create <paramref name="count"/> inactive instances to avoid first-use hitches.</summary>
        public void Prewarm(GameObject prefab, int count)
        {
            if (prefab == null) return;
            var stack = GetStack(prefab);
            for (int i = 0; i < count; i++)
            {
                var go = CreateInstance(prefab);
                go.SetActive(false);
                stack.Push(go);
            }
        }

        public GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            if (prefab == null) return null;

            var stack = GetStack(prefab);
            GameObject go = stack.Count > 0 ? stack.Pop() : CreateInstance(prefab);

            var t = go.transform;
            t.SetParent(parent, false);
            t.SetPositionAndRotation(position, rotation);
            go.SetActive(true);

            if (go.TryGetComponent(out IPoolable poolable)) poolable.OnSpawned();
            return go;
        }

        public T Get<T>(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null) where T : Component
        {
            var go = Get(prefab, position, rotation, parent);
            return go != null ? go.GetComponent<T>() : null;
        }

        public void Release(GameObject instance)
        {
            if (instance == null) return;

            if (!_instanceToPrefab.TryGetValue(instance, out var prefab))
            {
                // Not pooled (e.g. scene object) — just disable defensively.
                instance.SetActive(false);
                return;
            }

            if (instance.TryGetComponent(out IPoolable poolable)) poolable.OnDespawned();

            instance.SetActive(false);
            instance.transform.SetParent(_root, false);
            GetStack(prefab).Push(instance);
        }

        private Stack<GameObject> GetStack(GameObject prefab)
        {
            if (!_available.TryGetValue(prefab, out var stack))
            {
                stack = new Stack<GameObject>();
                _available.Add(prefab, stack);
            }
            return stack;
        }

        private GameObject CreateInstance(GameObject prefab)
        {
            var go = Instantiate(prefab, _root);
            _instanceToPrefab[go] = prefab;
            return go;
        }
    }
}
