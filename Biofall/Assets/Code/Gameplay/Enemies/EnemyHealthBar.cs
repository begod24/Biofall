using UnityEngine;

namespace Biofall.Gameplay
{
    public sealed class EnemyHealthBar : MonoBehaviour
    {
        [Tooltip("Left-pivoted fill transform; its localScale.x is set to the HP fraction.")]
        [SerializeField] private Transform fill;
        [Tooltip("Seconds the bar stays visible after the last hit before auto-hiding.")]
        [SerializeField] private float hideDelay = 4f;

        private Transform _tf;
        private Camera _cam;
        private float _hideTimer;

        private void EnsureRefs()
        {
            if (_tf == null) _tf = transform;
            if (fill == null)
            {
                var f = _tf.Find("BG/Fill");
                if (f == null) f = _tf.Find("Fill");
                fill = f;
            }
        }

        public void Set(float fraction)
        {
            EnsureRefs();
            fraction = Mathf.Clamp01(fraction);
            if (fill != null)
            {
                Vector3 s = fill.localScale;
                s.x = fraction;
                fill.localScale = s;
            }
            _hideTimer = hideDelay;

            if (fraction >= 1f) { Hide(); return; }
            if (!gameObject.activeSelf) gameObject.SetActive(true);
        }

        public void ResetBar()
        {
            EnsureRefs();
            if (fill != null)
            {
                Vector3 s = fill.localScale;
                s.x = 1f;
                fill.localScale = s;
            }
            Hide();
        }

        public void Hide()
        {
            if (gameObject.activeSelf) gameObject.SetActive(false);
        }

        private void LateUpdate()
        {
            if (_cam == null) _cam = Camera.main;
            if (_cam != null && _tf != null) _tf.rotation = _cam.transform.rotation;

            _hideTimer -= Time.deltaTime;
            if (_hideTimer <= 0f) Hide();
        }
    }
}
