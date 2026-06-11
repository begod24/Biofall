using UnityEngine;

namespace Biofall.Combat
{
    /// <summary>
    /// Dev-only shooting target: logs damage and deactivates on death so weapon feel can
    /// be tested before real enemies are in the scene.
    /// </summary>
    [RequireComponent(typeof(HealthComponent))]
    public class TargetDummy : MonoBehaviour
    {
        private HealthComponent _health;

        private void Awake() => _health = GetComponent<HealthComponent>();

        private void OnEnable()
        {
            _health.OnDamaged += HandleDamaged;
            _health.OnDied += HandleDied;
        }

        private void OnDisable()
        {
            _health.OnDamaged -= HandleDamaged;
            _health.OnDied -= HandleDied;
        }

        private void HandleDamaged(DamageInfo info)
            => Debug.Log($"{name} took {info.Amount} damage ({_health.Current}/{_health.Max} left)");

        private void HandleDied(DamageInfo _)
        {
            Debug.Log($"{name} destroyed");
            gameObject.SetActive(false);
        }
    }
}
