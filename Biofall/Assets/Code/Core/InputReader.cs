using UnityEngine;
using UnityEngine.InputSystem;

namespace Biofall.Core
{
    /// <summary>
    /// Reads raw input devices and exposes player intent outward.
    /// Knows nothing about movement, weapons or any gameplay rule — it only
    /// answers "what is the player asking for this frame?". Gameplay systems
    /// read these properties; they never touch Keyboard/Mouse directly.
    /// Continuous state = properties; one-shot intents = "pressed this frame" flags.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class InputReader : MonoBehaviour
    {
        /// <summary>Normalized-ish WASD direction on the XY input plane (x = strafe, y = forward).</summary>
        public Vector2 Move { get; private set; }

        /// <summary>Mouse position in screen space (for aim raycasts).</summary>
        public Vector2 PointerScreenPosition { get; private set; }

        public bool FireHeld { get; private set; }
        public bool FirePressed { get; private set; }
        public bool ReloadPressed { get; private set; }

        /// <summary>Throw-grenade requested this frame (G).</summary>
        public bool GrenadePressed { get; private set; }

        /// <summary>Interact / use requested this frame (E) — generator, beacon, etc.</summary>
        public bool InteractPressed { get; private set; }

        /// <summary>Weapon slot requested this frame: 1 or 2 when pressed, else 0.</summary>
        public int WeaponSlot { get; private set; }

        private void Update()
        {
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;

            if (keyboard == null || mouse == null)
            {
                Move = Vector2.zero;
                FireHeld = FirePressed = ReloadPressed = GrenadePressed = InteractPressed = false;
                WeaponSlot = 0;
                return;
            }

            float x = (keyboard.dKey.isPressed ? 1f : 0f) - (keyboard.aKey.isPressed ? 1f : 0f);
            float y = (keyboard.wKey.isPressed ? 1f : 0f) - (keyboard.sKey.isPressed ? 1f : 0f);
            Move = new Vector2(x, y);

            PointerScreenPosition = mouse.position.ReadValue();
            FireHeld = mouse.leftButton.isPressed;
            FirePressed = mouse.leftButton.wasPressedThisFrame;
            ReloadPressed = keyboard.rKey.wasPressedThisFrame;
            GrenadePressed = keyboard.gKey.wasPressedThisFrame;
            InteractPressed = keyboard.eKey.wasPressedThisFrame;

            WeaponSlot = keyboard.digit1Key.wasPressedThisFrame ? 1
                       : keyboard.digit2Key.wasPressedThisFrame ? 2
                       : 0;
        }
    }
}
