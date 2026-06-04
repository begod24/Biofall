using UnityEngine;

namespace Biofall.Weapons
{
    public abstract class SingleProjectileWeapon : WeaponBase
    {
        protected override void Fire(Vector3 aimDirection)
        {
            Vector3 dir = ApplySpread(aimDirection);
            SpawnProjectile(firePoint.position, dir);
        }
    }
}
