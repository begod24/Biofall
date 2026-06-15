using System;
using System.Collections.Generic;

namespace Biofall.Core
{
    /// <summary>
    /// Observer + Singleton. Typed, allocation-free publish/subscribe so systems
    /// communicate through events instead of holding references to each other.
    /// Events are <c>struct</c>s (see <see cref="GameEvents"/>) — no garbage on publish.
    /// </summary>
    public static class EventBus
    {
        // One delegate per event type. Keyed by Type so Clear() can wipe everything
        // between play-mode sessions (important when Domain Reload is disabled).
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

        /// <summary>Drop all subscriptions. Call on bootstrap to stay safe across play sessions.</summary>
        public static void Clear() => Handlers.Clear();
    }
}
