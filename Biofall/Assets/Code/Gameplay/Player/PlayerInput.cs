using UnityEngine;
using Biofall.Core;

namespace Biofall.Gameplay
{
    /// <summary>
    /// Command/SOLID seam between the global <see cref="InputReader"/> and the player's
    /// own systems. Player components (Motor/Aim/Controller) read intent from THIS local
    /// component, not from the global reader — so they don't know where input comes from.
    /// Adds no gameplay rules; it only surfaces "what the player wants" this frame.
    /// The reader is re-acquired lazily: after a (networked) scene load the previous scene's
    /// InputReader is destroyed, so a player that persisted across the load must rebind to the
    /// new scene's reader — otherwise input silently goes dead (can't move/shoot).
    /// </summary>
    public sealed class PlayerInput : MonoBehaviour
    {
        private InputReader _reader;

        /// <summary>Current InputReader, re-found if the cached one was destroyed (scene change).</summary>
        private InputReader Reader
        {
            get
            {
                if (_reader == null) _reader = FindAnyObjectByType<InputReader>();
                return _reader;
            }
        }

        /// <summary>WASD direction (x = strafe, y = forward) on the input plane.</summary>
        public Vector2 Move => Reader != null ? Reader.Move : Vector2.zero;

        /// <summary>Mouse position in screen space, for aim raycasts.</summary>
        public Vector2 PointerScreenPosition => Reader != null ? Reader.PointerScreenPosition : Vector2.zero;

        public bool FireHeld => Reader != null && Reader.FireHeld;
        public bool FirePressed => Reader != null && Reader.FirePressed;
        public bool ReloadPressed => Reader != null && Reader.ReloadPressed;

        /// <summary>Throw-grenade requested this frame (G).</summary>
        public bool GrenadePressed => Reader != null && Reader.GrenadePressed;

        /// <summary>Interact / use requested this frame (E).</summary>
        public bool InteractPressed => Reader != null && Reader.InteractPressed;

        /// <summary>Weapon slot requested this frame (1/2), else 0.</summary>
        public int WeaponSlot => Reader != null ? Reader.WeaponSlot : 0;

        private void Awake()
        {
            _reader = FindAnyObjectByType<InputReader>();
        }
    }
}
