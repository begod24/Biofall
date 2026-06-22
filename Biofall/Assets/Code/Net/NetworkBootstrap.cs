using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

namespace Biofall.Net
{
    /// <summary>
    /// Owns the co-op session lifecycle. Lives on the <c>NetworkRoot</c> prefab (with
    /// <see cref="NetworkManager"/> + <see cref="UnityTransport"/>), persists across scene loads,
    /// and is only ever instantiated on the CO-OP path (Host/Join from the lobby) — solo never
    /// spawns it, so solo stays completely non-networked. Phase 0 scaffolding: exposes
    /// StartHost / StartClient(ip) / Shutdown and flips <see cref="NetSession.InCoop"/>.
    /// LAN address handling is refined in Phase A/B (direct IP) and discovery in Phase B.
    /// </summary>
    [RequireComponent(typeof(NetworkManager))]
    [RequireComponent(typeof(UnityTransport))]
    public sealed class NetworkBootstrap : MonoBehaviour
    {
        public static NetworkBootstrap Instance { get; private set; }

        /// <summary>Why the last session ended unexpectedly (host left / connection lost) — the main
        /// menu can surface this. Cleared when a new session starts.</summary>
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
        private bool _intentionalShutdown; // distinguishes a deliberate leave from a dropped connection

        public ushort Port => port;
        public string SessionName { get => sessionName; set => sessionName = value; }
        public GameObject PlayerPrefab => playerPrefab;

        /// <summary>LAN lobby discovery (advertise as host / browse as client). Always present.</summary>
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

        /// <summary>Start hosting (server + local player). Binds all interfaces so LAN clients can join.</summary>
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
                _discovery?.StartAdvertising(sessionName, port); // become findable on the LAN
                if (coopSessionPrefab != null)
                {
                    var go = Instantiate(coopSessionPrefab);
                    go.GetComponent<NetworkObject>().Spawn();
                }
            }
            return ok;
        }

        /// <summary>Join a host by its LAN IP.</summary>
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
                _discovery?.StopListening(); // stop browsing once we're joining
            }
            return ok;
        }

        /// <summary>(Re)subscribe to NGO connection callbacks for this session.</summary>
        private void SubscribeNet()
        {
            if (_nm == null) return;
            _nm.OnClientDisconnectCallback -= OnClientDisconnect;
            _nm.OnClientDisconnectCallback += OnClientDisconnect;
        }

        /// <summary>
        /// A connection dropped. On a pure CLIENT, losing the host (no host-migration in base NGO)
        /// tears the session, so bail back to the main menu with a reason. A deliberate leave
        /// (<see cref="Shutdown"/>) sets <see cref="_intentionalShutdown"/> so this is ignored. On the
        /// HOST a client leaving is handled by CoopSession/CoopPlayerLife — the match continues.
        /// </summary>
        private void OnClientDisconnect(ulong clientId)
        {
            if (_intentionalShutdown || _nm == null) return;
            if (_nm.IsServer) return; // host: a client left; session continues

            // Pure client: our own disconnect (or the server's) means the session is gone.
            if (clientId == _nm.LocalClientId || clientId == NetworkManager.ServerClientId)
            {
                LastDisconnectMessage = "Lost connection to host.";
                LeaveToMainMenu();
            }
        }

        public void Shutdown()
        {
            _intentionalShutdown = true; // a deliberate leave — don't treat the resulting disconnect as a drop
            _discovery?.StopAdvertising();
            _discovery?.StopListening();
            if (_nm != null)
            {
                _nm.OnClientDisconnectCallback -= OnClientDisconnect;
                if (_nm.IsListening) _nm.Shutdown();
            }
            NetSession.InCoop = false;
        }

        /// <summary>
        /// Tear down the co-op session and return to the main menu. Used by the end-of-mission /
        /// game-over UI so leaving a networked match doesn't local-load a scene while NGO is still live.
        /// </summary>
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
