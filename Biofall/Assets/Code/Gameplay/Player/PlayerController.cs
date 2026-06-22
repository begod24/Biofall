using UnityEngine;
using Biofall.Core;

namespace Biofall.Gameplay
{
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
            if (Time.timeScale <= 0f) return;
            _motor.Move(_input.Move);
            _aim.AimAt(_input.PointerScreenPosition);
        }
    }
}
