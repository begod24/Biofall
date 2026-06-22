using UnityEngine;
using Biofall.Core;

namespace Biofall.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class MuzzleFlash : MonoBehaviour, IPoolable
    {
        [SerializeField] private Light flashLight;
        [SerializeField] private Transform core;
        [Tooltip("How long the flash stays visible (seconds).")]
        [SerializeField] private float duration = 0.06f;
        [SerializeField] private float lightIntensity = 6f;
        [SerializeField] private float coreScale = 0.12f;

        private float _timer;

        private void Awake()
        {
            if (flashLight == null) flashLight = GetComponentInChildren<Light>();
            if (core == null)
            {
                Transform found = transform.Find("Core");
                if (found != null) core = found;
            }
        }

        public void OnSpawned()
        {
            _timer = duration;

            transform.Rotate(Vector3.forward, Random.Range(0f, 360f), Space.Self);
            float s = coreScale * Random.Range(0.85f, 1.15f);

            if (core != null) core.localScale = new Vector3(s, s, s);
            if (flashLight != null)
            {
                flashLight.enabled = true;
                flashLight.intensity = lightIntensity;
            }
        }

        public void OnDespawned()
        {
            if (flashLight != null) flashLight.enabled = false;
        }

        private void Update()
        {
            if (_timer <= 0f) return;

            _timer -= Time.deltaTime;
            float t = Mathf.Clamp01(_timer / duration);

            if (flashLight != null) flashLight.intensity = lightIntensity * t;
            if (core != null) core.localScale = Vector3.one * (coreScale * t);

            if (_timer <= 0f)
            {
                if (PoolService.Instance != null) PoolService.Instance.Despawn(gameObject);
                else gameObject.SetActive(false);
            }
        }
    }
}
