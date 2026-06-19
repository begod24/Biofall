using UnityEngine;

namespace Biofall.Gameplay
{
    /// <summary>
    /// Feeds the character Animator's 2D locomotion blend tree. The body turns to face
    /// the cursor (see <see cref="PlayerAim"/>), so movement is converted into the body's
    /// local space: MoveX = strafe (left/right), MoveY = forward/back. This makes the
    /// character strafe and backpedal correctly instead of always playing "walk forward".
    /// Presentation only — it reads intent and drives params, it never moves the body.
    /// </summary>
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

            // Express movement relative to where the body is facing (aim direction).
            Vector3 local = transform.InverseTransformDirection(world);

            float dt = Time.deltaTime;
            animator.SetFloat(MoveXId, local.x, damp, dt);
            animator.SetFloat(MoveYId, local.z, damp, dt);
            animator.SetFloat(SpeedId, world.magnitude, damp, dt);
        }
    }
}
