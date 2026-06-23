using UnityEngine;
using Biofall.Core;
using Biofall.Net;

namespace Biofall.Gameplay
{
    // Drives the Spitter's ranged acid attack. Watches the shared "Attack" animator state (same hook
    // the Screamer uses) and, after a wind-up, drops a lingering acid pool at the targeted player's
    // feet. Mirrors ScreamWaveAttack: the visual is spawned on every client, but the pool itself only
    // applies damage on the server (see AcidPool), so it is coop-safe.
    public sealed class SpitAcidAttack : MonoBehaviour
    {
        [SerializeField] private SpitterData data;
        [SerializeField] private Animator animator;
        [SerializeField] private AudioSource audioSource;
        [Tooltip("Optional mouth/origin for the spit SFX (defaults to this transform).")]
        [SerializeField] private Transform mouth;

        private static readonly int AttackStateHash = Animator.StringToHash("Attack");

        private Transform _tf;
        private bool _spitting;
        private bool _spawned;
        private float _windupTimer;

        private void Awake()
        {
            _tf = transform;
            if (animator == null) animator = GetComponentInParent<Animator>();
            if (audioSource == null) audioSource = GetComponent<AudioSource>();
            if (mouth == null) mouth = _tf;
        }

        private void OnDisable()
        {
            _spitting = false;
            _spawned = false;
        }

        private void Update()
        {
            if (animator == null || data == null) return;

            bool inAttack = animator.GetCurrentAnimatorStateInfo(0).shortNameHash == AttackStateHash;

            if (inAttack && !_spitting)
            {
                _spitting = true;
                _spawned = false;
                _windupTimer = data.spitWindup;
                PlaySpitSfx();
            }
            else if (!inAttack && _spitting)
            {
                _spitting = false;
            }

            if (_spitting && !_spawned)
            {
                _windupTimer -= Time.deltaTime;
                if (_windupTimer <= 0f)
                {
                    _spawned = true;
                    SpawnAcid();
                }
            }
        }

        private void PlaySpitSfx()
        {
            if (data.spitSfx != null && audioSource != null)
                audioSource.PlayOneShot(data.spitSfx, data.spitVolume);
        }

        private void SpawnAcid()
        {
            Transform target = PlayerRegistry.Nearest(_tf.position);
            if (target == null) target = PlayerRegistry.Player;
            if (target == null) return;

            // The target's pivot sits at its feet (already on the ground), so use it directly. A
            // downward raycast here would hit the player / other enemies and float the pool up to
            // body height, making it draw on top of everyone.
            Vector3 landing = target.position;
            if (data.aimLead > 0f)
            {
                Vector3 fwd = target.forward; fwd.y = 0f;
                landing += fwd.normalized * data.aimLead;
            }

            if (PoolService.Instance == null) return;

            // Lob a visible glob if we have one — it flies in an arc and bursts into the acid pool on
            // impact (so you can see the spit coming). Falls back to dropping the pool directly.
            if (data.spitProjectilePrefab != null)
            {
                Vector3 origin = (mouth != null && mouth != _tf)
                    ? mouth.position
                    : _tf.position + Vector3.up * data.spitOriginHeight;
                GameObject globGo = PoolService.Instance.Spawn(data.spitProjectilePrefab, origin, Quaternion.identity);
                if (globGo != null && globGo.TryGetComponent(out AcidProjectile glob))
                {
                    glob.Launch(origin, landing, data);
                    return;
                }
            }

            SpawnPoolDirect(landing);
        }

        private void SpawnPoolDirect(Vector3 landing)
        {
            if (data.spitVfxPrefab != null)
                PoolService.Instance.Spawn(data.spitVfxPrefab, landing + Vector3.up * 0.05f, Quaternion.identity);

            if (data.acidPoolPrefab != null)
            {
                GameObject go = PoolService.Instance.Spawn(data.acidPoolPrefab, landing + Vector3.up * 0.02f, Quaternion.identity);
                if (go != null && go.TryGetComponent(out AcidPool pool))
                    pool.Configure(data.poolRadius, data.poolLifetime, data.acidDamagePerTick, data.acidTickInterval);
            }
        }
    }
}
