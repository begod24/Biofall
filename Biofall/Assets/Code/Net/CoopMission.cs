using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using Biofall.Core;
using Biofall.Gameplay.Mission1;

namespace Biofall.Net
{
    [RequireComponent(typeof(NetworkObject))]
    public sealed class CoopMission : NetworkBehaviour
    {
        public static CoopMission Instance { get; private set; }

        [SerializeField] private GeneratorStation generator;
        [SerializeField] private BeaconStation beacon;

        private readonly NetworkVariable<MissionPhase> _phase = new(
            MissionPhase.FindGenerator,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private string _progLabel;
        private float _progValue;
        private bool _progActive;
        private bool _progDirty;
        private float _progSendTimer;

        public override void OnNetworkSpawn()
        {
            Instance = this;

            if (IsServer)
            {
                EventBus.Subscribe<MissionPhaseChanged>(OnPhaseServer);
                EventBus.Subscribe<MissionProgress>(OnProgressServer);
                EventBus.Subscribe<GeneratorActivated>(OnGeneratorServer);
                EventBus.Subscribe<BeaconActivated>(OnBeaconServer);
                EventBus.Subscribe<BeaconCharged>(OnChargedServer);
                EventBus.Subscribe<MissionCompleted>(OnCompletedServer);
            }
            else
            {
                _phase.OnValueChanged += OnPhaseClient;
                EventBus.Publish(new MissionPhaseChanged(_phase.Value));
            }
        }

        public override void OnNetworkDespawn()
        {
            if (Instance == this) Instance = null;

            if (IsServer)
            {
                EventBus.Unsubscribe<MissionPhaseChanged>(OnPhaseServer);
                EventBus.Unsubscribe<MissionProgress>(OnProgressServer);
                EventBus.Unsubscribe<GeneratorActivated>(OnGeneratorServer);
                EventBus.Unsubscribe<BeaconActivated>(OnBeaconServer);
                EventBus.Unsubscribe<BeaconCharged>(OnChargedServer);
                EventBus.Unsubscribe<MissionCompleted>(OnCompletedServer);
            }
            else
            {
                _phase.OnValueChanged -= OnPhaseClient;
            }
        }

        private void OnPhaseServer(MissionPhaseChanged e) => _phase.Value = e.Phase;

        private void OnProgressServer(MissionProgress e)
        {
            _progLabel = e.Label;
            _progValue = e.Value01;
            _progActive = e.Active;
            _progDirty = true;
        }

        private void OnGeneratorServer(GeneratorActivated _) => FactClientRpc(0);
        private void OnBeaconServer(BeaconActivated _) => FactClientRpc(1);
        private void OnChargedServer(BeaconCharged _) => FactClientRpc(2);
        private void OnCompletedServer(MissionCompleted _) => FactClientRpc(3);

        private void Update()
        {
            if (!IsServer || !_progDirty) return;

            _progSendTimer -= Time.deltaTime;
            if (_progActive && _progSendTimer > 0f) return;

            _progSendTimer = 0.1f;
            _progDirty = false;
            ProgressClientRpc(new FixedString64Bytes(_progLabel ?? string.Empty), _progValue, _progActive);
        }

        private void OnPhaseClient(MissionPhase _, MissionPhase current) =>
            EventBus.Publish(new MissionPhaseChanged(current));

        [Rpc(SendTo.NotServer)]
        private void ProgressClientRpc(FixedString64Bytes label, float value, bool active)
        {
            string l = label.Length > 0 ? label.ToString() : null;
            EventBus.Publish(new MissionProgress(l, value, active));
        }

        [Rpc(SendTo.NotServer)]
        private void FactClientRpc(int id)
        {
            switch (id)
            {
                case 0: EventBus.Publish(new GeneratorActivated()); break;
                case 1: EventBus.Publish(new BeaconActivated()); break;
                case 2: EventBus.Publish(new BeaconCharged()); break;
                case 3: EventBus.Publish(new MissionCompleted()); break;
            }
        }

        [Rpc(SendTo.Server)]
        public void RequestGeneratorRpc()
        {
            if (generator != null) generator.ServerInteract();
        }

        [Rpc(SendTo.Server)]
        public void RequestBeaconRpc()
        {
            if (beacon != null) beacon.ServerInteract();
        }
    }
}
