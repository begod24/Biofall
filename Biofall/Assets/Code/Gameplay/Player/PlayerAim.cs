using UnityEngine;

namespace Biofall.Gameplay
{
    public sealed class PlayerAim : MonoBehaviour
    {
        [Header("Aim")]
        [Tooltip("Degrees/sec to turn toward the cursor. 0 = snap instantly.")]
        [SerializeField] private float turnSpeed = 0f;

        private Camera _camera;

        public Vector3 AimPoint { get; private set; }

        private void Awake()
        {
            _camera = Camera.main;
        }

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
