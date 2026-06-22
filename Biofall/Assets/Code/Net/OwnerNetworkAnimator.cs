using Unity.Netcode.Components;
using UnityEngine;

namespace Biofall.Net
{
    /// <summary>
    /// Owner-authoritative <see cref="NetworkAnimator"/> for the co-op player. Movement is owner-auth
    /// (see <see cref="ClientNetworkTransform"/>), so the Animator params (MoveX/MoveY/Speed) and
    /// triggers (Fire/Reload/Die/Downed) are computed on the OWNER and replicated to everyone else —
    /// without this the remote replicas just slide with no limb animation. Standard NGO pattern:
    /// subclass and flip authority to the owner.
    ///
    /// On remote replicas the local animation drivers (<c>PlayerAnimator</c>, <c>PlayerDeath</c>) are
    /// disabled by <see cref="CoopPlayer"/> so nothing locally fights the synced values; only the
    /// owner drives the params and this component mirrors them.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class OwnerNetworkAnimator : NetworkAnimator
    {
        protected override bool OnIsServerAuthoritative() => false;
    }
}
