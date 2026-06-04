using UnityEngine;

namespace Biofall.Combat
{
    public enum DamageType { Generic, Bullet, Melee, Explosive, Acid }

    public struct DamageInfo
    {
        public float Amount;
        public GameObject Source;
        public Vector3 HitPoint;
        public Vector3 HitNormal;
        public DamageType Type;

        public DamageInfo(float amount, GameObject source = null, Vector3 hitPoint = default, Vector3 hitNormal = default, DamageType type = DamageType.Generic)
        {
            Amount = amount;
            Source = source;
            HitPoint = hitPoint;
            HitNormal = hitNormal;
            Type = type;
        }
    }
}
