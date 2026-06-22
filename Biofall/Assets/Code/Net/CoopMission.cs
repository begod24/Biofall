using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using Biofall.Core;
using Biofall.Gameplay.Mission1;

namespace Biofall.Net
{
    /// <summary>
    /// CO-OP mission authority + mirror (scene NetworkObject in the co-op mission scene; solo never
    /// has it). The real <see cref="MissionDirector"/> + stations run normally on the SERVER and talk
    /// over the local EventBus exactly like solo; this component forwards their results to clients:
    ///   • phase  → a server-written <see cref="NetworkVariable{T}"/> (also syncs late-joiners),
    ///   • facts  (generator on / beacon on / beacon charged / extracted) → <see cref="FactClientRpc"/>,
    ///   • the shared progress bar → throttled <see cref="ProgressClientRpc"/>.
    /// Clients re-publish each onto their LOCAL EventBus, so the existing HUD + station VFX react
    /// unchanged. It also routes client interaction requests (press E) back to the server stations.
    /// </summary>
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

        // Latest progress-bar state (server), pushed to clients at a capped rate.
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
                EventBus.Publish(new MissionPhaseChanged(_phase.Value)); // initialise the client HUD
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

        // ---- server: local EventBus → network ----

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

            // Continuous bars (defense/extraction) publish every frame on the server — cap the
            // network rate to ~10 Hz, but push discrete hide/show events immediately.
            _progSendTimer -= Time.deltaTime;
            if (_progActive && _progSendTimer > 0f) return;

            _progSendTimer = 0.1f;
            _progDirty = false;
            ProgressClientRpc(new FixedString64Bytes(_progLabel ?? string.Empty), _progValue, _progActive);
        }

        // ---- clients: network → local EventBus ----

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

        // ---- client → server interaction requests ----

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
