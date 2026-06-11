using System;
using System.Collections.Generic;

namespace Biofall.Pooling
{
    /// <summary>
    /// Lightweight generic pool for plain C# objects (e.g. reusable lists in the
    /// spatial grid) to avoid per-frame GC allocations. For Unity prefabs use
    /// <see cref="PoolManager"/> instead.
    /// </summary>
    public sealed class ObjectPool<T> where T : class
    {
        private readonly Stack<T> _items = new();
        private readonly Func<T> _create;
        private readonly Action<T> _onGet;
        private readonly Action<T> _onRelease;

        public ObjectPool(Func<T> create, Action<T> onGet = null, Action<T> onRelease = null, int prewarm = 0)
        {
            _create = create ?? throw new ArgumentNullException(nameof(create));
            _onGet = onGet;
            _onRelease = onRelease;
            for (int i = 0; i < prewarm; i++) _items.Push(_create());
        }

        public T Get()
        {
            T item = _items.Count > 0 ? _items.Pop() : _create();
            _onGet?.Invoke(item);
            return item;
        }

        public void Release(T item)
        {
            if (item == null) return;
            _onRelease?.Invoke(item);
            _items.Push(item);
        }
    }
}
