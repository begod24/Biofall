namespace Biofall.Core
{
    /// <summary>
    /// Polymorphism seam: anything that can receive damage implements this.
    /// Bullets, explosions, etc. talk to this interface, never to concrete classes.
    /// </summary>
    public interface IDamageable
    {
        /// <summary>Apply damage. Implementer decides how it affects health/state.</summary>
        void TakeDamage(in DamageInfo info);
    }

    /// <summary>Lightweight, alloc-free payload describing a single hit.</summary>
    public readonly struct DamageInfo
    {
        public readonly float Amount;
        public readonly UnityEngine.Vector3 HitPoint;
        public readonly UnityEngine.Vector3 HitDirection;
        public readonly UnityEngine.GameObject Source;

        public DamageInfo(float amount, UnityEngine.Vector3 hitPoint, UnityEngine.Vector3 hitDirection, UnityEngine.GameObject source)
        {
            Amount = amount;
            HitPoint = hitPoint;
            HitDirection = hitDirection;
            Source = source;
        }
    }
}
