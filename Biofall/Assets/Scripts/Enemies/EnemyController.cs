using Biofall.Combat;
using Biofall.Core;
using Biofall.Pooling;
using UnityEngine;

namespace Biofall.Enemies
{
    /// <summary>
    /// A single zombie. Deliberately has NO Update of its own — <see cref="EnemyManager"/>
    /// ticks the whole swarm in one batched loop. Movement is plain transform steering
    /// (no NavMesh, no dynamic Rigidbody) so hundreds can run at once. Uses the shared
    /// <see cref="HealthComponent"/> and returns to the pool on death.
    /// </summary>
    [RequireComponent(typeof(HealthComponent))]
    public class EnemyController : MonoBehaviour, IPoolable
    {
        [SerializeField] private EnemyData _data;

        private Transform _tf;
        private HealthComponent _health;

        private Transform _target;
        private IDamageable _targetDamageable;
        private float _nextAttackTime;

        public EnemyData Data => _data;
        public Vector3 Position => _tf.position;
        public bool IsActive { get; private set; }

        private void Awake()
        {
            _tf = transform;
            _health = GetComponent<HealthComponent>();
        }

        // --- Pooling lifecycle ---
        public void OnSpawned()
        {
            if (_data != null) _health.Configure(_data.MaxHealth);
            _health.OnDied += HandleDied;
            _nextAttackTime = 0f;
            IsActive = true;

            if (EnemyManager.Exists)
            {
                EnemyManager.Instance.Register(this);
                _target = EnemyManager.Instance.Target;
                _targetDamageable = EnemyManager.Instance.TargetDamageable;
            }
        }

        public void OnDespawned()
        {
            _health.OnDied -= HandleDied;
            IsActive = false;
            if (EnemyManager.Exists) EnemyManager.Instance.Unregister(this);
        }

        /// <summary>Called by the manager once per frame with a precomputed separation vector.</summary>
        public void Tick(float dt, Vector3 separation)
        {
            if (_data == null || _target == null) return;

            Vector3 toTarget = _target.position - _tf.position;
            toTarget.y = 0f;
            float dist = toTarget.magnitude;

            if (dist > _data.AttackRange)
            {
                Vector3 chase = (dist > 1e-4f ? toTarget / dist : Vector3.zero) * _data.ChaseWeight;
                Vector3 steer = chase + separation * _data.SeparationWeight;

                if (steer.sqrMagnitude > 1e-4f)
                {
                    Vector3 moveDir = steer.normalized;
                    _tf.position += moveDir * (_data.MoveSpeed * dt);
                    FaceDirection(moveDir, dt);
                }
            }
            else
            {
                FaceDirection(toTarget.sqrMagnitude > 1e-4f ? toTarget.normalized : _tf.forward, dt);
                TryAttack();
            }
        }

        private void TryAttack()
        {
            if (Time.time < _nextAttackTime || _targetDamageable == null || !_targetDamageable.IsAlive) return;
            _nextAttackTime = Time.time + _data.AttackCooldown;

            Vector3 dir = (_target.position - _tf.position).normalized;
            _targetDamageable.ApplyDamage(new DamageInfo(_data.AttackDamage, _tf.position, -dir, dir, gameObject));
        }

        private void FaceDirection(Vector3 dir, float dt)
        {
            if (dir.sqrMagnitude < 1e-4f) return;
            Quaternion target = Quaternion.LookRotation(dir, Vector3.up);
            _tf.rotation = Quaternion.RotateTowards(_tf.rotation, target, _data.TurnSpeed * dt);
        }

        private void HandleDied(DamageInfo _)
        {
            if (_data != null) GameEvents.RaiseEnemyKilled(_tf.position);
            if (PoolManager.Exists) PoolManager.Instance.Release(gameObject);
            else gameObject.SetActive(false);
        }
    }
}
