using UnityEngine;

namespace Biofall.Utilities
{
    /// <summary>
    /// Base class for the handful of stable global services that are allowed to be
    /// singletons (GameManager, PoolManager, AudioManager, etc.). Gameplay entities
    /// such as Player, Enemy, and Weapon must NOT use this.
    /// </summary>
    public abstract class MonoSingleton<T> : MonoBehaviour where T : MonoSingleton<T>
    {
        private static T _instance;
        private static bool _isQuitting;

        public static T Instance
        {
            get
            {
                if (_isQuitting) return null;
                if (_instance == null)
                {
                    _instance = FindAnyObjectByType<T>();
                }
                return _instance;
            }
        }

        public static bool Exists => _instance != null && !_isQuitting;

        protected virtual void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = (T)this;
            OnSingletonAwake();
        }

        /// <summary>Override instead of Awake so the singleton guard always runs first.</summary>
        protected virtual void OnSingletonAwake() { }

        protected virtual void OnApplicationQuit() => _isQuitting = true;

        protected virtual void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }
    }
}
