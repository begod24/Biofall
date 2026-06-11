using UnityEngine;

namespace Biofall.Player
{
    /// <summary>
    /// Resolves the player's facing from whichever aim scheme is active:
    /// KB+M cursor (raycast onto the aim plane) or gamepad right-stick (direct vector).
    /// Rotates the body yaw to face the aim direction. Exposes <see cref="AimDirection"/>
    /// for the weapon to fire along.
    /// </summary>
    public class PlayerAim : MonoBehaviour
    {
        [SerializeField] private float _turnSpeed = 720f; // deg/sec; high = snappy top-down feel
        [Tooltip("Height of the horizontal plane the cursor ray is projected onto (usually the player's pivot height).")]
        [SerializeField] private float _aimPlaneHeight = 0f;

        private PlayerInputReader _input;
        private Camera _camera;

        /// <summary>Normalized world-space direction the player is aiming (XZ plane). Zero if undetermined.</summary>
        public Vector3 AimDirection { get; private set; } = Vector3.forward;

        private void Awake()
        {
            _input = GetComponent<PlayerInputReader>();
        }

        private void OnEnable() => _camera = Camera.main;

        private void Update()
        {
            if (_camera == null) _camera = Camera.main;

            Vector3 dir = _input.CurrentAimScheme == PlayerInputReader.AimScheme.Stick
                ? ResolveStickAim()
                : ResolvePointerAim();

            if (dir.sqrMagnitude > 0.0001f)
            {
                AimDirection = dir.normalized;
                RotateTowards(AimDirection);
            }
        }

        private Vector3 ResolveStickAim()
        {
            Vector2 s = _input.AimStick;
            if (s.sqrMagnitude < 0.04f) return Vector3.zero; // dead zone -> keep last facing
            return new Vector3(s.x, 0f, s.y);
        }

        private Vector3 ResolvePointerAim()
        {
            if (_camera == null) return Vector3.zero;

            Ray ray = _camera.ScreenPointToRay(_input.PointerScreenPosition);
            var plane = new Plane(Vector3.up, new Vector3(0f, _aimPlaneHeight, 0f));
            if (!plane.Raycast(ray, out float enter)) return Vector3.zero;

            Vector3 worldPoint = ray.GetPoint(enter);
            Vector3 dir = worldPoint - transform.position;
            dir.y = 0f;
            return dir;
        }

        private void RotateTowards(Vector3 dir)
        {
            Quaternion target = Quaternion.LookRotation(dir, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, target, _turnSpeed * Time.deltaTime);
        }
    }
}
