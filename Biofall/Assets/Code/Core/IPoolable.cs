namespace Biofall.Core
{
    public interface IPoolable
    {
        void OnSpawned();

        void OnDespawned();
    }
}
