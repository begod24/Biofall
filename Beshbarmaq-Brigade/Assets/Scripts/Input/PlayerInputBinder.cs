using UnityEngine;
using UnityEngine.InputSystem;
using System;

namespace Biofall.Player
{
    public class PlayerInputBinder : MonoBehaviour
    {
        public InputAction Move { get; private set; }
        public InputAction Aim { get; private set; }
        public InputAction Fire { get; private set; }
        public InputAction Sprint { get; private set; }
        public InputAction Dodge { get; private set; }
        public InputAction Interact { get; private set; }
        public InputAction Reload { get; private set; }
        public InputAction NextWeapon { get; private set; }
        public InputAction PrevWeapon { get; private set; }

        public bool UsingGamepad { get; private set; }
        public event Action<bool> OnDeviceChanged;

        private InputDevice lastDevice;

        private void Awake()
        {
            Move = new InputAction("Move", InputActionType.Value, expectedControlType: "Vector2");
            Move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");
            Move.AddBinding("<Gamepad>/leftStick");

            Aim = new InputAction("Aim", InputActionType.Value, expectedControlType: "Vector2");
            Aim.AddBinding("<Mouse>/position");
            Aim.AddBinding("<Gamepad>/rightStick");

            Fire = new InputAction("Fire", InputActionType.Button);
            Fire.AddBinding("<Mouse>/leftButton");
            Fire.AddBinding("<Gamepad>/rightTrigger");

            Sprint = new InputAction("Sprint", InputActionType.Button);
            Sprint.AddBinding("<Keyboard>/leftShift");
            Sprint.AddBinding("<Gamepad>/leftStickPress");

            Dodge = new InputAction("Dodge", InputActionType.Button);
            Dodge.AddBinding("<Keyboard>/space");
            Dodge.AddBinding("<Gamepad>/buttonSouth");

            Interact = new InputAction("Interact", InputActionType.Button);
            Interact.AddBinding("<Keyboard>/e");
            Interact.AddBinding("<Gamepad>/buttonWest");

            Reload = new InputAction("Reload", InputActionType.Button);
            Reload.AddBinding("<Keyboard>/r");
            Reload.AddBinding("<Gamepad>/buttonNorth");

            NextWeapon = new InputAction("NextWeapon", InputActionType.Button);
            NextWeapon.AddBinding("<Keyboard>/q");
            NextWeapon.AddBinding("<Gamepad>/dpad/right");

            PrevWeapon = new InputAction("PrevWeapon", InputActionType.Button);
            PrevWeapon.AddBinding("<Keyboard>/tab");
            PrevWeapon.AddBinding("<Gamepad>/dpad/left");
        }

        private void OnEnable()
        {
            Move.Enable(); Aim.Enable(); Fire.Enable(); Sprint.Enable();
            Dodge.Enable(); Interact.Enable(); Reload.Enable();
            NextWeapon.Enable(); PrevWeapon.Enable();
            InputSystem.onActionChange += HandleActionChange;
        }

        private void OnDisable()
        {
            Move.Disable(); Aim.Disable(); Fire.Disable(); Sprint.Disable();
            Dodge.Disable(); Interact.Disable(); Reload.Disable();
            NextWeapon.Disable(); PrevWeapon.Disable();
            InputSystem.onActionChange -= HandleActionChange;
        }

        private void OnDestroy()
        {
            Move?.Dispose(); Aim?.Dispose(); Fire?.Dispose(); Sprint?.Dispose();
            Dodge?.Dispose(); Interact?.Dispose(); Reload?.Dispose();
            NextWeapon?.Dispose(); PrevWeapon?.Dispose();
        }

        private void HandleActionChange(object obj, InputActionChange change)
        {
            if (change != InputActionChange.ActionPerformed) return;
            if (!(obj is InputAction action)) return;
            var device = action.activeControl?.device;
            if (device == null || device == lastDevice) return;
            lastDevice = device;
            bool gamepad = device is Gamepad;
            if (gamepad != UsingGamepad)
            {
                UsingGamepad = gamepad;
                OnDeviceChanged?.Invoke(UsingGamepad);
            }
        }
    }
}
