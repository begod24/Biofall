using UnityEngine;
using Biofall.Core;
using Biofall.Net;

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
        {
            // CO-OP: HP hitting 0 means DOWNED, not GAME OVER. CoopPlayerLife (which also watches
            // Health.Died on the owner) turns this into a networked downed state + revive flow, so we
            // must NOT publish PlayerDied here (that would pop the solo Game Over and lock the player).
            if (NetSession.InCoop) return;
            EventBus.Publish(new PlayerDied());
        }
    }
}
