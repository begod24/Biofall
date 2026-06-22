using UnityEngine;
using Biofall.Core;

namespace Biofall.Gameplay
{
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

        public void Play(float radius, float duration)
        {
            _tf.rotation = Quaternion.Euler(90f, 0f, 0f);

            _targetDiameter = radius * 2f;
            _duration = Mathf.Max(0.01f, duration);
            _timer = 0f;
            _playing = true;
            Apply(0f);
        }

        public void OnSpawned()
        {
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
            float diameter = _targetDiameter * Mathf.Lerp(0.15f, 1f, t);
            _tf.localScale = new Vector3(diameter, diameter, diameter);

            if (ringRenderer == null) return;
            ringRenderer.GetPropertyBlock(_mpb);
            _mpb.SetFloat(ProgressId, t);
            ringRenderer.SetPropertyBlock(_mpb);
        }
    }
}
