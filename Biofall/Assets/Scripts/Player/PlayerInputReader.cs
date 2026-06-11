using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Biofall.Player
{
    /// <summary>
    /// Single source of player input. Builds its action map in code so movement, aim
    /// (BOTH KB+M cursor and gamepad right-stick), fire, reload, dodge, and sprint are
    /// available from day one without depending on a specific .inputactions asset layout.
    ///
    /// Aim is intentionally exposed as raw inputs (mouse screen position OR stick vector);
    /// <see cref="PlayerAim"/> resolves them into a world-space aim direction, because only
    /// it knows the camera and the player's world position.
    /// </summary>
    public class PlayerInputReader : MonoBehaviour
    {
        public enum AimScheme { Pointer, Stick }

        private InputAction _move;
        private InputAction _aimStick;
        private InputAction _aimPointer;
        private InputAction _fire;
        private InputAction _reload;
        private InputAction _dodge;
        private InputAction _sprint;

        // --- Read by gameplay each frame ---
        public Vector2 MoveInput { get; private set; }
        public bool FireHeld { get; private set; }
        public bool SprintHeld { get; private set; }

        /// <summary>Which scheme last produced aim input, so PlayerAim picks the right resolution.</summary>
        public AimScheme CurrentAimScheme { get; private set; } = AimScheme.Pointer;
        /// <summary>Mouse position in screen space (valid when CurrentAimScheme == Pointer).</summary>
        public Vector2 PointerScreenPosition { get; private set; }
        /// <summary>Right-stick vector (valid when CurrentAimScheme == Stick).</summary>
        public Vector2 AimStick { get; private set; }

        public event Action OnFirePressed;
        public event Action OnReloadPressed;
        public event Action OnDodgePressed;

        private void Awake()
        {
            _move = new InputAction("Move", InputActionType.Value, expectedControlType: "Vector2");
            _move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
            _move.AddBinding("<Gamepad>/leftStick");

            _aimStick = new InputAction("AimStick", InputActionType.Value, "<Gamepad>/rightStick", expectedControlType: "Vector2");
            _aimPointer = new InputAction("AimPointer", InputActionType.Value, "<Pointer>/position", expectedControlType: "Vector2");

            _fire = new InputAction("Fire", InputActionType.Button);
            _fire.AddBinding("<Mouse>/leftButton");
            _fire.AddBinding("<Gamepad>/rightTrigger");

            _reload = new InputAction("Reload", InputActionType.Button);
            _reload.AddBinding("<Keyboard>/r");
            _reload.AddBinding("<Gamepad>/buttonWest");

            _dodge = new InputAction("Dodge", InputActionType.Button);
            _dodge.AddBinding("<Keyboard>/space");
            _dodge.AddBinding("<Gamepad>/buttonSouth");

            _sprint = new InputAction("Sprint", InputActionType.Button);
            _sprint.AddBinding("<Keyboard>/leftShift");
            _sprint.AddBinding("<Gamepad>/leftStickPress");

            _fire.performed += _ => OnFirePressed?.Invoke();
            _reload.performed += _ => OnReloadPressed?.Invoke();
            _dodge.performed += _ => OnDodgePressed?.Invoke();

            // Switch aim scheme based on which device the player last touched.
            _aimStick.performed += ctx =>
            {
                if (ctx.ReadValue<Vector2>().sqrMagnitude > 0.04f)
                    CurrentAimScheme = AimScheme.Stick;
            };
            _aimPointer.performed += _ =>
            {
                // Any mouse movement reverts to pointer aiming.
                CurrentAimScheme = AimScheme.Pointer;
            };
        }

        private void OnEnable()
        {
            _move.Enable(); _aimStick.Enable(); _aimPointer.Enable();
            _fire.Enable(); _reload.Enable(); _dodge.Enable(); _sprint.Enable();
        }

        private void OnDisable()
        {
            _move.Disable(); _aimStick.Disable(); _aimPointer.Disable();
            _fire.Disable(); _reload.Disable(); _dodge.Disable(); _sprint.Disable();
        }

        private void Update()
        {
            MoveInput = _move.ReadValue<Vector2>();
            AimStick = _aimStick.ReadValue<Vector2>();
            PointerScreenPosition = _aimPointer.ReadValue<Vector2>();
            FireHeld = _fire.IsPressed();
            SprintHeld = _sprint.IsPressed();
        }

        private void OnDestroy()
        {
            _move?.Dispose(); _aimStick?.Dispose(); _aimPointer?.Dispose();
            _fire?.Dispose(); _reload?.Dispose(); _dodge?.Dispose(); _sprint?.Dispose();
        }
    }
}
