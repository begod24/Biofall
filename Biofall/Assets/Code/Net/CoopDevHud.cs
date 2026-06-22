using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Biofall.Net
{
    public sealed class CoopDevHud : MonoBehaviour
    {
        [SerializeField] private string serverIp = "127.0.0.1";

        private readonly List<DiscoveredHost> _hosts = new();
        private bool _subscribed;

        private void OnEnable() => TrySubscribe();
        private void OnDisable()
        {
            var d = NetworkBootstrap.Instance != null ? NetworkBootstrap.Instance.Discovery : null;
            if (d != null) d.HostDiscovered -= OnHostDiscovered;
            _subscribed = false;
        }

        private void TrySubscribe()
        {
            var d = NetworkBootstrap.Instance != null ? NetworkBootstrap.Instance.Discovery : null;
            if (d != null && !_subscribed) { d.HostDiscovered += OnHostDiscovered; _subscribed = true; }
        }

        private void OnHostDiscovered(DiscoveredHost h)
        {
            foreach (var e in _hosts) if (e.Address == h.Address && e.Port == h.Port) return;
            _hosts.Add(h);
        }

        private void OnGUI()
        {
            var nb = NetworkBootstrap.Instance;
            var nm = NetworkManager.Singleton;

            GUILayout.BeginArea(new Rect(12, 12, 280, 320), GUI.skin.box);
            GUILayout.Label("CO-OP DEV (Phase B)");

            if (nb == null || nm == null) { GUILayout.Label("No NetworkRoot in scene."); GUILayout.EndArea(); return; }
            TrySubscribe();

            if (!nm.IsListening)
            {
                if (GUILayout.Button("Host (advertise on LAN)")) nb.StartHost();

                GUILayout.Space(6);
                GUILayout.Label("LAN lobbies:");
                if (GUILayout.Button("Find Hosts")) { _hosts.Clear(); nb.Discovery.StartListening(); nb.Discovery.RefreshHosts(); }
                foreach (var h in _hosts)
                    if (GUILayout.Button($"Join  {h.Name}  ({h.Address}:{h.Port})"))
                        nb.StartClient(h.Address);

                GUILayout.Space(6);
                GUILayout.BeginHorizontal();
                GUILayout.Label("IP", GUILayout.Width(20));
                serverIp = GUILayout.TextField(serverIp);
                GUILayout.EndHorizontal();
                if (GUILayout.Button("Join by IP")) nb.StartClient(serverIp);
            }
            else
            {
                string role = nm.IsHost ? "HOST" : (nm.IsServer ? "SERVER" : "CLIENT");
                GUILayout.Label($"Role: {role}   Clients: {nm.ConnectedClientsList.Count}");

                var session = CoopSession.Instance;
                if (session != null && session.Slots != null)
                {
                    GUILayout.Label("LOBBY:");
                    foreach (var s in session.Slots)
                    {
                        bool me = s.ClientId == nm.LocalClientId;
                        GUILayout.Label($"  P{s.ClientId}{(me ? " (you)" : "")} — {(s.Ready ? "READY" : "...")}");
                    }
                    if (GUILayout.Button("Toggle Ready")) session.ToggleReadyRpc();
                    if (nm.IsHost)
                    {
                        GUI.enabled = session.AllReady();
                        if (GUILayout.Button("START GAME")) session.StartGame();
                        GUI.enabled = true;
                    }
                }

                if (GUILayout.Button("Shutdown")) { nb.Shutdown(); _hosts.Clear(); }
            }

            GUILayout.EndArea();
        }
    }
}
