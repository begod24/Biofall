using Unity.Netcode;
using UnityEngine;
using Biofall.Core;
using Biofall.Gameplay;
using Biofall.Gameplay.Mission1;

namespace Biofall.Net
{
    /// <summary>
    /// Lives only on the CO-OP player prefab variant (solo uses the plain Player prefab and never
    /// has this). On spawn it decides who drives this body:
    ///   • Owner  → register as the local player + point the camera at it; all the normal player
    ///     systems keep running, so the owner controls it exactly like in solo.
    ///   • Remote → disable the local simulation components (input/motor/aim/weapons), so this
    ///     machine doesn't drive someone else's character — the body just follows its
    ///     owner-authoritative <see cref="ClientNetworkTransform"/>.
    /// Keeps every player registered in <see cref="PlayerRegistry"/> for enemy targeting (Phase C).
    /// </summary>
    [RequireComponent(typeof(NetworkObject))]
    public sealed class CoopPlayer : NetworkBehaviour
    {
        public override void OnNetworkSpawn()
        {
            if (IsOwner)
            {
                PlayerRegistry.SetLocal(transform);
                var cam = FindFirstObjectByType<TopDownCamera>();
                if (cam != null) cam.SetTarget(transform);
            }
            else
            {
                DisableLocalSimulation();
                // PlayerController.OnDisable just unregistered us — re-add so we stay a valid target.
                PlayerRegistry.Register(transform);
            }
        }

        public override void OnNetworkDespawn()
        {
            PlayerRegistry.Unregister(transform);
        }

        /// <summary>
        /// Server → this player's OWNER: apply enemy melee damage locally. Player HP is owner-
        /// authoritative (each client owns its own <see cref="Health"/>), so the server can't write a
        /// remote player's HP directly — it asks the owner, whose local Health.TakeDamage then drives
        /// PlayerHealthReporter → HUD / camera shake / low-health FX exactly like solo.
        /// </summary>
        [Rpc(SendTo.Owner)]
        public void TakeDamageRpc(float amount, Vector3 from)
        {
            var health = GetComponent<Health>();
            if (health == null) return;
            Vector3 dir = transform.position - from;
            dir.y = 0f;
            health.TakeDamage(new DamageInfo(amount, transform.position, dir.normalized, null));
        }

        private void DisableLocalSimulation()
        {
            Disable<PlayerController>();
            Disable<PlayerMotor>();
            Disable<PlayerAim>();
            Disable<PlayerInput>();
            Disable<Weapon>();
            Disable<WeaponController>();
            Disable<GrenadeThrower>();
            Disable<PlayerInteractor>();
        }

        private void Disable<T>() where T : Behaviour
        {
            var c = GetComponent<T>();
            if (c != null) c.enabled = false;
        }
    }
}
