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

        public const ushort DefaultPort = 7777;

        [Tooltip("UDP port the host listens on / clients connect to.")]
        [SerializeField] private ushort port = DefaultPort;
        [Tooltip("Session name advertised on the LAN browser.")]
        [SerializeField] private string sessionName = "BIOFALL Squad";
        [Tooltip("CoopSession prefab (NetworkObject) the host spawns to drive the lobby.")]
        [SerializeField] private GameObject coopSessionPrefab;

        private NetworkManager _nm;
        private UnityTransport _transport;
        private LanDiscovery _discovery;

        public ushort Port => port;
        public string SessionName { get => sessionName; set => sessionName = value; }

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
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>Start hosting (server + local player). Binds all interfaces so LAN clients can join.</summary>
        public bool StartHost(string listenAddress = "0.0.0.0")
        {
            Configure("127.0.0.1", listenAddress);
            bool ok = _nm.StartHost();
            NetSession.InCoop = ok;
            if (ok)
            {
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
            bool ok = _nm.StartClient();
            NetSession.InCoop = ok;
            if (ok) _discovery?.StopListening(); // stop browsing once we're joining
            return ok;
        }

        public void Shutdown()
        {
            _discovery?.StopAdvertising();
            _discovery?.StopListening();
            if (_nm != null && _nm.IsListening) _nm.Shutdown();
            NetSession.InCoop = false;
        }

        private void Configure(string serverAddress, string listenAddress)
        {
            if (_transport != null)
                _transport.SetConnectionData(serverAddress, port, listenAddress);
        }
    }
}
