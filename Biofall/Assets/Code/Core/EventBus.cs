using System;
using System.Collections.Generic;

namespace Biofall.Core
{
    public static class EventBus
    {
        private static readonly Dictionary<Type, Delegate> Handlers = new();

        public static void Subscribe<T>(Action<T> handler) where T : struct
        {
            if (handler == null) return;
            Handlers.TryGetValue(typeof(T), out var existing);
            Handlers[typeof(T)] = (Action<T>)existing + handler;
        }

        public static void Unsubscribe<T>(Action<T> handler) where T : struct
        {
            if (handler == null) return;
            if (!Handlers.TryGetValue(typeof(T), out var existing)) return;

            var updated = (Action<T>)existing - handler;
            if (updated == null) Handlers.Remove(typeof(T));
            else Handlers[typeof(T)] = updated;
        }

        public static void Publish<T>(in T evt) where T : struct
        {
            if (Handlers.TryGetValue(typeof(T), out var existing))
                ((Action<T>)existing)?.Invoke(evt);
        }

        public static void Clear() => Handlers.Clear();
    }
}
