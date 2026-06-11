namespace Biofall.Pooling
{
    /// <summary>
    /// Optional hook for pooled objects to reset their state on reuse. The
    /// <see cref="PoolManager"/> calls these around Get/Release.
    /// </summary>
    public interface IPoolable
    {
        void OnSpawned();
        void OnDespawned();
    }
}
