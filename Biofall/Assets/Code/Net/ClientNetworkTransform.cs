using Unity.Netcode.Components;
using UnityEngine;

namespace Biofall.Net
{
    [DisallowMultipleComponent]
    public sealed class ClientNetworkTransform : NetworkTransform
    {
        protected override bool OnIsServerAuthoritative() => false;
    }
}
