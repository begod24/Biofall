using UnityEngine;
using UnityEngine.Rendering;
using Biofall.Core;

namespace Biofall.UI
{
    [RequireComponent(typeof(Volume))]
    public sealed class LowHealthVolume : MonoBehaviour
    {
        [SerializeField] private Volume volume;

        [Header("Low-HP ramp")]
        [Tooltip("HP at/above which the effect is off.")]
        [SerializeField] private float lowHpThreshold = 55f;
        [Tooltip("HP at/below which the effect is at full strength.")]
        [SerializeField] private float lowHpFull = 15f;
        [Tooltip("Max weight reached from low HP alone (hit pulses can push to 1).")]
        [SerializeField] private float maxWeight = 0.85f;

        [Header("Hit pulse")]
        [SerializeField] private float pulseStrength = 0.35f;
        [SerializeField] private float pulseDecay = 1.8f;

        [Tooltip("How fast the weight eases toward its target (per second).")]
        [SerializeField] private float smooth = 6f;

        private float _hp = 100f;
        private float _pulse;
        private float _weight;

        private void Awake()
        {
            if (volume == null) volume = GetComponent<Volume>();
            if (volume != null) volume.weight = 0f;
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
            if (e.Amount > 0f) _pulse = pulseStrength;
        }

        private void OnDied(PlayerDied _)
        {
            _hp = 0f;
            _pulse = 0.9f;
        }

        private void Update()
        {
            _pulse = Mathf.MoveTowards(_pulse, 0f, pulseDecay * Time.unscaledDeltaTime);
            float low = Mathf.Clamp01(Mathf.InverseLerp(lowHpThreshold, lowHpFull, _hp)) * maxWeight;
            float target = Mathf.Clamp01(low + _pulse);
            _weight = Mathf.MoveTowards(_weight, target, smooth * Time.unscaledDeltaTime);
            if (volume != null) volume.weight = _weight;
        }
    }
}
