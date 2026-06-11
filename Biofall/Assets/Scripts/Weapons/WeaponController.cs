using Biofall.Core;
using Biofall.Player;
using UnityEngine;

namespace Biofall.Weapons
{
    /// <summary>
    /// Drives the currently equipped weapon from player input and republishes ammo/fire
    /// to the global event hub for the HUD. Holds equipped-weapon logic only.
    /// </summary>
    public class WeaponController : MonoBehaviour
    {
        [SerializeField] private PlayerInputReader _input;
        [SerializeField] private WeaponBase _equippedWeapon;

        private void Awake()
        {
            if (_input == null) _input = GetComponentInParent<PlayerInputReader>();
        }

        private void OnEnable()
        {
            if (_input != null)
            {
                _input.OnReloadPressed += HandleReload;
            }
            HookWeapon(_equippedWeapon);
        }

        private void OnDisable()
        {
            if (_input != null)
            {
                _input.OnReloadPressed -= HandleReload;
            }
            UnhookWeapon(_equippedWeapon);
        }

        private void Update()
        {
            if (_equippedWeapon == null || _input == null) return;

            // Automatic fire while held; semi-auto weapons can gate on OnFirePressed later.
            if (_input.FireHeld && _equippedWeapon.TryFire())
            {
                GameEvents.RaiseWeaponFired();
            }
        }

        public void Equip(WeaponBase weapon)
        {
            UnhookWeapon(_equippedWeapon);
            _equippedWeapon = weapon;
            HookWeapon(weapon);
        }

        private void HandleReload() => _equippedWeapon?.Reload();

        private void HookWeapon(WeaponBase weapon)
        {
            if (weapon == null) return;
            weapon.OnAmmoChanged += HandleAmmoChanged;
            GameEvents.RaiseAmmoChanged(weapon.MagazineAmmo, weapon.ReserveAmmo);
        }

        private void UnhookWeapon(WeaponBase weapon)
        {
            if (weapon == null) return;
            weapon.OnAmmoChanged -= HandleAmmoChanged;
        }

        private void HandleAmmoChanged(int magazine, int reserve)
            => GameEvents.RaiseAmmoChanged(magazine, reserve);
    }
}
