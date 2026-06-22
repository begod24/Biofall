using System.Collections;
using UnityEngine;
using Biofall.Core;
using Biofall.Net;

namespace Biofall.Gameplay
{
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
        [Tooltip("Floating world-space HP bar (auto-found in children if left empty). Optional.")]
        [SerializeField] private EnemyHealthBar healthBar;

        [Header("Death dissolve")]
        [Tooltip("Delay after death before the corpse starts burning away (lets the death anim play).")]
        [SerializeField] private float dissolveDelay = 0.55f;
        [SerializeField] private float dissolveDuration = 1.1f;
        [SerializeField] private float dissolveNoiseScale = 9f;
        [SerializeField] private float dissolveEdgeWidth = 0.08f;
        [ColorUsage(true, true)]
        [SerializeField] private Color dissolveEdgeColor = new Color(4f, 1.2f, 0.25f, 1f);

        private Health _health;
        private Transform _tf;
        private Transform _playerTf;
        private IDamageable _playerDamageable;

        private bool _dead;
        private bool _inRange;
        private bool _aggroed;
        private float _attackTimer;
        private float _flashTimer;
        private int _attackId;

        private Renderer[] _renderers;
        private MaterialPropertyBlock _mpb;

        private Material[][] _originalMats;
        private Material[][] _dissolveMats;
        private bool _dissolveSupported = true;

        public event System.Action DespawnRequested;

        public event System.Action AttackTriggered;

        public bool SuppressDespawn { get; set; }

        private static readonly int SpeedId = Animator.StringToHash("Speed");
        private static readonly int DieId = Animator.StringToHash("Die");
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
        private static readonly int DissolveAmountId = Shader.PropertyToID("_DissolveAmount");
        private static readonly int EdgeColorId = Shader.PropertyToID("_EdgeColor");
        private static readonly int EdgeWidthId = Shader.PropertyToID("_EdgeWidth");
        private static readonly int NoiseScaleId = Shader.PropertyToID("_NoiseScale");

        private static float s_nextGroanAllowed;

