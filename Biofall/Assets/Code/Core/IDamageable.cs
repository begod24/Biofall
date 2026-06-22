namespace Biofall.Core
{
    public interface IDamageable
    {
        void TakeDamage(in DamageInfo info);
    }

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
