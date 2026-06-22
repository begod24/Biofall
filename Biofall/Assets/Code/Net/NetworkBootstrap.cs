using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

namespace Biofall.Net
{
    [RequireComponent(typeof(NetworkManager))]
    [RequireComponent(typeof(UnityTransport))]
    public sealed class NetworkBootstrap : MonoBehaviour
    {
        public static NetworkBootstrap Instance { get; private set; }

        public static string LastDisconnectMessage;

        public const ushort DefaultPort = 7777;

        [Tooltip("UDP port the host listens on / clients connect to.")]
        [SerializeField] private ushort port = DefaultPort;
        [Tooltip("Session name advertised on the LAN browser.")]
        [SerializeField] private string sessionName = "BIOFALL Squad";
        [Tooltip("CoopSession prefab (NetworkObject) the host spawns to drive the lobby.")]
        [SerializeField] private GameObject coopSessionPrefab;
        [Tooltip("Networked co-op player prefab. Spawned by CoopSession after the game scene loads.")]
        [SerializeField] private GameObject playerPrefab;

        private NetworkManager _nm;
        private UnityTransport _transport;
        private LanDiscovery _discovery;
        private bool _intentionalShutdown;

        public ushort Port => port;
        public string SessionName { get => sessionName; set => sessionName = value; }
        public GameObject PlayerPrefab => playerPrefab;

        public LanDiscovery Discovery => _discovery;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            _nm = GetComponent<NetworkManager>();
            _transport = GetComponent<UnityTransport>();
            _discovery = GetComponent<LanDiscovery>();
            if (_discovery == null) _discovery = gameObject.AddComponent<LanDiscovery>();

            if (playerPrefab == null && _nm != null)
                playerPrefab = _nm.NetworkConfig.PlayerPrefab;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public bool StartHost(string listenAddress = "0.0.0.0")
        {
            Configure("127.0.0.1", listenAddress);
            DisableAutomaticPlayerSpawn();
            _intentionalShutdown = false;
            LastDisconnectMessage = null;
            bool ok = _nm.StartHost();
            NetSession.InCoop = ok;
            if (ok)
            {
                SubscribeNet();
                _discovery?.StartAdvertising(sessionName, port);
                if (coopSessionPrefab != null)
                {
                    var go = Instantiate(coopSessionPrefab);
                    go.GetComponent<NetworkObject>().Spawn();
                }
            }
            return ok;
        }

        public bool StartClient(string serverAddress)
        {
            Configure(serverAddress, "0.0.0.0");
            DisableAutomaticPlayerSpawn();
            _intentionalShutdown = false;
            LastDisconnectMessage = null;
            bool ok = _nm.StartClient();
            NetSession.InCoop = ok;
            if (ok)
            {
                SubscribeNet();
                _discovery?.StopListening();
            }
            return ok;
        }

        private void SubscribeNet()
        {
            if (_nm == null) return;
            _nm.OnClientDisconnectCallback -= OnClientDisconnect;
            _nm.OnClientDisconnectCallback += OnClientDisconnect;
        }

        private void OnClientDisconnect(ulong clientId)
        {
            if (_intentionalShutdown || _nm == null) return;
            if (_nm.IsServer) return;

            if (clientId == _nm.LocalClientId || clientId == NetworkManager.ServerClientId)
            {
                LastDisconnectMessage = "Lost connection to host.";
                LeaveToMainMenu();
            }
        }

        public void Shutdown()
        {
            _intentionalShutdown = true;
            _discovery?.StopAdvertising();
            _discovery?.StopListening();
            if (_nm != null)
            {
                _nm.OnClientDisconnectCallback -= OnClientDisconnect;
                if (_nm.IsListening) _nm.Shutdown();
            }
            NetSession.InCoop = false;
        }

        public void LeaveToMainMenu()
        {
            Shutdown();
            UnityEngine.SceneManagement.SceneManager.LoadScene(Biofall.Core.GameScenes.MainMenu);
        }

        private void Configure(string serverAddress, string listenAddress)
        {
            if (_transport != null)
                _transport.SetConnectionData(serverAddress, port, listenAddress);
        }

        private void DisableAutomaticPlayerSpawn()
        {
            if (_nm == null) return;
            if (playerPrefab == null) playerPrefab = _nm.NetworkConfig.PlayerPrefab;
            _nm.NetworkConfig.PlayerPrefab = null;
            _nm.NetworkConfig.AutoSpawnPlayerPrefabClientSide = false;
        }
    }
}
