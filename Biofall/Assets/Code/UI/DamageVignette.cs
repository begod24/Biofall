using UnityEngine;
using UnityEngine.UI;
using Biofall.Core;

namespace Biofall.UI
{
    /// <summary>
    /// Maroon edge vignette that reflects pain: a brief pulse when the player is hit, and a
    /// persistent glow that grows as HP gets low (~60% coverage by ~20–30 HP). Pure observer —
    /// it only reads <see cref="PlayerDamaged"/>/<see cref="PlayerDied"/> and tints a full-screen Image.
    /// </summary>
    public sealed class DamageVignette : MonoBehaviour
    {
        [SerializeField] private Image image;
        [SerializeField] private Color color = new Color(0.45f, 0f, 0.03f, 1f);

        [Header("Low-HP glow")]
        [Tooltip("HP at/above which there is no persistent vignette.")]
        [SerializeField] private float lowHpThreshold = 60f;
        [Tooltip("HP at/below which the low-HP vignette reaches its max.")]
        [SerializeField] private float lowHpFull = 20f;
        [Tooltip("Max coverage from low HP (≈0.6 = ~60%).")]
        [SerializeField] private float maxLowAlpha = 0.6f;

        [Header("Hit pulse")]
        [SerializeField] private float pulseStrength = 0.4f;
        [SerializeField] private float pulseDecay = 1.6f;

        private float _hp = 100f;
        private float _pulse;

        private void Awake()
        {
            if (image == null) image = GetComponent<Image>();
            Apply(0f);
        }

        private void OnEnable()
        {
            EventBus.Subscribe<PlayerDamaged>(OnDamaged);
            EventBus.Subscribe<PlayerDied>(OnDied);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<PlayerDamaged>(OnDamaged);
            EventBus.Unsubscribe<PlayerDied>(OnDied);
        }

        private void OnDamaged(PlayerDamaged e)
        {
            _hp = e.Current;
            if (e.Amount > 0f) _pulse = pulseStrength; // flash on real hits, not the init sync
        }

        private void OnDied(PlayerDied _)
        {
            _hp = 0f;
            _pulse = 0.9f;
        }

        private void Update()
        {
            _pulse = Mathf.MoveTowards(_pulse, 0f, pulseDecay * Time.unscaledDeltaTime);
            float low = Mathf.Clamp01(Mathf.InverseLerp(lowHpThreshold, lowHpFull, _hp)) * maxLowAlpha;
            Apply(Mathf.Clamp01(low + _pulse));
        }

        private void Apply(float alpha)
        {
            if (image != null) image.color = new Color(color.r, color.g, color.b, alpha);
        }
    }
}
