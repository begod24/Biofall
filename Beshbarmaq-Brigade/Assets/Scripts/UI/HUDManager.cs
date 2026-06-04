using UnityEngine;
using Biofall.Combat;
using Biofall.Weapons;

namespace Biofall.UI
{
    public class HUDManager : MonoBehaviour
    {
        public static HUDManager Instance { get; private set; }

        [SerializeField] private HealthBarUI healthBar;
        [SerializeField] private AmmoUI ammo;
        [SerializeField] private HealthComponent playerHealth;
        [SerializeField] private WeaponHolder playerWeaponHolder;

        private WeaponBase boundWeapon;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void OnEnable()
        {
            if (playerHealth != null)
            {
                playerHealth.OnHealthChanged += HandleHealthChanged;
                HandleHealthChanged(playerHealth);
            }
            if (playerWeaponHolder != null)
            {
                playerWeaponHolder.OnWeaponChanged += HandleWeaponChanged;
                HandleWeaponChanged(playerWeaponHolder.Current);
            }
        }

        private void OnDisable()
        {
            if (playerHealth != null) playerHealth.OnHealthChanged -= HandleHealthChanged;
            if (playerWeaponHolder != null) playerWeaponHolder.OnWeaponChanged -= HandleWeaponChanged;
            UnbindWeapon();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void HandleHealthChanged(HealthComponent h)
        {
            if (healthBar != null) healthBar.SetValue(h.Normalized);
        }

        private void HandleWeaponChanged(WeaponBase w)
        {
            UnbindWeapon();
            boundWeapon = w;
            if (boundWeapon != null)
            {
                boundWeapon.OnAmmoChanged += RefreshAmmo;
                boundWeapon.OnReloadStart += RefreshAmmo;
                boundWeapon.OnReloadComplete += RefreshAmmo;
            }
            RefreshAmmo();
        }

        private void UnbindWeapon()
        {
            if (boundWeapon == null) return;
            boundWeapon.OnAmmoChanged -= RefreshAmmo;
            boundWeapon.OnReloadStart -= RefreshAmmo;
            boundWeapon.OnReloadComplete -= RefreshAmmo;
            boundWeapon = null;
        }

        private void RefreshAmmo()
        {
            if (ammo == null) return;
            if (boundWeapon == null) { ammo.SetText("--"); return; }
            if (boundWeapon.Data != null && boundWeapon.Data.infiniteAmmo) { ammo.SetText("∞"); return; }
            if (boundWeapon.IsReloading) { ammo.SetText("..."); return; }
            ammo.SetText($"{boundWeapon.CurrentAmmo}/{boundWeapon.MaxAmmo}");
        }
    }
}
