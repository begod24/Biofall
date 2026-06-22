using System.Collections;
using UnityEngine;
using Biofall.Core;

namespace Biofall.Gameplay
{
    public sealed class BloodPool : MonoBehaviour, IPoolable
    {
        [Tooltip("The flat quad child that is scaled (left null = first child, or self).")]
        [SerializeField] private Transform visual;
        [SerializeField] private float growTime = 0.25f;
        [SerializeField] private Vector2 sizeRange = new Vector2(0.8f, 1.4f);

        private float _targetScale = 1f;

        private void Awake()
        {
            if (visual == null) visual = transform.childCount > 0 ? transform.GetChild(0) : transform;
        }

        public void OnSpawned()
        {
            _targetScale = Random.Range(sizeRange.x, sizeRange.y);
            StopAllCoroutines();
            StartCoroutine(Grow());
        }

        public void OnDespawned()
        {
            StopAllCoroutines();
        }

        private IEnumerator Grow()
        {
            SetScale(0.01f);
            float t = 0f;
            float dur = Mathf.Max(0.02f, growTime);
            while (t < dur)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / dur);
                SetScale(Mathf.Lerp(0.01f, _targetScale, k * (2f - k)));
                yield return null;
            }
            SetScale(_targetScale);
        }

        private void SetScale(float s)
        {
            if (visual != null) visual.localScale = new Vector3(s, s, s);
        }
    }
}
