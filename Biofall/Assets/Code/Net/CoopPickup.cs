using Unity.Netcode;
using UnityEngine;
using Biofall.Core;
using Biofall.Gameplay;

namespace Biofall.Net
{
    /// <summary>
    /// Server-authoritative bridge for a networked ground pickup (lives only on the <c>*_Coop</c>
    /// pickup prefab variants; solo uses the plain pickup prefab and never has this). NGO spawning
    /// does NOT fire the pooling <c>IPoolable.OnSpawned</c>, so the base <see cref="Pickup"/> only
    /// animates the visual in co-op and this drives collection + lifetime:
    ///   • Every peer checks its OWN local player against the pickup's collect radius; the first to
    ///     reach it asks the server with <see cref="CollectRpc"/>.
    ///   • SERVER → the first request wins (others are ignored), the reward is granted to ONLY that
    ///     player via <see cref="AwardRpc"/>, then the <see cref="NetworkObject"/> is despawned for
    ///     everyone. Because each wallet / ammo / grenade pouch is local to its owner, this makes
    ///     Bio Samples and supplies per-player.
    /// </summary>
    [RequireComponent(typeof(NetworkObject))]
    [RequireComponent(typeof(Pickup))]
    public sealed class CoopPickup : NetworkBehaviour
    {
        [Tooltip("Seconds before the server auto-despawns an uncollected pickup. 0 = never (matches the solo prefabs).")]
        [SerializeField] private float lifetime = 0f;

        private Pickup _pickup;
        private Transform _tf;
        private bool _requested;   // local guard so a peer only fires one CollectRpc
        private bool _consumed;    // server guard so only the first request is awarded
        private float _age;

        private void Awake()
        {
            _pickup = GetComponent<Pickup>();
            _tf = transform;
        }

        private void Update()
        {
            // First-to-reach detection runs on every peer against its own local player.
            if (!_requested && PlayerRegistry.LocalPlayer != null)
            {
                Vector3 d = PlayerRegistry.LocalPlayer.position - _tf.position;
                d.y = 0f;
                float r = _pickup.CollectRadius;
                if (d.sqrMagnitude <= r * r)
                {
                    _requested = true; // optimistic; the server has the final say
                    CollectRpc();
                }
            }

            // Server owns the lifetime timer (pickups default to "never expire").
            if (IsServer && lifetime > 0f)
            {
                _age += Time.deltaTime;
                if (_age >= lifetime && NetworkObject.IsSpawned) NetworkObject.Despawn(true);
            }
        }

        [Rpc(SendTo.Server)]
        private void CollectRpc(RpcParams rpcParams = default)
        {
            if (_consumed) return;
            _consumed = true;

            // Grant the reward to ONLY the collecting player, then remove the pickup for everyone.
            // The award RPC is sent before the despawn so it is processed on the collector while the
            // object is still alive (same reliable ordering CoopEnemy relies on for its death FX).
            AwardRpc(RpcTarget.Single(rpcParams.Receive.SenderClientId, RpcTargetUse.Temp));
            if (NetworkObject.IsSpawned) NetworkObject.Despawn(true);
        }

        [Rpc(SendTo.SpecifiedInParams)]
        private void AwardRpc(RpcParams rpcParams = default) => _pickup.ApplyReward();
    }
}
