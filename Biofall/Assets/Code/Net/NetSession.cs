using Unity.Netcode;

namespace Biofall.Net
{
    public static class NetSession
    {
        public static bool InCoop { get; internal set; }

        private static NetworkManager NM => NetworkManager.Singleton;

        public static bool IsServer => InCoop && NM != null && NM.IsServer;
        public static bool IsClient => InCoop && NM != null && NM.IsClient;
        public static bool IsHost   => InCoop && NM != null && NM.IsHost;

        public static bool HasAuthority => !InCoop || IsServer;
    }
}
