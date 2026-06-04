using UnityEngine;

namespace Biofall.Weapons
{
    public class DuoPistols : WeaponBase
    {
        [SerializeField] private Transform firePointLeft;
        [SerializeField] private Transform firePointRight;
        private bool useLeft;

        protected override void Fire(Vector3 aimDirection)
        {
            Transform fp = useLeft ? firePointLeft : firePointRight;
            if (fp == null) fp = firePoint;
            Vector3 dir = ApplySpread(aimDirection);
            SpawnProjectile(fp.position, dir);
            useLeft = !useLeft;
        }
    }
}
