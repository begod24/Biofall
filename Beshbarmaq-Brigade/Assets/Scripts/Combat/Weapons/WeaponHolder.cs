using UnityEngine;
using UnityEngine.InputSystem;
using Biofall.Player;
using System.Collections.Generic;

namespace Biofall.Weapons
{
    public class WeaponHolder : MonoBehaviour
    {
        [SerializeField] private List<WeaponBase> weapons = new();
        [SerializeField] private int startingIndex;
        [SerializeField] private PlayerInputBinder input;
        [SerializeField] private PlayerAimer aimer;

        private int currentIndex = -1;

        public WeaponBase Current { get; private set; }
        public IReadOnlyList<WeaponBase> Weapons => weapons;
        public int CurrentIndex => currentIndex;

        public event System.Action<WeaponBase> OnWeaponChanged;

        private void Awake()
        {
            if (input == null) input = GetComponent<PlayerInputBinder>();
            if (aimer == null) aimer = GetComponent<PlayerAimer>();

            foreach (var w in weapons)
            {
                if (w == null) continue;
                w.SetOwner(gameObject);
                w.gameObject.SetActive(false);
            }

            if (weapons.Count > 0)
                Equip(Mathf.Clamp(startingIndex, 0, weapons.Count - 1));
        }

        private void OnEnable()
        {
            if (input == null) return;
            input.NextWeapon.performed += OnNext;
            input.PrevWeapon.performed += OnPrev;
            input.Reload.performed += OnReload;
        }

        private void OnDisable()
        {
            if (input == null) return;
            input.NextWeapon.performed -= OnNext;
            input.PrevWeapon.performed -= OnPrev;
            input.Reload.performed -= OnReload;
        }

        private void Update()
        {
            if (Current == null || input == null || aimer == null) return;
            if (input.Fire.IsPressed())
            {
                Current.TryFire(aimer.AimDirection);
            }
        }

        public void Equip(int index)
        {
            if (index < 0 || index >= weapons.Count) return;
            if (currentIndex == index) return;
            if (Current != null) Current.gameObject.SetActive(false);
            currentIndex = index;
            Current = weapons[index];
            if (Current != null) Current.gameObject.SetActive(true);
            OnWeaponChanged?.Invoke(Current);
        }

        private void OnNext(InputAction.CallbackContext _)
        {
            if (weapons.Count == 0) return;
            Equip((currentIndex + 1) % weapons.Count);
        }

        private void OnPrev(InputAction.CallbackContext _)
        {
            if (weapons.Count == 0) return;
            Equip((currentIndex - 1 + weapons.Count) % weapons.Count);
        }

        private void OnReload(InputAction.CallbackContext _)
        {
            Current?.TryReload();
        }
    }
}
