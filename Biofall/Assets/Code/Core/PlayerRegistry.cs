using System.Collections.Generic;
using UnityEngine;

namespace Biofall.Core
{
    /// <summary>
    /// Single source of truth for "who are the players". Enemies/systems ask here instead of
    /// FindObjectOfType every frame. Players register on spawn, unregister on destroy.
    ///
    /// SOLO: exactly one player registers → it becomes <see cref="LocalPlayer"/> and
    /// <see cref="Player"/>, so all existing single-player code keeps working unchanged.
    /// CO-OP: every player (local + remote replicas) registers into <see cref="All"/>; the
    /// machine's own player is set as <see cref="LocalPlayer"/> explicitly via <see cref="SetLocal"/>
    /// (so the camera/HUD follow the owned player, not a remote one). Enemy targeting uses
    /// <see cref="Nearest"/> over all players.
    /// </summary>
    public static class PlayerRegistry
    {
        private static readonly List<Transform> _players = new(4);

        /// <summary>This machine's own player (the one local input controls). Solo: the only player.</summary>
        public static Transform LocalPlayer { get; private set; }

        /// <summary>Back-compatible "the player": prefers the local player, else any registered one.</summary>
        public static Transform Player =>
            LocalPlayer != null ? LocalPlayer : (_players.Count > 0 ? _players[0] : null);

        public static bool HasPlayer => Player != null;

        /// <summary>All registered players (read-only). One in solo, up to N in co-op.</summary>
        public static IReadOnlyList<Transform> All => _players;

        public static void Register(Transform player)
        {
            if (player == null) return;
            if (!_players.Contains(player)) _players.Add(player);
            // Solo (no co-op session): the single registered player is automatically local.
            if (LocalPlayer == null && !Biofall.Net.NetSession.InCoop) LocalPlayer = player;
        }

        /// <summary>Mark which player this machine owns (co-op). Also ensures it's registered.</summary>
        public static void SetLocal(Transform player)
        {
            if (player == null) return;
            if (!_players.Contains(player)) _players.Add(player);
            LocalPlayer = player;
        }

        public static void Unregister(Transform player)
        {
            _players.Remove(player);
            if (LocalPlayer == player)
                LocalPlayer = _players.Count > 0 ? _players[0] : null;
        }

        /// <summary>Convenience: a player position, or <paramref name="fallback"/> if none.</summary>
        public static Vector3 PositionOr(Vector3 fallback)
        {
            Transform p = Player;
            return p != null ? p.position : fallback;
        }

        /// <summary>The registered player closest to <paramref name="position"/> (enemy targeting).</summary>
        public static Transform Nearest(Vector3 position)
        {
            Transform best = null;
            float bestSqr = float.MaxValue;
            for (int i = 0; i < _players.Count; i++)
            {
                Transform p = _players[i];
                if (p == null) continue;
                float sqr = (p.position - position).sqrMagnitude;
                if (sqr < bestSqr) { bestSqr = sqr; best = p; }
            }
            return best;
        }
    }
}
