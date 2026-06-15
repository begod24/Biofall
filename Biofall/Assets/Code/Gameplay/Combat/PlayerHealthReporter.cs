using UnityEngine;
using Biofall.Core;

namespace Biofall.Gameplay
{
    /// <summary>
    /// Bridges the player's <see cref="Health"/> to the EventBus (Observer) so the UI can
    /// react without ever referencing gameplay. Publishes <see cref="PlayerDamaged"/>
    /// (including an initial sync on Start so freshly-subscribed HUD initializes through
    /// the same event path) and <see cref="PlayerDied"/>.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public sealed class PlayerHealthReporter : MonoBehaviour
    {
        private Health _health;

        private void Awake()
        {
            _health = GetComponent<Health>();
        }

        private void OnEnable()
        {
            _health.Damaged += OnDamaged;
            _health.Healed += OnHealed;
            _health.Died += OnDied;
        }

        private void OnDisable()
        {
            _health.Damaged -= OnDamaged;
            _health.Healed -= OnHealed;
            _health.Died -= OnDied;
        }

        private void Start()
        {
            // Announce current HP so the HUD initializes via the event, not a direct read.
            EventBus.Publish(new PlayerDamaged(_health.Current, _health.Max, 0f));
        }

        private void OnDamaged(DamageInfo info, float current)
            => EventBus.Publish(new PlayerDamaged(current, _health.Max, info.Amount));

        private void OnHealed(float current, float max)
            => EventBus.Publish(new PlayerDamaged(current, max, 0f));

        private void OnDied()
            => EventBus.Publish(new PlayerDied());
    }
}
