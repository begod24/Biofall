using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

namespace Biofall.Net
{
    /// <summary>A co-op host found on the local network.</summary>
    public readonly struct DiscoveredHost
    {
        public readonly string Address;   // host LAN IP to connect to
        public readonly ushort Port;      // game (UnityTransport) port
        public readonly string Name;      // session/host name

        public DiscoveredHost(string address, ushort port, string name)
        {
            Address = address; Port = port; Name = name;
        }
    }

    /// <summary>
    /// LAN lobby discovery over UDP broadcast (NGO has none built in). Host ADVERTISES: it binds
    /// the discovery port and answers broadcast requests with its game port + session name. A
    /// browsing client LISTENS: it broadcasts a request and raises <see cref="HostDiscovered"/> for
    /// every reply (the sender's IP = the host to connect to). Sockets run on background threads;
    /// results are marshalled to the main thread via a queue and surfaced in Update. Falls back
    /// gracefully — if broadcast is blocked (AP isolation), the player can still Join by direct IP.
    /// </summary>
    public sealed class LanDiscovery : MonoBehaviour
    {
        public const ushort DiscoveryPort = 47777;
        private const uint Magic = 0x0B10FA11;   // "BIOFALL" — filters foreign packets
        private const byte MsgRequest = 1;
        private const byte MsgResponse = 2;

        /// <summary>Raised on the main thread when a host replies to a discovery request.</summary>
        public event Action<DiscoveredHost> HostDiscovered;

        private readonly List<DiscoveredHost> _hosts = new();

        /// <summary>De-duplicated hosts found since the last <see cref="ClearHosts"/> (for the lobby browser).</summary>
        public IReadOnlyList<DiscoveredHost> Hosts => _hosts;

        public void ClearHosts() => _hosts.Clear();

        private UdpClient _serverSocket;   // host: listens for requests, replies
        private UdpClient _clientSocket;   // browser: sends requests, listens for replies
        private string _sessionName = "BIOFALL Squad";
        private ushort _gamePort = NetworkBootstrap.DefaultPort;

        private readonly ConcurrentQueue<DiscoveredHost> _found = new();

        // ---- Host side ----

        public void StartAdvertising(string sessionName, ushort gamePort)
        {
            StopAdvertising();
            _sessionName = string.IsNullOrEmpty(sessionName) ? "BIOFALL Squad" : sessionName;
            _gamePort = gamePort;
            try
            {
                _serverSocket = new UdpClient();
                _serverSocket.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                _serverSocket.EnableBroadcast = true;
                _serverSocket.Client.Bind(new IPEndPoint(IPAddress.Any, DiscoveryPort));
                _serverSocket.BeginReceive(OnServerReceive, null);
            }
            catch (Exception e) { Debug.LogWarning($"[LanDiscovery] advertise failed: {e.Message}"); }
        }

        public void StopAdvertising()
        {
            try { _serverSocket?.Close(); } catch { }
            _serverSocket = null;
        }

        private void OnServerReceive(IAsyncResult ar)
        {
            if (_serverSocket == null) return;
            try
            {
                var from = new IPEndPoint(IPAddress.Any, 0);
                byte[] data = _serverSocket.EndReceive(ar, ref from);
                if (IsValid(data, MsgRequest))
                {
                    byte[] reply = BuildResponse();
                    _serverSocket.Send(reply, reply.Length, from);
                }
            }
            catch { /* socket closed or bad packet — ignore */ }
            finally
            {
                try { _serverSocket?.BeginReceive(OnServerReceive, null); } catch { }
            }
        }

        // ---- Client/browser side ----

        public void StartListening()
        {
            StopListening();
            try
            {
                _clientSocket = new UdpClient();
                _clientSocket.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                _clientSocket.EnableBroadcast = true;
                _clientSocket.Client.Bind(new IPEndPoint(IPAddress.Any, 0)); // ephemeral
                _clientSocket.BeginReceive(OnClientReceive, null);
            }
            catch (Exception e) { Debug.LogWarning($"[LanDiscovery] listen failed: {e.Message}"); }
        }

        public void StopListening()
        {
            try { _clientSocket?.Close(); } catch { }
            _clientSocket = null;
        }

        /// <summary>Broadcast a discovery request; hosts reply and fire <see cref="HostDiscovered"/>.</summary>
        public void RefreshHosts()
        {
            if (_clientSocket == null) StartListening();
            try
            {
                byte[] req = BuildRequest();
                _clientSocket.Send(req, req.Length, new IPEndPoint(IPAddress.Broadcast, DiscoveryPort));
            }
            catch (Exception e) { Debug.LogWarning($"[LanDiscovery] refresh failed: {e.Message}"); }
        }

        private void OnClientReceive(IAsyncResult ar)
        {
            if (_clientSocket == null) return;
            try
            {
                var from = new IPEndPoint(IPAddress.Any, 0);
                byte[] data = _clientSocket.EndReceive(ar, ref from);
                if (TryParseResponse(data, out ushort gamePort, out string name))
                    _found.Enqueue(new DiscoveredHost(from.Address.ToString(), gamePort, name));
            }
            catch { }
            finally
            {
                try { _clientSocket?.BeginReceive(OnClientReceive, null); } catch { }
            }
        }

        private void Update()
        {
            while (_found.TryDequeue(out var host))
            {
                bool known = false;
                for (int i = 0; i < _hosts.Count; i++)
                    if (_hosts[i].Address == host.Address && _hosts[i].Port == host.Port) { known = true; break; }
                if (!known) _hosts.Add(host);
                HostDiscovered?.Invoke(host);
            }
        }

        private void OnDestroy()
        {
            StopAdvertising();
            StopListening();
        }

        // ---- packet encoding ----

        private static byte[] BuildRequest()
        {
            using var ms = new System.IO.MemoryStream();
            using var w = new System.IO.BinaryWriter(ms);
            w.Write(Magic); w.Write(MsgRequest);
            return ms.ToArray();
        }

        private byte[] BuildResponse()
        {
            using var ms = new System.IO.MemoryStream();
            using var w = new System.IO.BinaryWriter(ms);
            w.Write(Magic); w.Write(MsgResponse); w.Write(_gamePort);
            byte[] name = Encoding.UTF8.GetBytes(_sessionName);
            w.Write((ushort)name.Length); w.Write(name);
            return ms.ToArray();
        }

        private static bool IsValid(byte[] data, byte expectedType)
        {
            if (data == null || data.Length < 5) return false;
            return BitConverter.ToUInt32(data, 0) == Magic && data[4] == expectedType;
        }

        private static bool TryParseResponse(byte[] data, out ushort gamePort, out string name)
        {
            gamePort = 0; name = "";
            if (!IsValid(data, MsgResponse) || data.Length < 9) return false;
            try
            {
                using var ms = new System.IO.MemoryStream(data);
                using var r = new System.IO.BinaryReader(ms);
                r.ReadUInt32(); r.ReadByte();            // magic + type
                gamePort = r.ReadUInt16();
                ushort len = r.ReadUInt16();
                name = Encoding.UTF8.GetString(r.ReadBytes(len));
                return true;
            }
            catch { return false; }
        }
    }
}
