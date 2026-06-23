using UnityEngine;

namespace Biofall.Gameplay
{
    [RequireComponent(typeof(PlayerInput))]
    public sealed class WeaponController : MonoBehaviour
    {
        [Tooltip("Slot order: 0 = pistol (key 1), 1 = M4 (key 2), 2 = SG-12 (key 3).")]
        [SerializeField] private Weapon[] weapons;
        [SerializeField] private Animator animator;

        private PlayerInput _input;
        private int _active = -1;

        private static readonly int WeaponId = Animator.StringToHash("Weapon");
        private static readonly int FireId = Animator.StringToHash("Fire");
        private static readonly int ReloadId = Animator.StringToHash("Reload");

        public int ActiveSlot => _active;

        public Weapon WeaponAt(int index) =>
            (weapons != null && index >= 0 && index < weapons.Length) ? weapons[index] : null;

        public AmmoSystem ActiveAmmo =>
            (_active >= 0 && _active < weapons.Length && weapons[_active] != null)
                ? weapons[_active].GetComponent<AmmoSystem>()
                : null;

        public void AddReserveAmmo(int amount)
        {
            if (weapons == null) return;
            foreach (var w in weapons)
            {
                if (w == null || w.InfiniteAmmo) continue;
                var ammo = w.GetComponent<AmmoSystem>();
                if (ammo != null) ammo.AddRounds(amount);
            }
        }

        private void Awake()
        {
            _input = GetComponent<PlayerInput>();
            if (animator == null) animator = GetComponentInChildren<Animator>();
        }

        private void Start() => Equip(0);

        private void Update()
        {
            int slot = _input.WeaponSlot;
            if (slot == 1) Equip(0);
            else if (slot == 2) Equip(1);
            else if (slot == 3) Equip(2);
        }

        private void Equip(int index)
        {
            if (weapons == null || index < 0 || index >= weapons.Length || index == _active) return;
            _active = index;

            for (int k = 0; k < weapons.Length; k++)
                if (weapons[k] != null) weapons[k].gameObject.SetActive(k == index);

            if (animator != null)
            {
                // Pistol (slot 0) uses the one-handed pose; every long gun (M4, SG-12) uses the rifle pose.
                animator.SetInteger(WeaponId, index == 0 ? 0 : 1);
                animator.ResetTrigger(FireId);
                animator.ResetTrigger(ReloadId);
            }
        }
    }
}
