namespace Biofall.Core
{
    /// <summary>
    /// Implemented by objects that live in the <see cref="PoolService"/>.
    /// Lets a pooled object reset its state on reuse instead of relying on a fresh Instantiate.
    /// </summary>
    public interface IPoolable
    {
        /// <summary>Called right after the object is taken from the pool and activated.</summary>
        void OnSpawned();

        /// <summary>Called right before the object is returned to the pool and deactivated.</summary>
        void OnDespawned();
    }
}
