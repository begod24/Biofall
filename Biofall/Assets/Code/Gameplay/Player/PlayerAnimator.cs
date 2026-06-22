using UnityEngine;

namespace Biofall.Gameplay
{
    [RequireComponent(typeof(PlayerInput))]
    public sealed class PlayerAnimator : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [Tooltip("Smoothing time for the blend params (seconds).")]
        [SerializeField] private float damp = 0.1f;

        private PlayerInput _input;

        private static readonly int MoveXId = Animator.StringToHash("MoveX");
        private static readonly int MoveYId = Animator.StringToHash("MoveY");
        private static readonly int SpeedId = Animator.StringToHash("Speed");

        private void Awake()
        {
            _input = GetComponent<PlayerInput>();
            if (animator == null) animator = GetComponentInChildren<Animator>();
        }

        private void Update()
        {
            if (animator == null) return;

            Vector3 world = new Vector3(_input.Move.x, 0f, _input.Move.y);
            if (world.sqrMagnitude > 1f) world.Normalize();

            Vector3 local = transform.InverseTransformDirection(world);

            float dt = Time.deltaTime;
            animator.SetFloat(MoveXId, local.x, damp, dt);
            animator.SetFloat(MoveYId, local.z, damp, dt);
            animator.SetFloat(SpeedId, world.magnitude, damp, dt);
        }
    }
}
