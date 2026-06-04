using UnityEngine;

namespace Biofall.Player
{
    [RequireComponent(typeof(PlayerInputBinder))]
    public class PlayerAimer : MonoBehaviour
    {
        [SerializeField] private float rotationLerp = 18f;
        [SerializeField] private Camera referenceCamera;

        private PlayerInputBinder input;
        private Plane aimPlane;

        public Vector3 AimWorldPoint { get; private set; }
        public Vector3 AimDirection { get; private set; }

        private void Awake()
        {
            input = GetComponent<PlayerInputBinder>();
            if (referenceCamera == null) referenceCamera = Camera.main;
            AimDirection = transform.forward;
        }

        private void Update()
        {
            Vector3 lookDir;

            if (input.UsingGamepad)
            {
                Vector2 stick = input.Aim.ReadValue<Vector2>();
                if (stick.sqrMagnitude < 0.15f)
                {
                    AimDirection = transform.forward;
                    AimWorldPoint = transform.position + transform.forward * 5f;
                    return;
                }
                lookDir = new Vector3(stick.x, 0f, stick.y);
                AimWorldPoint = transform.position + lookDir.normalized * 8f;
            }
            else
            {
                if (referenceCamera == null) return;
                aimPlane.SetNormalAndPosition(Vector3.up, transform.position);
                Vector2 mouse = input.Aim.ReadValue<Vector2>();
                Ray ray = referenceCamera.ScreenPointToRay(mouse);
                if (!aimPlane.Raycast(ray, out float dist)) return;
                AimWorldPoint = ray.GetPoint(dist);
                lookDir = AimWorldPoint - transform.position;
                lookDir.y = 0f;
            }

            if (lookDir.sqrMagnitude < 0.001f) return;
            AimDirection = lookDir.normalized;
            Quaternion target = Quaternion.LookRotation(lookDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, target, Time.deltaTime * rotationLerp);
        }
    }
}
