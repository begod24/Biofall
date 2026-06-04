using UnityEngine;
using UnityEngine.AI;
using Biofall.Combat;

namespace Biofall.Enemies
{
    [RequireComponent(typeof(HealthComponent))]
    public class EnemyBase : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 3f;
        [SerializeField] private float attackRange = 1.5f;
        [SerializeField] private float attackDamage = 10f;
        [SerializeField] private float attackInterval = 1f;
        [SerializeField] private bool useNavMesh = true;

        private Transform target;
        private NavMeshAgent agent;
        private HealthComponent health;
        private float attackTimer;

        public bool IsAlive => health != null && health.IsAlive;

        public event System.Action<EnemyBase> OnDespawned;

        private void Awake()
        {
            health = GetComponent<HealthComponent>();
            if (useNavMesh) TryGetComponent(out agent);
            if (agent != null)
            {
                agent.speed = moveSpeed;
                agent.stoppingDistance = attackRange * 0.9f;
            }
        }

        private void OnEnable()
        {
            health.ResetHealth();
            health.OnDied += HandleDied;
        }

        private void OnDisable()
        {
            health.OnDied -= HandleDied;
        }

        public void Initialize(Transform playerTarget)
        {
            target = playerTarget;
            attackTimer = 0f;
            if (agent != null && agent.isOnNavMesh) agent.isStopped = false;
        }

        private void Update()
        {
            if (target == null || !health.IsAlive) return;

            float dist = Vector3.Distance(transform.position, target.position);

            if (agent != null && agent.isOnNavMesh)
            {
                agent.SetDestination(target.position);
            }
            else
            {
                Vector3 dir = target.position - transform.position;
                dir.y = 0f;
                if (dist > attackRange)
                    transform.position += dir.normalized * moveSpeed * Time.deltaTime;
                if (dir.sqrMagnitude > 0.001f)
                    transform.rotation = Quaternion.LookRotation(dir);
            }

            attackTimer -= Time.deltaTime;
            if (dist <= attackRange && attackTimer <= 0f) Attack();
        }

        private void Attack()
        {
            attackTimer = attackInterval;
            if (target.TryGetComponent<IDamageable>(out var dmg))
            {
                dmg.TakeDamage(new DamageInfo(attackDamage, gameObject, transform.position, -transform.forward, DamageType.Melee));
            }
        }

        private void HandleDied(HealthComponent _)
        {
            if (agent != null && agent.isOnNavMesh) agent.isStopped = true;
            OnDespawned?.Invoke(this);
            gameObject.SetActive(false);
        }
    }
}
