using UnityEngine;
using Biofall.Core;

namespace Biofall.Gameplay
{
    public sealed class Explosion : MonoBehaviour, IPoolable
    {
        [SerializeField] private float duration = 0.6f;

        [Header("Fireball")]
        [SerializeField] private Renderer fireball;
        [SerializeField] private float fireballStart = 0.5f;
        [SerializeField] private float fireballEnd = 4f;

        [Header("Shockwave ring")]
        [SerializeField] private Renderer shockwave;
        [SerializeField] private float ringEnd = 7f;

        [Header("Flash")]
        [SerializeField] private Light flash;
        [SerializeField] private float flashIntensity = 9f;

        private Transform _fireTf;
        private Transform _ringTf;
        private MaterialPropertyBlock _mpb;
        private float _t;

        private static readonly int ProgressId = Shader.PropertyToID("_Progress");

        private void Awake()
        {
            if (fireball == null) { var t = transform.Find("Fireball"); if (t) fireball = t.GetComponent<Renderer>(); }
            if (shockwave == null) { var t = transform.Find("Shockwave"); if (t) shockwave = t.GetComponent<Renderer>(); }
            if (flash == null) flash = GetComponentInChildren<Light>(true);
            _fireTf = fireball != null ? fireball.transform : null;
            _ringTf = shockwave != null ? shockwave.transform : null;
            _mpb = new MaterialPropertyBlock();
        }

        public void OnSpawned()
        {
            _t = 0f;
            Apply(0f);
        }

        public void OnDespawned() { }

        private void Update()
        {
            _t += Time.deltaTime / Mathf.Max(0.05f, duration);
            float p = Mathf.Clamp01(_t);
            Apply(p);
            if (p >= 1f) Despawn();
        }

        private void Apply(float p)
        {
            float ease = 1f - (1f - p) * (1f - p);

            if (_fireTf != null)
                _fireTf.localScale = Vector3.one * Mathf.Lerp(fireballStart, fireballEnd, ease);
            if (fireball != null)
            {
                fireball.GetPropertyBlock(_mpb);
                _mpb.SetFloat(ProgressId, p);
                fireball.SetPropertyBlock(_mpb);
            }

            if (_ringTf != null)
            {
                float r = Mathf.Lerp(0.2f, ringEnd, ease);
                _ringTf.localScale = new Vector3(r, _ringTf.localScale.y, r);
            }
            if (shockwave != null)
            {
                shockwave.GetPropertyBlock(_mpb);
                _mpb.SetFloat(ProgressId, Mathf.Clamp01(p * 1.15f));
                shockwave.SetPropertyBlock(_mpb);
            }

            if (flash != null)
                flash.intensity = flashIntensity * (1f - p) * Mathf.Clamp01(p * 8f);
        }

        private void Despawn()
        {
            if (PoolService.Instance != null) PoolService.Instance.Despawn(gameObject);
            else gameObject.SetActive(false);
        }
    }
}
