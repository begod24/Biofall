using UnityEngine;

namespace Biofall.Gameplay
{
    /// <summary>
    /// Encapsulates ONLY planar movement. Knows nothing about input, aim or camera —
    /// it just moves the body when something calls <see cref="Move"/>. Uses a
    /// CharacterController so it slides along colliders and stays grounded.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerMotor : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float gravity = -20f;

        private CharacterController _controller;
        private float _verticalVelocity;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
        }

        /// <summary>
        /// Drive movement for this frame. <paramref name="input"/> is x = strafe, y = forward
        /// in the XY input plane; it maps to world XZ. Magnitude is clamped to 1.
        /// </summary>
        public void Move(Vector2 input)
        {
            Vector3 planar = new Vector3(input.x, 0f, input.y);
            if (planar.sqrMagnitude > 1f) planar.Normalize();

            if (_controller.isGrounded && _verticalVelocity < 0f)
                _verticalVelocity = -2f; // small downforce keeps isGrounded stable
            _verticalVelocity += gravity * Time.deltaTime;

            Vector3 velocity = planar * moveSpeed + Vector3.up * _verticalVelocity;
            _controller.Move(velocity * Time.deltaTime);
        }
    }
}
