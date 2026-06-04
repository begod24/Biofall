using UnityEngine;
using UnityEngine.InputSystem;
using Biofall.Combat;

namespace Biofall.Player
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(PlayerInputBinder))]
    [RequireComponent(typeof(HealthComponent))]
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 6f;
        [SerializeField] private float sprintMultiplier = 1.5f;
        [SerializeField] private float dodgeForce = 14f;
        [SerializeField] private float dodgeCooldown = 1.2f;
        [SerializeField] private float dodgeDuration = 0.25f;

        private Rigidbody rb;
        private PlayerInputBinder input;
        private HealthComponent health;

        private Vector2 moveInput;
        private bool sprinting;
        private float dodgeTimer;
        private float dodgeEndTime;

        public Vector2 MoveInput => moveInput;
        public bool IsSprinting => sprinting && moveInput.sqrMagnitude > 0.1f;
        public bool IsDodging => Time.time < dodgeEndTime;

        public event System.Action OnDodged;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            input = GetComponent<PlayerInputBinder>();
            health = GetComponent<HealthComponent>();
            rb.constraints = RigidbodyConstraints.FreezeRotation;
        }

        private void OnEnable()
        {
            input.Dodge.performed += HandleDodge;
            health.OnDied += HandleDied;
        }

        private void OnDisable()
        {
            input.Dodge.performed -= HandleDodge;
            health.OnDied -= HandleDied;
        }

        private void Update()
        {
            if (!health.IsAlive) return;
            moveInput = input.Move.ReadValue<Vector2>();
            sprinting = input.Sprint.IsPressed();
            if (dodgeTimer > 0f) dodgeTimer -= Time.deltaTime;
        }

        private void FixedUpdate()
        {
            if (!health.IsAlive) return;
            if (IsDodging) return;
            ApplyMovement();
        }

        private void ApplyMovement()
        {
            Vector3 dir = new Vector3(moveInput.x, 0f, moveInput.y);
            float speed = moveSpeed * (IsSprinting ? sprintMultiplier : 1f);
            Vector3 target = dir.normalized * speed;
            rb.linearVelocity = new Vector3(target.x, rb.linearVelocity.y, target.z);
        }

        private void HandleDodge(InputAction.CallbackContext ctx)
        {
            if (!health.IsAlive || dodgeTimer > 0f) return;
            Vector3 dir = new Vector3(moveInput.x, 0f, moveInput.y).normalized;
            if (dir.sqrMagnitude < 0.1f) dir = transform.forward;
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            rb.AddForce(dir * dodgeForce, ForceMode.VelocityChange);
            dodgeTimer = dodgeCooldown;
            dodgeEndTime = Time.time + dodgeDuration;
            OnDodged?.Invoke();
        }

        private void HandleDied(HealthComponent _)
        {
            rb.linearVelocity = Vector3.zero;
            enabled = false;
        }
    }
}
