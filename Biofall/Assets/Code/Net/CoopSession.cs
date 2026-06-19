using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Biofall.Net
{
    /// <summary>One player's lobby slot, replicated in the <see cref="CoopSession"/> NetworkList.</summary>
    public struct LobbySlot : INetworkSerializable, IEquatable<LobbySlot>
    {
        public ulong ClientId;
        public bool Ready;

        public void NetworkSerialize<T>(BufferSerializer<T> s) where T : IReaderWriter
        {
            s.SerializeValue(ref ClientId);
            s.SerializeValue(ref Ready);
        }

        public bool Equals(LobbySlot o) => ClientId == o.ClientId && Ready == o.Ready;
    }

    /// <summary>
    /// Server-authoritative co-op lobby state, spawned by the host when it starts. Tracks a
    /// replicated list of connected players + their ready flag (<see cref="Slots"/>), lets any
    /// client toggle ready (<see cref="ToggleReadyRpc"/>), and lets the host start the match —
    /// a networked scene load that brings everyone into the game scene together. Persists across
    /// that load (DontDestroyOnLoad). UI (dev HUD now, real lobby panel next) only reads/sends.
    /// </summary>
    public sealed class CoopSession : NetworkBehaviour
    {
        public static CoopSession Instance { get; private set; }

        [Tooltip("Scene loaded (networked) when the host starts the match.")]
        [SerializeField] private string gameScene = "CoopArena";

        public NetworkList<LobbySlot> Slots;

        /// <summary>Raised (all peers) whenever the slot list changes — UI refresh hook.</summary>
        public event Action SlotsChanged;

        private void Awake()
        {
            Slots = new NetworkList<LobbySlot>();
        }

        public override void OnNetworkSpawn()
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Slots.OnListChanged += _ => SlotsChanged?.Invoke();

            if (IsServer)
            {
                AddSlot(NetworkManager.LocalClientId); // host's own slot
                NetworkManager.OnClientConnectedCallback += OnClientConnected;
                NetworkManager.OnClientDisconnectCallback += OnClientDisconnected;
            }
        }

        public override void OnNetworkDespawn()
        {
            if (IsServer)
            {
                NetworkManager.OnClientConnectedCallback -= OnClientConnected;
                NetworkManager.OnClientDisconnectCallback -= OnClientDisconnected;
            }
            if (Instance == this) Instance = null;
        }

        private void OnClientConnected(ulong id) { if (IsServer) AddSlot(id); }
        private void OnClientDisconnected(ulong id) { if (IsServer) RemoveSlot(id); }

        private void AddSlot(ulong id)
        {
            foreach (var s in Slots) if (s.ClientId == id) return;
            Slots.Add(new LobbySlot { ClientId = id, Ready = false });
        }

        private void RemoveSlot(ulong id)
        {
            for (int i = 0; i < Slots.Count; i++)
                if (Slots[i].ClientId == id) { Slots.RemoveAt(i); return; }
        }

        /// <summary>Any client flips its own ready flag (server applies it).</summary>
        [Rpc(SendTo.Server)]
        public void ToggleReadyRpc(RpcParams rpc = default)
        {
            ulong id = rpc.Receive.SenderClientId;
            for (int i = 0; i < Slots.Count; i++)
                if (Slots[i].ClientId == id)
                {
                    var s = Slots[i];
                    s.Ready = !s.Ready;
                    Slots[i] = s;
                    return;
                }
        }

        public bool AllReady()
        {
            if (Slots.Count == 0) return false;
            foreach (var s in Slots) if (!s.Ready) return false;
            return true;
        }

        /// <summary>Host-only: load the game scene for everyone (networked scene management).</summary>
        public void StartGame()
        {
            if (!IsServer) return;
            NetworkManager.SceneManager.LoadScene(gameScene, LoadSceneMode.Single);
        }
    }
}
