using UnityEngine;
using Biofall.Core;

namespace Biofall.Gameplay
{
    /// <summary>
    /// Command/SOLID seam between the global <see cref="InputReader"/> and the player's
    /// own systems. Player components (Motor/Aim/Controller) read intent from THIS local
    /// component, not from the global reader — so they don't know where input comes from.
    /// Adds no gameplay rules; it only surfaces "what the player wants" this frame.
    /// </summary>
    public sealed class PlayerInput : MonoBehaviour
    {
        private InputReader _reader;

        /// <summary>WASD direction (x = strafe, y = forward) on the input plane.</summary>
        public Vector2 Move => _reader != null ? _reader.Move : Vector2.zero;

        /// <summary>Mouse position in screen space, for aim raycasts.</summary>
        public Vector2 PointerScreenPosition => _reader != null ? _reader.PointerScreenPosition : Vector2.zero;

        public bool FireHeld => _reader != null && _reader.FireHeld;
        public bool FirePressed => _reader != null && _reader.FirePressed;
        public bool ReloadPressed => _reader != null && _reader.ReloadPressed;

        /// <summary>Weapon slot requested this frame (1/2), else 0.</summary>
        public int WeaponSlot => _reader != null ? _reader.WeaponSlot : 0;

        private void Awake()
        {
            _reader = FindAnyObjectByType<InputReader>();
            if (_reader == null)
                Debug.LogWarning("[PlayerInput] No InputReader in scene — input will be ignored.");
        }
    }
}
