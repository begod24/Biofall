using UnityEngine;
using Biofall.Core;

namespace Biofall.Gameplay
{
    [RequireComponent(typeof(PlayerInput))]
    [RequireComponent(typeof(GrenadeInventory))]
    public sealed class GrenadeThrower : MonoBehaviour
    {
        [SerializeField] private GameObject grenadePrefab;
        [Tooltip("Height above the player the grenade leaves from.")]
        [SerializeField] private float originHeight = 1.2f;
        [Tooltip("Forward offset from the player so the grenade doesn't spawn inside them (body faces the cursor).")]
        [SerializeField] private float forwardOffset = 0.6f;
        [Tooltip("Seconds the throw takes to reach the cursor. Short = flat, near-straight trajectory toward the aim point.")]
        [SerializeField] private float flightTime = 0.55f;
        [Tooltip("Fallback throw distance if there's no aim point yet.")]
        [SerializeField] private float fallbackDistance = 7f;

        private PlayerInput _input;
        private PlayerAim _aim;
        private GrenadeInventory _inventory;

        private void Awake()
        {
            _input = GetComponent<PlayerInput>();
            _aim = GetComponent<PlayerAim>();
            _inventory = GetComponent<GrenadeInventory>();
        }

        private void Update()
        {
            if (Time.timeScale <= 0f) return;
            if (_input == null || !_input.GrenadePressed) return;
            if (grenadePrefab == null || PoolService.Instance == null) return;
            if (!_inventory.TryConsume()) return;

            Vector3 origin = transform.position + Vector3.up * originHeight + transform.forward * forwardOffset;
            Vector3 target = _aim != null ? _aim.AimPoint : transform.position + transform.forward * fallbackDistance;

            GameObject go = PoolService.Instance.Spawn(grenadePrefab, origin, Random.rotationUniform);
            if (go != null && go.TryGetComponent(out ThrownGrenade grenade))
                grenade.Launch(target, flightTime);
        }
    }
}
