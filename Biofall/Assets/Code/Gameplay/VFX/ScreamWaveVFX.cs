using UnityEngine;
using Biofall.Core;

namespace Biofall.Gameplay
{
    /// <summary>
    /// Pooled visual for the Screamer's scream wave: a flat quad lying on the ground whose material
    /// (ScreamWave.shader) draws pulsing dark-red rings. <see cref="Play"/> expands it from nothing to
    /// the damage radius while driving the shader's <c>_Progress</c> (0→1), then returns to the pool.
    /// Purely cosmetic — the actual damage lives in <see cref="ScreamWaveAttack"/>.
    /// </summary>
    public sealed class ScreamWaveVFX : MonoBehaviour, IPoolable
    {
        [SerializeField] private Renderer ringRenderer;

        private static readonly int ProgressId = Shader.PropertyToID("_Progress");

        private Transform _tf;
        private MaterialPropertyBlock _mpb;
        private float _duration;
        private float _timer;
        private float _targetDiameter;
        private bool _playing;

        private void Awake()
        {
            _tf = transform;
            if (ringRenderer == null) ringRenderer = GetComponentInChildren<Renderer>();
            _mpb = new MaterialPropertyBlock();
        }

        /// <summary>Configure and start the expansion. radius = wave damage radius (metres).</summary>
        public void Play(float radius, float duration)
        {
            // Always lie flat on the ground, no matter what rotation we were spawned with
            // (PoolService spawns with Quaternion.identity, which would leave the quad upright).
            _tf.rotation = Quaternion.Euler(90f, 0f, 0f);

            _targetDiameter = radius * 2f;        // quad spans the full diameter
            _duration = Mathf.Max(0.01f, duration);
            _timer = 0f;
            _playing = true;
            Apply(0f);
        }

        public void OnSpawned()
        {
            // Defaults in case Play() isn't called for some reason.
            if (!_playing) Play(5f, 0.6f);
        }

        public void OnDespawned() => _playing = false;

        private void Update()
        {
            if (!_playing) return;

            _timer += Time.deltaTime;
            float t = Mathf.Clamp01(_timer / _duration);
            Apply(t);

            if (t >= 1f)
            {
                _playing = false;
                if (PoolService.Instance != null) PoolService.Instance.Despawn(gameObject);
                else gameObject.SetActive(false);
            }
        }

        private void Apply(float t)
        {
            // Grow from a visible base (15%) to full so the ring is never a zero-size dot.
            float diameter = _targetDiameter * Mathf.Lerp(0.15f, 1f, t);
            _tf.localScale = new Vector3(diameter, diameter, diameter);

            if (ringRenderer == null) return;
            ringRenderer.GetPropertyBlock(_mpb);
            _mpb.SetFloat(ProgressId, t);
            ringRenderer.SetPropertyBlock(_mpb);
        }
    }
}
