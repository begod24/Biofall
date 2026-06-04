using UnityEngine;
using UnityEngine.InputSystem;

namespace Biofall.Player
{
    [RequireComponent(typeof(PlayerInputBinder))]
    public class PlayerInteraction : MonoBehaviour
    {
        [SerializeField] private float interactRadius = 1.8f;
        [SerializeField] private LayerMask interactableMask = ~0;

        private PlayerInputBinder input;
        private readonly Collider[] buffer = new Collider[8];

        public IInteractable CurrentTarget { get; private set; }
        public event System.Action<IInteractable> OnTargetChanged;

        private void Awake()
        {
            input = GetComponent<PlayerInputBinder>();
        }

        private void OnEnable()
        {
            input.Interact.performed += HandleInteract;
        }

        private void OnDisable()
        {
            input.Interact.performed -= HandleInteract;
        }

        private void Update()
        {
            FindTarget();
        }

        private void FindTarget()
        {
            int count = Physics.OverlapSphereNonAlloc(transform.position, interactRadius, buffer, interactableMask, QueryTriggerInteraction.Collide);
            IInteractable best = null;
            float bestDist = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                var c = buffer[i];
                if (c.gameObject == gameObject) continue;
                if (!c.TryGetComponent<IInteractable>(out var it)) continue;
                if (!it.CanInteract(gameObject)) continue;
                float d = (c.transform.position - transform.position).sqrMagnitude;
                if (d < bestDist) { best = it; bestDist = d; }
            }
            if (!ReferenceEquals(best, CurrentTarget))
            {
                CurrentTarget = best;
                OnTargetChanged?.Invoke(best);
            }
        }

        private void HandleInteract(InputAction.CallbackContext ctx)
        {
            CurrentTarget?.Interact(gameObject);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.4f);
            Gizmos.DrawWireSphere(transform.position, interactRadius);
        }
    }
}
