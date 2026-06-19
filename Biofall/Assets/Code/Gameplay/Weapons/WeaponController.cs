using UnityEngine;

namespace Biofall.Gameplay
{
    /// <summary>
    /// Player arsenal: switches the active weapon (1 = slot 0, 2 = slot 1) by enabling one weapon
    /// GameObject at a time and telling the Animator which weapon is held (Weapon int: 0 pistol,
    /// 1 rifle). Each weapon keeps its own <see cref="AmmoSystem"/>; the active one drives the HUD.
    /// </summary>
    [RequireComponent(typeof(PlayerInput))]
    public sealed class WeaponController : MonoBehaviour
    {
        [Tooltip("Slot order: 0 = pistol (key 1), 1 = M4 (key 2).")]
        [SerializeField] private Weapon[] weapons;
        [SerializeField] private Animator animator;

        private PlayerInput _input;
        private int _active = -1;

        private static readonly int WeaponId = Animator.StringToHash("Weapon");
        private static readonly int FireId = Animator.StringToHash("Fire");
        private static readonly int ReloadId = Animator.StringToHash("Reload");

        /// <summary>Ammo of the currently equipped weapon (for pickups).</summary>
        public AmmoSystem ActiveAmmo =>
            (_active >= 0 && _active < weapons.Length && weapons[_active] != null)
                ? weapons[_active].GetComponent<AmmoSystem>()
                : null;

        /// <summary>Ammo pickups feed every finite weapon (the pistol is infinite, so rounds go to the M4).</summary>
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
        }

        private void Equip(int index)
        {
            if (weapons == null || index < 0 || index >= weapons.Length || index == _active) return;
            _active = index;

            for (int k = 0; k < weapons.Length; k++)
                if (weapons[k] != null) weapons[k].gameObject.SetActive(k == index);

            if (animator != null)
            {
                animator.SetInteger(WeaponId, index == 1 ? 1 : 0);
                animator.ResetTrigger(FireId);   // drop any stale trigger from the previous weapon
                animator.ResetTrigger(ReloadId);
            }
        }
    }
}
