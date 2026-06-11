namespace Biofall.Combat
{
    /// <summary>Anything that can take damage. Implemented by player and enemies via shared components.</summary>
    public interface IDamageable
    {
        bool IsAlive { get; }
        void ApplyDamage(in DamageInfo damageInfo);
    }
}
