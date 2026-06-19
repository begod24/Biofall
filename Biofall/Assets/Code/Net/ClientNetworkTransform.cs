using Unity.Netcode.Components;
using UnityEngine;

namespace Biofall.Net
{
    /// <summary>
    /// Owner-authoritative NetworkTransform: the player that OWNS this object simulates its own
    /// movement locally and replicates the result (responsive controls). Standard NGO pattern —
    /// a NetworkTransform whose authority is the owner, not the server. Used on the co-op player.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ClientNetworkTransform : NetworkTransform
    {
        protected override bool OnIsServerAuthoritative() => false;
    }
}
