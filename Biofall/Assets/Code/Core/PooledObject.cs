using UnityEngine;

namespace Biofall.Core
{
    public sealed class PooledObject : MonoBehaviour
    {
        public GameObject SourcePrefab { get; internal set; }
    }
}
