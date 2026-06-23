using UnityEngine;
using UnityEngine.InputSystem;

namespace Biofall.Core
{
    [DefaultExecutionOrder(-100)]
    public class InputReader : MonoBehaviour
    {
        public Vector2 Move { get; private set; }

        public Vector2 PointerScreenPosition { get; private set; }

        public bool FireHeld { get; private set; }
        public bool FirePressed { get; private set; }
        public bool ReloadPressed { get; private set; }

        public bool GrenadePressed { get; private set; }

        public bool InteractPressed { get; private set; }

        public bool InteractHeld { get; private set; }

        public int WeaponSlot { get; private set; }

        private void Update()
        {
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;

            if (keyboard == null || mouse == null)
            {
                Move = Vector2.zero;
                FireHeld = FirePressed = ReloadPressed = GrenadePressed = InteractPressed = InteractHeld = false;
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
            InteractHeld = keyboard.eKey.isPressed;

            WeaponSlot = keyboard.digit1Key.wasPressedThisFrame ? 1
                       : keyboard.digit2Key.wasPressedThisFrame ? 2
                       : keyboard.digit3Key.wasPressedThisFrame ? 3
                       : 0;
        }
    }
}
