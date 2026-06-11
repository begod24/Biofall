using UnityEngine;

namespace Biofall.Combat
{
    /// <summary>
    /// Immutable description of a single damage application. Passed to
    /// <see cref="IDamageable.ApplyDamage"/> so every damage source (hitscan, projectile,
    /// explosion, contact) shares one path.
    /// </summary>
    public readonly struct DamageInfo
    {
        public readonly float Amount;
        public readonly Vector3 HitPoint;
        public readonly Vector3 HitNormal;
        public readonly Vector3 SourceDirection; // direction the damage came from (for knockback/reaction)
        public readonly GameObject Instigator;   // who caused it (may be null)

        public DamageInfo(float amount, Vector3 hitPoint, Vector3 hitNormal, Vector3 sourceDirection, GameObject instigator)
        {
            Amount = amount;
            HitPoint = hitPoint;
            HitNormal = hitNormal;
            SourceDirection = sourceDirection;
            Instigator = instigator;
        }

        public DamageInfo(float amount, GameObject instigator)
            : this(amount, Vector3.zero, Vector3.zero, Vector3.zero, instigator) { }
    }
}
