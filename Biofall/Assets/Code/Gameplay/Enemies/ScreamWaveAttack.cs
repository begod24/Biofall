using UnityEngine;
using Biofall.Core;

namespace Biofall.Gameplay
{
    /// <summary>
    /// The Screamer's attack. While the Animator is in the scream/Attack state it plays SFX_Scremaer
    /// once, then repeatedly emits expanding wave rings (every <see cref="ScreamerData.waveInterval"/>)
    /// that ripple out across the area around it. Each wave shakes the camera and damages the player
    /// if they're inside the radius — so standing in the zone during the scream keeps hurting. Enemies
    /// never take wave damage. Lives beside <see cref="Enemy"/> on the Screamer prefab.
    /// </summary>
    public sealed class ScreamWaveAttack : MonoBehaviour
    {
        [SerializeField] private ScreamerData data;
        [SerializeField] private Animator animator;
        [SerializeField] private AudioSource audioSource;
        [Tooltip("Optional spawn origin for the wave VFX (defaults to this transform, on the ground).")]
        [SerializeField] private Transform waveOrigin;

        // The state that plays the scream clip is named "Attack" in the controller.
        private static readonly int ScreamStateHash = Animator.StringToHash("Attack");

        private Transform _tf;
        private bool _screaming;
        private float _pulseTimer;

        private void Awake()
        {
            _tf = transform;
            if (animator == null) animator = GetComponentInParent<Animator>();
            if (audioSource == null) audioSource = GetComponent<AudioSource>();
            if (waveOrigin == null) waveOrigin = _tf;
        }

        private void OnDisable()
        {
            _screaming = false; // clean slate for a pooled reuse
        }

        private void Update()
        {
            if (animator == null || data == null) return;

            bool inScream = animator.GetCurrentAnimatorStateInfo(0).shortNameHash == ScreamStateHash;

            if (inScream && !_screaming)
            {
                // Scream just started.
                _screaming = true;
                _pulseTimer = data.waveDelay;
                PlayScreamSfx();
            }
            else if (!inScream && _screaming)
            {
                _screaming = false;
            }

            if (_screaming)
            {
                _pulseTimer -= Time.deltaTime;
                if (_pulseTimer <= 0f)
                {
                    _pulseTimer = Mathf.Max(0.05f, data.waveInterval);
                    EmitWave();
                }
            }
        }

        private void PlayScreamSfx()
        {
            if (data.screamSfx != null && audioSource != null)
                audioSource.PlayOneShot(data.screamSfx, data.screamVolume);
        }

        /// <summary>One ripple: spawn the expanding ring VFX, shake the camera, hurt the player if near.</summary>
        private void EmitWave()
        {
            Vector3 origin = waveOrigin.position;

            // Visual: pooled, pulsing red ring that expands to the damage radius and fades.
            if (data.waveVfxPrefab != null && PoolService.Instance != null)
            {
                Vector3 vfxPos = origin + Vector3.up * 0.08f; // lift off the ground to avoid z-fighting
                GameObject go = PoolService.Instance.Spawn(data.waveVfxPrefab, vfxPos, Quaternion.identity);
                if (go != null && go.TryGetComponent(out ScreamWaveVFX vfx))
                    vfx.Play(data.waveRadius, data.waveExpandDuration);
            }

            if (data.cameraShakeAmplitude > 0f)
                EventBus.Publish(new CameraShake(data.cameraShakeAmplitude));

            // Damage: player only (zombies are immune to the wave).
            Transform playerTf = PlayerRegistry.Player;
            if (playerTf == null) return;

            Vector3 d = playerTf.position - origin;
            d.y = 0f;
            if (d.sqrMagnitude > data.waveRadius * data.waveRadius) return;

            IDamageable target = playerTf.GetComponentInParent<IDamageable>();
            target?.TakeDamage(new DamageInfo(data.waveDamage, playerTf.position, d.normalized, gameObject));
        }
    }
}
