using UnityEngine;

namespace Biofall.Weapons
{
    public class Shotgun : WeaponBase
    {
        protected override void Fire(Vector3 aimDirection)
        {
            int pellets = Mathf.Max(1, data.pelletsPerShot);
            for (int i = 0; i < pellets; i++)
            {
                Vector3 dir = ApplySpread(aimDirection);
                SpawnProjectile(firePoint.position, dir);
            }
        }
    }
}
