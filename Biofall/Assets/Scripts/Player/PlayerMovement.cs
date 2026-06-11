using UnityEngine;

namespace Biofall.Player
{
    /// <summary>
    /// Responsive top-down movement on the XZ plane via CharacterController.
    /// Single responsibility: translate input into position. Aim/rotation lives in
    /// <see cref="PlayerAim"/>.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMovement : MonoBehaviour
    {
        [Header("Speed")]
        [SerializeField] private float _moveSpeed = 6f;
        [SerializeField] private float _sprintMultiplier = 1.6f;
        [SerializeField] private float _acceleration = 60f; // units/sec^2 toward target velocity

        [Header("Gravity")]
        [SerializeField] private float _gravity = -20f;

        private CharacterController _controller;
        private PlayerInputReader _input;
        private Vector3 _planarVelocity;
        private float _verticalVelocity;

        public float CurrentSpeed => _planarVelocity.magnitude;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            _input = GetComponent<PlayerInputReader>();
        }

        private void Update()
        {
            Vector2 move = _input.MoveInput;
            Vector3 desiredDir = new Vector3(move.x, 0f, move.y);
            if (desiredDir.sqrMagnitude > 1f) desiredDir.Normalize();

            float targetSpeed = _moveSpeed * (_input.SprintHeld ? _sprintMultiplier : 1f);
            Vector3 targetVelocity = desiredDir * targetSpeed;

            _planarVelocity = Vector3.MoveTowards(_planarVelocity, targetVelocity, _acceleration * Time.deltaTime);

            if (_controller.isGrounded && _verticalVelocity < 0f) _verticalVelocity = -2f;
            _verticalVelocity += _gravity * Time.deltaTime;

            Vector3 motion = _planarVelocity + Vector3.up * _verticalVelocity;
            _controller.Move(motion * Time.deltaTime);
        }
    }
}
