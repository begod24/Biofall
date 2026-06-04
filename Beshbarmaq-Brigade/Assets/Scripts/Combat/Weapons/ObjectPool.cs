using UnityEngine;
using System.Collections.Generic;

namespace Biofall.Weapons
{
    public class ObjectPool<T> where T : Component
    {
        private readonly T prefab;
        private readonly Transform parent;
        private readonly Stack<T> available = new Stack<T>();

        public ObjectPool(T prefab, int prewarm = 0, Transform parent = null)
        {
            this.prefab = prefab;
            this.parent = parent;
            for (int i = 0; i < prewarm; i++) available.Push(CreateNew());
        }

        private T CreateNew()
        {
            T inst = Object.Instantiate(prefab, parent);
            inst.gameObject.SetActive(false);
            return inst;
        }

        public T Get()
        {
            T inst = available.Count > 0 ? available.Pop() : CreateNew();
            inst.gameObject.SetActive(true);
            return inst;
        }

        public void Return(T inst)
        {
            if (inst == null) return;
            inst.gameObject.SetActive(false);
            available.Push(inst);
        }
    }
}
