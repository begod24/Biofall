using UnityEngine;

namespace Biofall.Gameplay
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerMotor : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float gravity = -20f;

        private CharacterController _controller;
        private float _verticalVelocity;
        private float _speedMultiplier = 1f;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
        }

        public void ResetVerticalVelocity() => _verticalVelocity = 0f;

        public void SetSpeedMultiplier(float multiplier) => _speedMultiplier = Mathf.Max(0.1f, multiplier);

        public void Move(Vector2 input)
        {
            Vector3 planar = new Vector3(input.x, 0f, input.y);
            if (planar.sqrMagnitude > 1f) planar.Normalize();

            if (_controller.isGrounded && _verticalVelocity < 0f)
                _verticalVelocity = -2f;
            _verticalVelocity += gravity * Time.deltaTime;

            Vector3 velocity = planar * (moveSpeed * _speedMultiplier) + Vector3.up * _verticalVelocity;
            _controller.Move(velocity * Time.deltaTime);
        }
    }
}
