using UnityEngine;

namespace Biofall.Gameplay
{
    /// <summary>
    /// Data-driven weapon configuration (ScriptableObject). Stats and asset references live
    /// here, not hardcoded in <see cref="Weapon"/>, so tweaking/adding weapons needs no code.
    /// </summary>
    [CreateAssetMenu(menuName = "Biofall/Weapon Data", fileName = "WD_Weapon")]
    public sealed class WeaponData : ScriptableObject
    {
        public enum FireMode { Single, Auto, Burst }

        [Header("Stats")]
        public float damage = 20f;
        [Tooltip("Shots per second (the rate within a burst / for auto).")]
        public float fireRate = 5f;
        public int magazineSize = 12;
        public float reloadTime = 1.2f;
        [Tooltip("Max hitscan distance (metres).")]
        public float range = 100f;

        [Header("Fire mode")]
        [Tooltip("Single = one per click; Auto = continuous while held; Burst = tap fires one, hold fires bursts.")]
        public FireMode fireMode = FireMode.Single;
        public int burstCount = 3;
        [Tooltip("Gap between bursts while the trigger is held (seconds).")]
        public float burstCooldown = 0.35f;
        [Tooltip("How long the trigger must be held before burst-firing kicks in (tap vs hold).")]
        public float holdToBurst = 0.18f;

        [Header("Bullet (visual tracer)")]
        public GameObject bulletPrefab;
        public float bulletSpeed = 80f;

        [Header("VFX / SFX")]
        public GameObject muzzleFlashPrefab;
        public AudioClip shootSfx;
        public AudioClip reloadSfx;
    }
}
