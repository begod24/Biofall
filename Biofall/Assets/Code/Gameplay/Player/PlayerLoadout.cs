using UnityEngine;
using Biofall.Core;
using Biofall.Net;

namespace Biofall.Gameplay
{
    /// <summary>
    /// Applies the player's PERSISTENT upgrades (see <see cref="PlayerProgression"/>) to this body once
    /// at spawn: +max HP, move-speed multiplier, +grenade capacity, and a slow out-of-combat health
    /// regen. The pickup-radius and revive-speed bonuses are read directly where they apply (Pickup /
    /// CoopReviveInteractor), so they aren't touched here.
    ///
    /// SOLO applies from <see cref="Start"/>. CO-OP applies only on the OWNER, driven by
    /// <c>CoopPlayer.OnNetworkSpawn</c> (remote replicas are puppets and must NOT re-stat someone else's
    /// body). Reads the static progression, so per-process = per-player in co-op, exactly like the wallet.
    /// </summary>
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
            // Solo applies immediately; co-op waits for the owner's network spawn (see CoopPlayer).
            if (!NetSession.InCoop) Apply();
        }

        /// <summary>Apply all persistent stat upgrades to this player. Idempotent (runs once).</summary>
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

            // Heal in whole-HP steps so we don't fire a Healed event (and a co-op HP sync) every frame.
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
