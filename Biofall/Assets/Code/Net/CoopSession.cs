using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using Biofall.Core;

namespace Biofall.Net
{
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

    public sealed class CoopSession : NetworkBehaviour
    {
        public static CoopSession Instance { get; private set; }

        [Tooltip("Scene loaded (networked) when the host starts the match.")]
        [SerializeField] private string gameScene = GameScenes.MissionCoop;

        public NetworkList<LobbySlot> Slots;

        public event Action SlotsChanged;

        private bool _gameStarted;

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
                AddSlot(NetworkManager.LocalClientId);
                NetworkManager.OnClientConnectedCallback += OnClientConnected;
                NetworkManager.OnClientDisconnectCallback += OnClientDisconnected;
                NetworkManager.SceneManager.OnSceneEvent += OnSceneEventServer;
            }
        }

        public override void OnNetworkDespawn()
        {
            if (IsServer)
            {
                NetworkManager.OnClientConnectedCallback -= OnClientConnected;
                NetworkManager.OnClientDisconnectCallback -= OnClientDisconnected;
                if (NetworkManager != null && NetworkManager.SceneManager != null)
                    NetworkManager.SceneManager.OnSceneEvent -= OnSceneEventServer;
            }
            if (Instance == this) Instance = null;
        }

        private void OnClientConnected(ulong id)
        {
            if (!IsServer) return;
            AddSlot(id);
            if (_gameStarted && SceneManager.GetActiveScene().name == gameScene)
                StartCoroutine(SpawnPlayerForClientNextFrame(id));
        }

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

        public void StartGame()
        {
            if (!IsServer) return;
            _gameStarted = true;
            NetworkManager.SceneManager.LoadScene(gameScene, LoadSceneMode.Single);
        }

        private void OnSceneEventServer(SceneEvent sceneEvent)
        {
            if (!IsServer) return;
            if (sceneEvent.SceneName != gameScene) return;
            if (sceneEvent.SceneEventType != SceneEventType.LoadEventCompleted) return;

            _gameStarted = true;
            StartCoroutine(SpawnMissionPlayersNextFrame());
        }

        private IEnumerator SpawnMissionPlayersNextFrame()
        {
            yield return null;
            foreach (var client in NetworkManager.ConnectedClientsList)
                SpawnPlayerForClient(client.ClientId);
        }

        private IEnumerator SpawnPlayerForClientNextFrame(ulong clientId)
        {
            yield return null;
            SpawnPlayerForClient(clientId);
        }

        private void SpawnPlayerForClient(ulong clientId)
        {
            if (!IsServer) return;
            if (NetworkManager.ConnectedClients.TryGetValue(clientId, out var client) && client.PlayerObject != null)
                return;

            GameObject prefab = NetworkBootstrap.Instance != null ? NetworkBootstrap.Instance.PlayerPrefab : null;
            if (prefab == null)
            {
                Debug.LogError("[CoopSession] Missing co-op player prefab; cannot spawn player.");
                return;
            }

            Vector3 spawn = CoopPlayer.ResolveMissionSpawnPosition(clientId);
            var go = Instantiate(prefab, spawn, Quaternion.identity);
            go.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId, destroyWithScene: true);
        }
    }
}
