using UnityEngine;

namespace Biofall.Weapons
{
    public abstract class WeaponBase : MonoBehaviour
    {
        [SerializeField] protected WeaponData data;
        [SerializeField] protected Transform firePoint;
        [SerializeField] protected AudioSource audioSource;

        protected ObjectPool<Projectile> projectilePool;
        protected GameObject owner;
        protected float nextFireTime;
        protected int currentAmmo;
        protected bool reloading;

        public WeaponData Data => data;
        public int CurrentAmmo => data != null && data.infiniteAmmo ? int.MaxValue : currentAmmo;
        public int MaxAmmo => data != null ? data.magazineSize : 0;
        public bool IsReloading => reloading;

        public event System.Action OnFired;
        public event System.Action OnReloadStart;
        public event System.Action OnReloadComplete;
        public event System.Action OnAmmoChanged;

        protected virtual void Awake()
        {
            if (data != null && data.projectilePrefab != null)
                projectilePool = new ObjectPool<Projectile>(data.projectilePrefab, data.projectilePoolSize);
            currentAmmo = data != null ? data.magazineSize : 0;
        }

        public void SetOwner(GameObject ownerGo)
        {
            owner = ownerGo;
        }

        public virtual void TryFire(Vector3 aimDirection)
        {
            if (data == null || firePoint == null) return;
            if (Time.time < nextFireTime || reloading) return;
            if (!data.infiniteAmmo && currentAmmo <= 0) { TryReload(); return; }

            Fire(aimDirection);
            nextFireTime = Time.time + 1f / Mathf.Max(0.01f, data.fireRate);

            if (!data.infiniteAmmo)
            {
                currentAmmo--;
                OnAmmoChanged?.Invoke();
            }
            OnFired?.Invoke();
            if (audioSource != null && data.fireClip != null) audioSource.PlayOneShot(data.fireClip);
        }

        protected abstract void Fire(Vector3 aimDirection);

        public virtual void TryReload()
        {
            if (reloading || data == null || data.infiniteAmmo) return;
            if (currentAmmo >= data.magazineSize) return;
            reloading = true;
            OnReloadStart?.Invoke();
            if (audioSource != null && data.reloadClip != null) audioSource.PlayOneShot(data.reloadClip);
            Invoke(nameof(FinishReload), data.reloadTime);
        }

        private void FinishReload()
        {
            currentAmmo = data.magazineSize;
            reloading = false;
            OnAmmoChanged?.Invoke();
            OnReloadComplete?.Invoke();
        }

        protected void SpawnProjectile(Vector3 origin, Vector3 direction)
        {
            if (projectilePool == null) return;
            var p = projectilePool.Get();
            p.Launch(origin, direction, data.damage, data.projectileSpeed, data.projectileLifetime, owner, projectilePool.Return);
        }

        protected Vector3 ApplySpread(Vector3 direction)
        {
            if (data.spreadDegrees <= 0f) return direction.normalized;
            float yaw = Random.Range(-data.spreadDegrees, data.spreadDegrees);
            return (Quaternion.AngleAxis(yaw, Vector3.up) * direction).normalized;
        }
    }
}
