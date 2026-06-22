using UnityEngine;

namespace Biofall.Gameplay
{
    /// <summary>
    /// A cheap world-space health bar that floats over an enemy. Hidden (GameObject inactive) while the
    /// enemy is at full HP and for a few seconds after the last hit, so a full horde costs nothing and
    /// only the handful of currently-damaged zombies render a bar. The fill depletes from the right via
    /// a left-pivoted child's <c>localScale.x</c> (same trick as the player HUD bar) and the root
    /// billboards to the camera. Driven by <see cref="Enemy"/> — solo/server from the real Health, co-op
    /// clients from a replicated HP fraction (see <see cref="Net.CoopEnemy"/>).
    /// </summary>
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

        /// <summary>Set the bar to <paramref name="fraction"/> [0..1]. Shows the bar; full HP hides it.</summary>
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

        /// <summary>Reset to full and hide (pooled reuse / fresh spawn).</summary>
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
            if (_cam != null && _tf != null) _tf.rotation = _cam.transform.rotation; // billboard

            _hideTimer -= Time.deltaTime;
            if (_hideTimer <= 0f) Hide();
        }
    }
}
