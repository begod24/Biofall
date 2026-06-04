using UnityEngine;
using Biofall.Combat;

namespace Biofall.Weapons
{
    [RequireComponent(typeof(Rigidbody))]
    public class Projectile : MonoBehaviour
    {
        [SerializeField] private LayerMask hitMask = ~0;

        private float damage;
        private float speed;
        private float lifeRemaining;
        private GameObject owner;
        private Rigidbody rb;
        private System.Action<Projectile> returnToPool;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            rb.useGravity = false;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
        }

        public void Launch(Vector3 origin, Vector3 direction, float dmg, float spd, float life, GameObject ownerGo, System.Action<Projectile> onReturn)
        {
            transform.SetPositionAndRotation(origin, Quaternion.LookRotation(direction.normalized));
            damage = dmg;
            speed = spd;
            lifeRemaining = life;
            owner = ownerGo;
            returnToPool = onReturn;
            rb.linearVelocity = direction.normalized * spd;
        }

        private void Update()
        {
            lifeRemaining -= Time.deltaTime;
            if (lifeRemaining <= 0f) Despawn();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (owner != null && other.transform.IsChildOf(owner.transform)) return;
            if (((1 << other.gameObject.layer) & hitMask) == 0) return;
            if (other.TryGetComponent<IDamageable>(out var dmg))
            {
                dmg.TakeDamage(new DamageInfo(damage, owner, transform.position, -transform.forward, DamageType.Bullet));
            }
            Despawn();
        }

        private void Despawn()
        {
            rb.linearVelocity = Vector3.zero;
            returnToPool?.Invoke(this);
        }
    }
}
