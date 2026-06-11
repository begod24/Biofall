using Biofall.Combat;
using UnityEngine;

namespace Biofall.Weapons
{
    /// <summary>
    /// Instant raycast weapon (SMG / AR / Shotgun). Resolves hits against the configured
    /// layers and applies shared <see cref="DamageInfo"/> to any <see cref="IDamageable"/>.
    /// </summary>
    public class HitscanWeapon : WeaponBase
    {
        [Header("Hitscan")]
        [SerializeField] private LayerMask _hitMask = ~0;
        [SerializeField] private QueryTriggerInteraction _triggerInteraction = QueryTriggerInteraction.Collide;

        /// <summary>Layers this weapon's rays collide with — so an aim line can match real shots.</summary>
        public LayerMask HitMask => _hitMask;
        public QueryTriggerInteraction TriggerInteraction => _triggerInteraction;

        protected override void FireRay(Vector3 direction)
        {
            Vector3 origin = Muzzle.position;
            if (!Physics.Raycast(origin, direction, out RaycastHit hit, _data.Range, _hitMask, _triggerInteraction))
                return;

            if (hit.collider.TryGetComponent(out IDamageable damageable) && damageable.IsAlive)
            {
                var info = new DamageInfo(_data.Damage, hit.point, hit.normal, direction, gameObject);
                damageable.ApplyDamage(info);
            }

            SpawnImpact(hit.point, hit.normal);
        }
    }
}
