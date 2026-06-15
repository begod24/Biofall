using UnityEngine;

namespace Biofall.Core
{
    /// <summary>
    /// Single source of truth for "who is the player". Enemies/systems ask here
    /// instead of calling FindObjectOfType every frame. The player registers
    /// itself on spawn and unregisters on destroy.
    /// </summary>
    public static class PlayerRegistry
    {
        public static Transform Player { get; private set; }

        public static bool HasPlayer => Player != null;

        public static void Register(Transform player)
        {
            Player = player;
        }

        public static void Unregister(Transform player)
        {
            if (Player == player) Player = null;
        }

        /// <summary>Convenience: player position, or <paramref name="fallback"/> if none registered.</summary>
        public static Vector3 PositionOr(Vector3 fallback)
        {
            return Player != null ? Player.position : fallback;
        }
    }
}