        private void Awake()
        {
            _tf = transform;
            _health = GetComponent<Health>();
            if (animator == null) animator = GetComponent<Animator>();
            if (audioSource == null) audioSource = GetComponent<AudioSource>();
            if (movement == null) movement = GetComponent<EnemyMovement>();
            if (bodyCollider == null) bodyCollider = GetComponent<Collider>();
            if (healthBar == null) healthBar = GetComponentInChildren<EnemyHealthBar>(true);
            _renderers = GetComponentsInChildren<Renderer>(true);
            _mpb = new MaterialPropertyBlock();
            CacheOriginalMaterials();
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

        public void OnSpawned()
        {
            StopAllCoroutines();
            RestoreMaterials();

            _dead = false;
            _inRange = false;
            _aggroed = false;
            _attackTimer = 0f;
            _attackId = Animator.StringToHash(string.IsNullOrEmpty(data.attackTrigger) ? "Attack" : data.attackTrigger);

            _health.SetMax(data.maxHealth, true);
            if (bodyCollider != null) bodyCollider.enabled = true;

            if (animator != null)
            {
                animator.Rebind();
                animator.Update(0f);
            }

            movement.Init(data);
            CacheTarget();
            if (healthBar != null) healthBar.ResetBar();
            EnemyManager.Instance?.Register(this);
        }

        public void OnDespawned()
        {
            EnemyManager.Instance?.Unregister(this);
        }

        public EnemyData Data => data;

        public Vector3 Position => _tf.position;

        public bool Dead => _dead;

        public float SeparationRadius => data != null ? data.separationRadius : 1f;

        public bool Aggroed => _aggroed;

        public float AggroRadius => data != null ? data.aggroRadius : 10f;

        public void Aggro() { if (!_dead) _aggroed = true; }

        public void Tick(float dt, Vector3 separation)
        {
            if (_dead) return;

            CacheTarget();
            if (_playerTf == null) return;

            if (!_aggroed)
            {
                Vector3 toPlayer = _playerTf.position - _tf.position;
                toPlayer.y = 0f;
                if (toPlayer.sqrMagnitude <= data.aggroRadius * data.aggroRadius) _aggroed = true;
            }

            _inRange = movement.Tick(_aggroed, _playerTf.position, separation, dt, out bool moving);
            if (animator != null) animator.SetFloat(SpeedId, moving ? 1f : 0f);

            _attackTimer -= dt;
            if (_inRange && _attackTimer <= 0f)
            {
                _attackTimer = data.attackCooldown;
                if (animator != null) animator.SetTrigger(_attackId);
                AttackTriggered?.Invoke();
            }

            TickFlash(dt);

            MaybeGroan(dt);
        }

        private void TickFlash(float dt)
        {
            if (_flashTimer > 0f)
            {
                _flashTimer -= dt;
                if (_flashTimer <= 0f) SetFlash(false);
            }
        }

        public void ClientFxTick(float dt)
        {
            TickFlash(dt);
            MaybeGroan(dt);
        }

        public void PlayAttackFx()
        {
            if (animator == null) return;
            if (_attackId == 0)
                _attackId = Animator.StringToHash(data != null && !string.IsNullOrEmpty(data.attackTrigger) ? data.attackTrigger : "Attack");
            animator.SetTrigger(_attackId);
        }

        public void OnAttackHit()
        {
            if (_dead || _playerDamageable == null || _playerTf == null) return;

            Vector3 d = _playerTf.position - _tf.position;
            d.y = 0f;
            float reach = data.attackRange * 1.15f;
            if (d.sqrMagnitude > reach * reach) return;

            if (NetSession.InCoop)
            {
                var coopPlayer = _playerTf.GetComponentInParent<CoopPlayer>();
                if (coopPlayer != null) coopPlayer.TakeDamageRpc(data.attackDamage, _tf.position);
                return;
            }

            _playerDamageable.TakeDamage(new DamageInfo(data.attackDamage, _tf.position, _tf.forward, gameObject));
        }

        private void OnDamaged(DamageInfo info, float current)
        {
            EventBus.Publish(new TargetDamaged(gameObject, current, info.Amount));
            PlayHitFx(info);
            if (healthBar != null && _health.Max > 0f) healthBar.Set(current / _health.Max);
            movement.AddKnockback(info.HitDirection.normalized * data.knockbackForce);
        }

        public void SetHealthBar(float fraction)
        {
            if (healthBar != null) healthBar.Set(fraction);
        }

        public void PlayHitFx(in DamageInfo info)
        {
            if (data.bloodPrefab != null && PoolService.Instance != null)
            {
                Vector3 dir = info.HitDirection.sqrMagnitude > 0.0001f ? info.HitDirection.normalized : Vector3.up;
                PoolService.Instance.Spawn(data.bloodPrefab, info.HitPoint, Quaternion.LookRotation(dir));
            }
            SetFlash(true);
            _flashTimer = data.hitFlashDuration;
        }

        private void OnDied()
        {
            if (_dead) return;
            EventBus.Publish(new TargetDied(gameObject));
            PlayDeathFx();
        }

        public void PlayDeathFx()
        {
            if (_dead) return;
            _dead = true;

            SetFlash(false);
            if (healthBar != null) healthBar.Hide();
            if (bodyCollider != null) bodyCollider.enabled = false;
            if (animator != null) animator.SetTrigger(DieId);
            if (audioSource != null && data.deathSfx != null) audioSource.PlayOneShot(data.deathSfx, data.deathVolume);

            if (data.bloodPrefab != null && PoolService.Instance != null)
            {
                Vector3 chest = _tf.position + Vector3.up * 1f;
                for (int i = 0; i < 3; i++)
                {
                    Vector3 dir = (Vector3.up * 2.2f + Random.insideUnitSphere).normalized;
                    PoolService.Instance.Spawn(data.bloodPrefab, chest, Quaternion.LookRotation(dir));
                }
            }

            CancelInvoke();
            if (_dissolveSupported && BuildDissolveMaterials())
                StartCoroutine(DissolveAndDespawn());
            else
                Invoke(nameof(Despawn), data.despawnDelay);
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
                    r.SetPropertyBlock(null);
                }
            }
        }

        private void CacheOriginalMaterials()
        {
            if (_renderers == null) return;
            _originalMats = new Material[_renderers.Length][];
            for (int i = 0; i < _renderers.Length; i++)
                _originalMats[i] = _renderers[i] != null ? _renderers[i].sharedMaterials : null;
        }

        private bool BuildDissolveMaterials()
        {
            if (_dissolveMats != null) return true;
            Shader sh = Shader.Find("Biofall/Dissolve");
            if (sh == null) { _dissolveSupported = false; return false; }

            _dissolveMats = new Material[_renderers.Length][];
            for (int i = 0; i < _renderers.Length; i++)
            {
                var r = _renderers[i];
                var src = _originalMats != null ? _originalMats[i] : null;
                if (r == null || src == null) { _dissolveMats[i] = null; continue; }

                var dst = new Material[src.Length];
                for (int m = 0; m < src.Length; m++)
                {
                    var dm = new Material(sh) { hideFlags = HideFlags.HideAndDontSave };
                    var s = src[m];
                    if (s != null)
                    {
                        if (s.HasProperty(BaseMapId)) dm.SetTexture(BaseMapId, s.GetTexture(BaseMapId));
                        if (s.HasProperty(BaseColorId)) dm.SetColor(BaseColorId, s.GetColor(BaseColorId));
                    }
                    dm.SetColor(EdgeColorId, dissolveEdgeColor);
                    dm.SetFloat(EdgeWidthId, dissolveEdgeWidth);
                    dm.SetFloat(NoiseScaleId, dissolveNoiseScale);
                    dm.SetFloat(DissolveAmountId, 0f);
                    dst[m] = dm;
                }
                _dissolveMats[i] = dst;
            }
            return true;
        }

        private void ApplyDissolveMaterials()
        {
            if (_dissolveMats == null) return;
            for (int i = 0; i < _renderers.Length; i++)
                if (_renderers[i] != null && _dissolveMats[i] != null)
                    _renderers[i].sharedMaterials = _dissolveMats[i];
        }

        private void RestoreMaterials()
        {
            if (_originalMats == null || _renderers == null) return;
            for (int i = 0; i < _renderers.Length; i++)
                if (_renderers[i] != null && _originalMats[i] != null)
                    _renderers[i].sharedMaterials = _originalMats[i];
            SetDissolveAmount(0f);
        }

        private void SetDissolveAmount(float a)
        {
            if (_dissolveMats == null) return;
            for (int i = 0; i < _dissolveMats.Length; i++)
            {
                var arr = _dissolveMats[i];
                if (arr == null) continue;
                for (int m = 0; m < arr.Length; m++)
                    if (arr[m] != null) arr[m].SetFloat(DissolveAmountId, a);
            }
        }

        private IEnumerator DissolveAndDespawn()
        {
            yield return new WaitForSeconds(dissolveDelay);
            SetDissolveAmount(0f);
            ApplyDissolveMaterials();

            float t = 0f;
            float dur = Mathf.Max(0.05f, dissolveDuration);
            while (t < dur)
            {
                t += Time.deltaTime;
                SetDissolveAmount(Mathf.Clamp01(t / dur));
                yield return null;
            }
            SetDissolveAmount(1f);
            Despawn();
        }

        private void OnDestroy()
        {
            if (_dissolveMats == null) return;
            foreach (var arr in _dissolveMats)
            {
                if (arr == null) continue;
                foreach (var m in arr) if (m != null) Destroy(m);
            }
        }

        private void Despawn()
        {
            if (SuppressDespawn) return;
            if (DespawnRequested != null) { DespawnRequested(); return; }
            if (PoolService.Instance != null) PoolService.Instance.Despawn(gameObject);
            else gameObject.SetActive(false);
        }

        private void CacheTarget()
        {
            _playerTf = PlayerRegistry.Nearest(_tf.position);
            if (_playerTf == null) _playerTf = PlayerRegistry.Player;
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
