using UnityEngine;
using Biofall.Core;

namespace Biofall.Gameplay
{
    /// <summary>
    /// Thin orchestrator (Clean Code / SOLID): wires Input → Motor → Aim each frame and
    /// registers the player in <see cref="PlayerRegistry"/>. Holds NO movement/aim rules
    /// itself — all behaviour lives in the dedicated components.
    /// </summary>
    [RequireComponent(typeof(PlayerInput))]
    [RequireComponent(typeof(PlayerMotor))]
    [RequireComponent(typeof(PlayerAim))]
    public sealed class PlayerController : MonoBehaviour
    {
        private PlayerInput _input;
        private PlayerMotor _motor;
        private PlayerAim _aim;

        private void Awake()
        {
            _input = GetComponent<PlayerInput>();
            _motor = GetComponent<PlayerMotor>();
            _aim = GetComponent<PlayerAim>();
        }

        private void OnEnable()  => PlayerRegistry.Register(transform);
        private void OnDisable() => PlayerRegistry.Unregister(transform);

        private void Update()
        {
            if (Time.timeScale <= 0f) return; // paused — freeze movement & aim
            _motor.Move(_input.Move);
            _aim.AimAt(_input.PointerScreenPosition);
        }
    }
}
