using Biofall.Combat;
using Biofall.Core;
using UnityEngine;

namespace Biofall.Player
{
    /// <summary>
    /// Bridges the player's shared <see cref="HealthComponent"/> to the game-wide event
    /// hub so HUD and game flow react without polling.
    /// </summary>
    [RequireComponent(typeof(HealthComponent))]
    public class PlayerHealth : MonoBehaviour
    {
        private HealthComponent _health;

        private void Awake() => _health = GetComponent<HealthComponent>();

        private void OnEnable()
        {
            _health.OnHealthChanged += HandleHealthChanged;
            _health.OnDied += HandleDied;
            GameEvents.RaisePlayerHealthChanged(_health.Current, _health.Max);
        }

        private void OnDisable()
        {
            _health.OnHealthChanged -= HandleHealthChanged;
            _health.OnDied -= HandleDied;
        }

        private void HandleHealthChanged(float current, float max)
            => GameEvents.RaisePlayerHealthChanged(current, max);

        private void HandleDied(DamageInfo _)
        {
            GameEvents.RaisePlayerDied();
            if (GameManager.Exists) GameManager.Instance.SetState(GameState.MissionFailed);
        }
    }
}
