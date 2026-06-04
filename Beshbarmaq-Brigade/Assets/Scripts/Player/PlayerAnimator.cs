using UnityEngine;
using Biofall.Combat;

namespace Biofall.Player
{
    public class PlayerAnimator : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private PlayerController controller;
        [SerializeField] private HealthComponent health;
        [SerializeField] private float damping = 8f;

        private static readonly int MoveX = Animator.StringToHash("MoveX");
        private static readonly int MoveY = Animator.StringToHash("MoveY");
        private static readonly int Speed = Animator.StringToHash("Speed");
        private static readonly int IsSprinting = Animator.StringToHash("IsSprinting");
        private static readonly int Fire = Animator.StringToHash("Fire");
        private static readonly int Dodge = Animator.StringToHash("Dodge");
        private static readonly int Reload = Animator.StringToHash("Reload");
        private static readonly int IsDead = Animator.StringToHash("IsDead");

        private Vector2 smoothed;

        private void OnEnable()
        {
            if (controller != null) controller.OnDodged += HandleDodge;
            if (health != null) health.OnDied += HandleDied;
        }

        private void OnDisable()
        {
            if (controller != null) controller.OnDodged -= HandleDodge;
            if (health != null) health.OnDied -= HandleDied;
        }

        private void Update()
        {
            if (controller == null || animator == null) return;
            Vector2 raw = controller.MoveInput;
            Vector3 worldDir = new Vector3(raw.x, 0f, raw.y);
            Vector3 localDir = transform.parent != null
                ? transform.parent.InverseTransformDirection(worldDir)
                : transform.InverseTransformDirection(worldDir);
            Vector2 target = new Vector2(localDir.x, localDir.z);
            smoothed = Vector2.Lerp(smoothed, target, Time.deltaTime * damping);
            animator.SetFloat(MoveX, smoothed.x);
            animator.SetFloat(MoveY, smoothed.y);
            animator.SetFloat(Speed, smoothed.magnitude);
            animator.SetBool(IsSprinting, controller.IsSprinting);
        }

        public void TriggerFire()
        {
            if (animator != null) animator.SetTrigger(Fire);
        }

        public void TriggerReload()
        {
            if (animator != null) animator.SetTrigger(Reload);
        }

        private void HandleDodge()
        {
            if (animator != null) animator.SetTrigger(Dodge);
        }

        private void HandleDied(HealthComponent _)
        {
            if (animator != null) animator.SetBool(IsDead, true);
        }
    }
}
