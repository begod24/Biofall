using UnityEngine;

namespace Biofall.Core
{
    /// <summary>
    /// Bookkeeping tag added automatically by <see cref="PoolService"/> to every
    /// spawned instance. Remembers which prefab it came from so it can be returned
    /// to the correct pool with a single <c>Despawn(gameObject)</c> call.
    /// </summary>
    public sealed class PooledObject : MonoBehaviour
    {
        public GameObject SourcePrefab { get; internal set; }
    }
}
