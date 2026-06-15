using UnityEngine;

namespace Biofall.Gameplay
{
    /// <summary>
    /// Encapsulates aiming: casts a ray from the mouse onto the ground plane at the
    /// body's height and turns the body to face that point. Exposes the world
    /// <see cref="AimPoint"/> for weapons/crosshair to use later. No input logic here —
    /// it's told which screen position to aim at.
    /// </summary>
    public sealed class PlayerAim : MonoBehaviour
    {
        [Header("Aim")]
        [Tooltip("Degrees/sec to turn toward the cursor. 0 = snap instantly.")]
        [SerializeField] private float turnSpeed = 0f;

        private Camera _camera;

        /// <summary>World point under the cursor on the player's ground plane.</summary>
        public Vector3 AimPoint { get; private set; }

        private void Awake()
        {
            _camera = Camera.main;
        }

        /// <summary>Aim the body toward the given screen position (usually the mouse).</summary>
        public void AimAt(Vector2 screenPosition)
        {
            if (_camera == null)
            {
                _camera = Camera.main;
                if (_camera == null) return;
            }

            Ray ray = _camera.ScreenPointToRay(screenPosition);
            Plane ground = new Plane(Vector3.up, new Vector3(0f, transform.position.y, 0f));

            if (!ground.Raycast(ray, out float distance)) return;

            AimPoint = ray.GetPoint(distance);

            Vector3 direction = AimPoint - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f) return;

            Quaternion target = Quaternion.LookRotation(direction);
            transform.rotation = turnSpeed <= 0f
                ? target
                : Quaternion.RotateTowards(transform.rotation, target, turnSpeed * Time.deltaTime);
        }
    }
}
