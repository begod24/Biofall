namespace Biofall.Core
{
    /// <summary>
    /// Encapsulation contract for a health pool. Hides how HP is stored/changed;
    /// exposes only what other systems need to read and the operations they may request.
    /// </summary>
    public interface IHealth
    {
        float Current { get; }
        float Max { get; }
        bool IsAlive { get; }

        void Heal(float amount);
        void SetMax(float max, bool refill);
    }
}
