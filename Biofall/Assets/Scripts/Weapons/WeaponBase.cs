using System;
using Biofall.Pooling;
using UnityEngine;

namespace Biofall.Weapons
{
    /// <summary>
    /// Shared weapon behavior: ammo, fire-rate gating, reload, spread, and pooled
    /// muzzle/impact feedback. Concrete weapons implement only <see cref="FireRay"/>
    /// for how a single pellet resolves (hitscan vs projectile) — Open/Closed in action.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public abstract class WeaponBase : MonoBehaviour, IWeapon
    {
        [SerializeField] protected WeaponData _data;
        [Tooltip("Origin and direction of fire. forward = aim direction.")]
        [SerializeField] protected Transform _muzzle;

        private AudioSource _audio;
        private float _nextFireTime;
        private float _reloadFinishTime;

        public WeaponData Data => _data;
        public int MagazineAmmo { get; private set; }
        public int ReserveAmmo { get; private set; }
        public bool IsReloading { get; private set; }

        /// <summary>Raised after a shot fires (magazine, reserve) for HUD/ammo wiring.</summary>
        public event Action<int, int> OnAmmoChanged;

        protected Transform Muzzle => _muzzle != null ? _muzzle : transform;

        protected virtual void Awake()
        {
            _audio = GetComponent<AudioSource>();
            if (_data != null)
            {
                MagazineAmmo = _data.MagazineSize;
                ReserveAmmo = _data.ReserveAmmo;
            }
        }

        private void Update()
        {
            if (IsReloading && Time.time >= _reloadFinishTime)
                FinishReload();
        }

        public bool TryFire()
        {
            if (_data == null || IsReloading || Time.time < _nextFireTime) return false;

            if (MagazineAmmo <= 0)
            {
                Reload();
                return false;
            }

            _nextFireTime = Time.time + _data.SecondsBetweenShots;
            MagazineAmmo--;

            for (int i = 0; i < Mathf.Max(1, _data.PelletsPerShot); i++)
                FireRay(GetSpreadDirection());

            PlayMuzzleFeedback();
            OnAmmoChanged?.Invoke(MagazineAmmo, ReserveAmmo);
            return true;
        }

        public void Reload()
        {
            if (IsReloading || _data == null) return;
            if (MagazineAmmo >= _data.MagazineSize || ReserveAmmo <= 0) return;

            IsReloading = true;
            _reloadFinishTime = Time.time + _data.ReloadTime;
        }

        private void FinishReload()
        {
            int needed = _data.MagazineSize - MagazineAmmo;
            int taken = Mathf.Min(needed, ReserveAmmo);
            MagazineAmmo += taken;
            ReserveAmmo -= taken;
            IsReloading = false;
            OnAmmoChanged?.Invoke(MagazineAmmo, ReserveAmmo);
        }

        /// <summary>Resolve a single pellet/ray along <paramref name="direction"/>.</summary>
        protected abstract void FireRay(Vector3 direction);

        protected Vector3 GetSpreadDirection()
        {
            Vector3 dir = Muzzle.forward;
            if (_data.SpreadDegrees <= 0f) return dir;

            float half = _data.SpreadDegrees;
            Quaternion rot = Quaternion.Euler(
                UnityEngine.Random.Range(-half, half),
                UnityEngine.Random.Range(-half, half),
                0f);
            return rot * dir;
        }

        protected void SpawnImpact(Vector3 point, Vector3 normal)
        {
            if (_data.ImpactVfxPrefab != null && PoolManager.Exists)
                PoolManager.Instance.Get(_data.ImpactVfxPrefab, point, Quaternion.LookRotation(normal));
        }

        private void PlayMuzzleFeedback()
        {
            if (_data.MuzzleVfxPrefab != null && PoolManager.Exists)
                PoolManager.Instance.Get(_data.MuzzleVfxPrefab, Muzzle.position, Muzzle.rotation);

            if (_data.FireSfx != null)
                _audio.PlayOneShot(_data.FireSfx);
        }
    }
}
