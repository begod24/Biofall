using UnityEngine;
using Biofall.Core;
using Biofall.Net;

namespace Biofall.Gameplay
{
    [RequireComponent(typeof(Health))]
    public sealed class PlayerLoadout : MonoBehaviour
    {
        [Tooltip("Seconds after taking damage before health regen resumes.")]
        [SerializeField] private float regenCombatDelay = 4f;

        private Health _health;
        private PlayerMotor _motor;
        private GrenadeInventory _grenades;

        private float _baseMax;
        private bool _applied;
        private float _regenBlockedUntil;
        private float _regenAccum;

        private void Awake()
        {
            _health = GetComponent<Health>();
            _motor = GetComponent<PlayerMotor>();
            _grenades = GetComponent<GrenadeInventory>();
            _baseMax = _health != null ? _health.Max : 100f;
        }

        private void Start()
        {
            if (!NetSession.InCoop) Apply();
        }

        public void Apply()
        {
            if (_applied) return;
            _applied = true;

            if (_health != null)
            {
                _health.SetMax(_baseMax + PlayerProgression.MaxHealthBonus, true);
                _health.Damaged += OnDamaged;
            }
            if (_motor != null) _motor.SetSpeedMultiplier(PlayerProgression.MoveSpeedMultiplier);
            if (_grenades != null) _grenades.ApplyCapacityBonus(PlayerProgression.GrenadeCapacityBonus);
        }

        private void OnDestroy()
        {
            if (_applied && _health != null) _health.Damaged -= OnDamaged;
        }

        private void OnDamaged(DamageInfo _, float __) => _regenBlockedUntil = Time.time + regenCombatDelay;

        private void Update()
        {
            if (!_applied) return;
            float regen = PlayerProgression.HealthRegenPerSecond;
            if (regen <= 0f || _health == null || !_health.IsAlive) return;
            if (Time.time < _regenBlockedUntil || _health.Current >= _health.Max) return;

            _regenAccum += regen * Time.deltaTime;
            if (_regenAccum >= 1f)
            {
                float whole = Mathf.Floor(_regenAccum);
                _regenAccum -= whole;
                _health.Heal(whole);
            }
        }
    }
}
