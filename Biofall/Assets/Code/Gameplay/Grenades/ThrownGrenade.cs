using System.Collections.Generic;
using UnityEngine;
using Biofall.Core;
using Biofall.Net;

namespace Biofall.Gameplay
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class ThrownGrenade : MonoBehaviour, IPoolable
    {
        [Header("Explosion")]
        [Tooltip("Seconds before it detonates — kept ~= the thrower's flight time so it blows up on arrival at the aim point.")]
        [SerializeField] private float fuse = 0.55f;
        [SerializeField] private float damage = 50f;
        [SerializeField] private float radius = 2f;
        [Tooltip("Layers the blast damages — set to the Enemy layer so the player isn't hurt.")]
        [SerializeField] private LayerMask damageMask = ~0;

        [Header("FX")]
        [SerializeField] private GameObject explosionPrefab;
        [SerializeField] private float shakeAmplitude = 0.5f;
        [SerializeField] private AudioClip explodeSfx;
        [Range(0f, 1f)][SerializeField] private float sfxVolume = 0.8f;

        private Rigidbody _rb;
        private float _timer;
        private bool _exploded;

        private static readonly Collider[] s_hits = new Collider[64];
        private static readonly HashSet<IDamageable> s_seen = new();
        private static readonly HashSet<CoopEnemy> s_seenCoop = new();

        private void Awake() => _rb = GetComponent<Rigidbody>();

        public void OnSpawned()
        {
            _exploded = false;
            _timer = fuse;
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
        }

        public void OnDespawned() { }

        public void Launch(Vector3 target, float flightTime)
        {
            float t = Mathf.Max(0.2f, flightTime);
            Vector3 g = Physics.gravity;
            Vector3 disp = target - transform.position;
            _rb.linearVelocity = (disp - 0.5f * g * t * t) / t;
            _rb.angularVelocity = Random.insideUnitSphere * 8f;
        }

        private void FixedUpdate()
        {
            if (_exploded) return;
            _timer -= Time.fixedDeltaTime;
            if (_timer <= 0f) Explode();
        }

        private void Explode()
        {
            _exploded = true;
            Vector3 center = transform.position;

            s_seen.Clear();
            s_seenCoop.Clear();
            int n = Physics.OverlapSphereNonAlloc(center, radius, s_hits, damageMask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                Vector3 dir = (s_hits[i].transform.position - center);
                dir.y = 0f;
                dir = dir.normalized;

                if (NetSession.InCoop)
                {
                    var coopEnemy = s_hits[i].GetComponentInParent<CoopEnemy>();
                    if (coopEnemy == null || !s_seenCoop.Add(coopEnemy)) continue;
                    coopEnemy.DamageRpc(damage, s_hits[i].transform.position, dir);
                }
                else
                {
                    var dmg = s_hits[i].GetComponentInParent<IDamageable>();
                    if (dmg == null || !s_seen.Add(dmg)) continue;
                    dmg.TakeDamage(new DamageInfo(damage, center, dir, gameObject));
                }
            }

            if (explosionPrefab != null && PoolService.Instance != null)
                PoolService.Instance.Spawn(explosionPrefab, center, Quaternion.identity);

            EventBus.Publish(new CameraShake(shakeAmplitude));
            if (explodeSfx != null) AudioSource.PlayClipAtPoint(explodeSfx, center, sfxVolume);

            Despawn();
        }

        private void Despawn()
        {
            if (PoolService.Instance != null) PoolService.Instance.Despawn(gameObject);
            else gameObject.SetActive(false);
        }
    }
}
