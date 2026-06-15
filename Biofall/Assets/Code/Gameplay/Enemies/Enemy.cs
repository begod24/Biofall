using UnityEngine;
using Biofall.Core;

namespace Biofall.Gameplay
{
    /// <summary>
    /// Zombie brain (Object Pooling + Observer). Reuses the generic <see cref="Health"/> for HP and
    /// IDamageable, drives the Animator (Speed / Attack / Die), chases via <see cref="EnemyMovement"/>,
    /// and damages the player at the Enemy_Attack hit frame (animation event -> <see cref="OnAttackHit"/>).
    /// Publishes <see cref="TargetDamaged"/>/<see cref="TargetDied"/>. Ticked centrally by
    /// <see cref="EnemyManager"/> (no per-enemy Update) so it scales to many zombies.
    /// </summary>
    [RequireComponent(typeof(Health))]
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(AudioSource))]
    [RequireComponent(typeof(EnemyMovement))]
    public sealed class Enemy : MonoBehaviour, IPoolable
    {
        [SerializeField] private EnemyData data;
        [SerializeField] private Animator animator;
        [SerializeField] private EnemyMovement movement;
        [SerializeField] private Collider bodyCollider;
        [SerializeField] private AudioSource audioSource;

        private Health _health;
        private Transform _tf;
        private Transform _playerTf;
        private IDamageable _playerDamageable;

        private bool _dead;
        private bool _inRange;
        private float _attackTimer;
        private float _flashTimer;

        private Renderer[] _renderers;
        private MaterialPropertyBlock _mpb;

        private static readonly int SpeedId = Animator.StringToHash("Speed");
        private static readonly int AttackId = Animator.StringToHash("Attack");
        private static readonly int DieId = Animator.StringToHash("Die");
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        // Global groan throttle so 100+ zombies don't all groan at once (cheap, no per-voice tracking).
        private static float s_nextGroanAllowed;

        private void Awake()
        {
            _tf = transform;
            _health = GetComponent<Health>();
            if (animator == null) animator = GetComponent<Animator>();
            if (audioSource == null) audioSource = GetComponent<AudioSource>();
            if (movement == null) movement = GetComponent<EnemyMovement>();
            if (bodyCollider == null) bodyCollider = GetComponent<Collider>();
            _renderers = GetComponentsInChildren<Renderer>(true);
            _mpb = new MaterialPropertyBlock();
        }

        private void OnEnable()
        {
            _health.Damaged += OnDamaged;
            _health.Died += OnDied;
        }

        private void OnDisable()
        {
            _health.Damaged -= OnDamaged;
            _health.Died -= OnDied;
        }

        // ---- Pooling lifecycle ----
        public void OnSpawned()
        {
            _dead = false;
            _inRange = false;
            _attackTimer = 0f;

            _health.SetMax(data.maxHealth, true);
            if (bodyCollider != null) bodyCollider.enabled = true;

            if (animator != null)
            {
                animator.Rebind();      // reset to Locomotion (clears any leftover death pose on reuse)
                animator.Update(0f);
            }

            movement.Init(data);
            CacheTarget();
            EnemyManager.Instance?.Register(this);
        }

        public void OnDespawned()
        {
            EnemyManager.Instance?.Unregister(this);
        }

        /// <summary>World position (cached transform) — used by the manager for boids separation.</summary>
        public Vector3 Position => _tf.position;

        /// <summary>True once dead (corpse) — excluded from separation and ticking logic.</summary>
        public bool Dead => _dead;

        /// <summary>Boids separation radius (from data) — read by the manager.</summary>
        public float SeparationRadius => data != null ? data.separationRadius : 1f;

        // ---- Central tick (called by EnemyManager) ----
        public void Tick(float dt, Vector3 separation)
        {
            if (_dead) return;

            if (_playerTf == null)
            {
                CacheTarget();
                if (_playerTf == null) return;
            }

            _inRange = movement.Tick(_playerTf.position, separation, dt, out bool moving);
            if (animator != null) animator.SetFloat(SpeedId, moving ? 1f : 0f);

            _attackTimer -= dt;
            if (_inRange && _attackTimer <= 0f)
            {
                _attackTimer = data.attackCooldown;
                if (animator != null) animator.SetTrigger(AttackId);
            }

            if (_flashTimer > 0f)
            {
                _flashTimer -= dt;
                if (_flashTimer <= 0f) SetFlash(false);
            }

            MaybeGroan(dt);
        }

        /// <summary>Animation event fired on the Enemy_Attack hit frame.</summary>
        public void OnAttackHit()
        {
            if (_dead || _playerDamageable == null || _playerTf == null) return;

            // Re-check distance so a hit doesn't land if the player already escaped.
            Vector3 d = _playerTf.position - _tf.position;
            d.y = 0f;
            float reach = data.attackRange * 1.15f;
            if (d.sqrMagnitude > reach * reach) return;

            _playerDamageable.TakeDamage(new DamageInfo(data.attackDamage, _tf.position, _tf.forward, gameObject));
        }

        // ---- Health reactions ----
        private void OnDamaged(DamageInfo info, float current)
        {
            EventBus.Publish(new TargetDamaged(gameObject, current, info.Amount));

            // Blood splatter at the hit point, sprayed along the bullet direction.
            if (data.bloodPrefab != null && PoolService.Instance != null)
            {
                Vector3 dir = info.HitDirection.sqrMagnitude > 0.0001f ? info.HitDirection.normalized : Vector3.up;
                PoolService.Instance.Spawn(data.bloodPrefab, info.HitPoint, Quaternion.LookRotation(dir));
            }

            // Stagger + flash.
            movement.AddKnockback(info.HitDirection.normalized * data.knockbackForce);
            SetFlash(true);
            _flashTimer = data.hitFlashDuration;
        }

        private void OnDied()
        {
            if (_dead) return;
            _dead = true;

            SetFlash(false);
            if (bodyCollider != null) bodyCollider.enabled = false; // corpse stops blocking shots
            if (animator != null) animator.SetTrigger(DieId);
            if (audioSource != null && data.deathSfx != null) audioSource.PlayOneShot(data.deathSfx, data.deathVolume);

            EventBus.Publish(new TargetDied(gameObject));
            DropLoot();

            CancelInvoke();
            Invoke(nameof(Despawn), data.despawnDelay);
        }

        private void DropLoot()
        {
            if (PoolService.Instance == null) return;
            Vector3 origin = _tf.position + Vector3.up * 0.3f;

            if (data.samplePrefab != null)
            {
                int n = Random.Range(data.sampleDropMin, data.sampleDropMax + 1);
                for (int i = 0; i < n; i++)
                {
                    Vector3 off = new Vector3(Random.Range(-0.6f, 0.6f), 0f, Random.Range(-0.6f, 0.6f));
                    PoolService.Instance.Spawn(data.samplePrefab, origin + off, Quaternion.identity);
                }
            }

            if (data.ammoPickupPrefab != null && Random.value < data.ammoDropChance)
                PoolService.Instance.Spawn(data.ammoPickupPrefab, origin, Quaternion.identity);
        }

        private void SetFlash(bool on)
        {
            if (_renderers == null) return;
            foreach (var r in _renderers)
            {
                if (r == null) continue;
                if (on)
                {
                    r.GetPropertyBlock(_mpb);
                    _mpb.SetColor(BaseColorId, data.hitFlashColor);
                    r.SetPropertyBlock(_mpb);
                }
                else
                {
                    r.SetPropertyBlock(null); // clear override → back to material default
                }
            }
        }

        private void Despawn()
        {
            if (PoolService.Instance != null) PoolService.Instance.Despawn(gameObject);
            else gameObject.SetActive(false);
        }

        // ---- Helpers ----
        private void CacheTarget()
        {
            _playerTf = PlayerRegistry.Player;
            _playerDamageable = _playerTf != null ? _playerTf.GetComponentInParent<IDamageable>() : null;
        }

        private void MaybeGroan(float dt)
        {
            if (audioSource == null || data.groanSfx == null || data.groanSfx.Length == 0) return;
            if (Time.time < s_nextGroanAllowed) return;
            if (Random.value < data.groanChancePerSecond * dt)
            {
                s_nextGroanAllowed = Time.time + data.globalGroanInterval;
                AudioClip clip = data.groanSfx[Random.Range(0, data.groanSfx.Length)];
                if (clip != null) audioSource.PlayOneShot(clip, data.groanVolume);
            }
        }
    }
}
