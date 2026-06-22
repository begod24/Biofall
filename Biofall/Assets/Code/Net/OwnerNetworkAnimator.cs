using Unity.Netcode.Components;
using UnityEngine;

namespace Biofall.Net
{
    [DisallowMultipleComponent]
    public sealed class OwnerNetworkAnimator : NetworkAnimator
    {
        protected override bool OnIsServerAuthoritative() => false;
    }
}
