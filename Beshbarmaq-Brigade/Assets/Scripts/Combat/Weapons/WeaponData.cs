using UnityEngine;

namespace Biofall.Weapons
{
    public enum WeaponClass { DuoPistols, Shotgun, UZI, SniperRifle, AssaultRifle }

    [CreateAssetMenu(fileName = "WeaponData", menuName = "Biofall/Weapon Data")]
    public class WeaponData : ScriptableObject
    {
        public string displayName;
        public WeaponClass weaponClass;
        public Sprite icon;

        [Header("Damage & Fire")]
        public float damage = 10f;
        public float fireRate = 8f;
        public float spreadDegrees = 1f;
        public int pelletsPerShot = 1;

        [Header("Ammo")]
        public bool infiniteAmmo;
        public int magazineSize = 12;
        public float reloadTime = 1.2f;

        [Header("Projectile")]
        public Projectile projectilePrefab;
        public float projectileSpeed = 60f;
        public float projectileLifetime = 2f;
        public int projectilePoolSize = 32;

        [Header("Feedback")]
        public float recoil = 0.05f;
        public AudioClip fireClip;
        public AudioClip reloadClip;
    }
}
