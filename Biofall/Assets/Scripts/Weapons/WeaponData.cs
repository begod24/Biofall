using UnityEngine;

namespace Biofall.Weapons
{
    public enum WeaponFireType { Hitscan, Projectile }

    /// <summary>
    /// Data-driven definition of a weapon. New weapons are authored as assets, not code,
    /// wherever behavior fits the hitscan/projectile templates.
    /// </summary>
    [CreateAssetMenu(menuName = "Biofall/Weapon Data", fileName = "WeaponData")]
    public class WeaponData : ScriptableObject
    {
        [Header("Identity")]
        public string WeaponName = "Assault Rifle";
        public WeaponFireType FireType = WeaponFireType.Hitscan;

        [Header("Damage & Fire")]
        public float Damage = 12f;
        [Tooltip("Rounds per minute.")]
        public float FireRate = 600f;
        [Tooltip("Pellets/rays per shot (1 for rifles, >1 for shotguns).")]
        public int PelletsPerShot = 1;
        [Tooltip("Max aim cone half-angle in degrees.")]
        public float SpreadDegrees = 1.5f;
        public float Range = 60f;

        [Header("Ammo")]
        public int MagazineSize = 30;
        public int ReserveAmmo = 120;
        public float ReloadTime = 1.6f;

        [Header("Projectile (FireType = Projectile only)")]
        public GameObject ProjectilePrefab;
        public float ProjectileSpeed = 30f;

        [Header("Feedback (pooled prefabs)")]
        public GameObject MuzzleVfxPrefab;
        public GameObject ImpactVfxPrefab;
        public AudioClip FireSfx;

        /// <summary>Seconds between shots derived from rounds-per-minute.</summary>
        public float SecondsBetweenShots => FireRate > 0f ? 60f / FireRate : 0f;
    }
}
