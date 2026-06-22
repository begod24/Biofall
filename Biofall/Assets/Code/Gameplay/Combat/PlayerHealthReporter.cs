using UnityEngine;
using Biofall.Core;
using Biofall.Net;

namespace Biofall.Gameplay
{
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
            EventBus.Publish(new PlayerDamaged(_health.Current, _health.Max, 0f));
        }

        private void OnDamaged(DamageInfo info, float current)
            => EventBus.Publish(new PlayerDamaged(current, _health.Max, info.Amount));

        private void OnHealed(float current, float max)
            => EventBus.Publish(new PlayerDamaged(current, max, 0f));

        private void OnDied()
        {
            if (NetSession.InCoop) return;
            EventBus.Publish(new PlayerDied());
        }
    }
}
