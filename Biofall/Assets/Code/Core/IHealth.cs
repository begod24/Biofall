namespace Biofall.Core
{
    public interface IHealth
    {
        float Current { get; }
        float Max { get; }
        bool IsAlive { get; }

        void Heal(float amount);
        void SetMax(float max, bool refill);
    }
}
