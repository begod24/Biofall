using Unity.Netcode;
using UnityEngine;
using Biofall.Core;
using Biofall.Gameplay;

namespace Biofall.Net
{
    [RequireComponent(typeof(NetworkObject))]
    [RequireComponent(typeof(Pickup))]
    public sealed class CoopPickup : NetworkBehaviour
    {
        [Tooltip("Seconds before the server auto-despawns an uncollected pickup. 0 = never (matches the solo prefabs).")]
        [SerializeField] private float lifetime = 0f;

        private Pickup _pickup;
        private Transform _tf;
        private bool _requested;
        private bool _consumed;
        private float _age;

        private void Awake()
        {
            _pickup = GetComponent<Pickup>();
            _tf = transform;
        }

        private void Update()
        {
            if (!_requested && PlayerRegistry.LocalPlayer != null)
            {
                Vector3 d = PlayerRegistry.LocalPlayer.position - _tf.position;
                d.y = 0f;
                float r = _pickup.CollectRadius;
                if (d.sqrMagnitude <= r * r)
                {
                    _requested = true;
                    CollectRpc();
                }
            }

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

            AwardRpc(RpcTarget.Single(rpcParams.Receive.SenderClientId, RpcTargetUse.Temp));
            if (NetworkObject.IsSpawned) NetworkObject.Despawn(true);
        }

        [Rpc(SendTo.SpecifiedInParams)]
        private void AwardRpc(RpcParams rpcParams = default) => _pickup.ApplyReward();
    }
}
