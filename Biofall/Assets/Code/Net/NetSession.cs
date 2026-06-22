using Unity.Netcode;

namespace Biofall.Net
{
    /// <summary>
    /// One tiny global truth: "are we in a networked co-op session right now?".
    /// SOLO never turns this on, so every gameplay system can keep its existing
    /// single-player path with a simple <c>if (!NetSession.InCoop) { ...local... }</c>.
    /// Ownership gates become <c>if (NetSession.InCoop &amp;&amp; !IsOwner) return;</c> — meaning
    /// in solo (no NetworkManager running) nothing is gated and everything behaves as today.
    /// Set by <see cref="NetworkBootstrap"/> when a host/client starts and cleared on shutdown.
    /// </summary>
    public static class NetSession
    {
        /// <summary>True while a co-op networked session is live. Solo leaves this false.</summary>
        public static bool InCoop { get; internal set; }

        private static NetworkManager NM => NetworkManager.Singleton;

        public static bool IsServer => InCoop && NM != null && NM.IsServer;
        public static bool IsClient => InCoop && NM != null && NM.IsClient;
        public static bool IsHost   => InCoop && NM != null && NM.IsHost;

        /// <summary>
        /// True when this machine owns gameplay authority for shared state (enemies, loot, mission).
        /// In solo that's always true (no server) so local systems just run.
        /// </summary>
        public static bool HasAuthority => !InCoop || IsServer;
    }
}
