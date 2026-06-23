using UnityEngine;
using Biofall.Core;
using Biofall.Net;

namespace Biofall.Gameplay
{
    // A lingering acid hazard on the ground spat by the Spitter. Grows in, damages any player standing
    // inside its radius on a fixed tick, then dries up (shader _Life fades) and despawns back to the pool.
    // Coop-safe: the visual runs on every client, but damage is only applied by the server.
    public sealed class AcidPool : MonoBehaviour, IPoolable
    {
        [Tooltip("Flat ground quad that is scaled to the pool diameter (left null = first child / self).")]
        [SerializeField] private Transform visual;
        [SerializeField] private Renderer poolRenderer;
        [SerializeField] private float radius = 2.6f;
        [SerializeField] private float lifetime = 5f;
        [SerializeField] private float damagePerTick = 4f;
        [SerializeField] private float tickInterval = 0.5f;
        [Tooltip("Seconds spent expanding to full size when it lands.")]
        [SerializeField] private float growTime = 0.3f;
        [Tooltip("Seconds spent drying out / fading at the end of life.")]
        [SerializeField] private float fadeTime = 0.9f;

        private static readonly int LifeId = Shader.PropertyToID("_Life");

        private Transform _tf;
        private MaterialPropertyBlock _mpb;
        private float _age;
        private float _tickTimer;
        private bool _active;

        private void Awake()
        {
            _tf = transform;
            if (visual == null) visual = transform.childCount > 0 ? transform.GetChild(0) : transform;
            if (poolRenderer == null) poolRenderer = GetComponentInChildren<Renderer>();
            _mpb = new MaterialPropertyBlock();
        }

        // Lets each spitter size its acid from its own data while the prefab keeps sane defaults.
        public void Configure(float poolRadius, float poolLifetime, float dps, float tick)
        {
            radius = Mathf.Max(0.1f, poolRadius);
            lifetime = Mathf.Max(0.3f, poolLifetime);
            damagePerTick = Mathf.Max(0f, dps);
            tickInterval = Mathf.Max(0.05f, tick);
        }

        public void OnSpawned()
        {
            _age = 0f;
            _tickTimer = tickInterval;
            _active = true;
            ApplyVisual(0f);
        }

        public void OnDespawned() => _active = false;

        private void Update()
        {
            if (!_active) return;

            _age += Time.deltaTime;
            ApplyVisual(_age);

            bool canDamage = !NetSession.InCoop || NetSession.IsServer;
            if (canDamage && damagePerTick > 0f)
            {
                _tickTimer -= Time.deltaTime;
                if (_tickTimer <= 0f)
                {
                    _tickTimer = tickInterval;
                    DamageInside();
                }
            }

            if (_age >= lifetime)
            {
                _active = false;
                if (PoolService.Instance != null) PoolService.Instance.Despawn(gameObject);
                else gameObject.SetActive(false);
            }
        }

        private void ApplyVisual(float age)
        {
            float grow = growTime > 0f ? Mathf.Clamp01(age / growTime) : 1f;
            float fadeStart = lifetime - fadeTime;
            float fade = fadeTime > 0f && age > fadeStart ? Mathf.Clamp01((age - fadeStart) / fadeTime) : 0f;

            float scale = (radius * 2f) * Mathf.SmoothStep(0f, 1f, grow);
            if (visual != null) visual.localScale = new Vector3(scale, scale, scale);

            if (poolRenderer != null)
            {
                poolRenderer.GetPropertyBlock(_mpb);
                _mpb.SetFloat(LifeId, 1f - fade);
                poolRenderer.SetPropertyBlock(_mpb);
            }
        }

        private void DamageInside()
        {
            Vector3 origin = _tf.position;
            float r2 = radius * radius;

            if (NetSession.InCoop)
            {
                var all = PlayerRegistry.All;
                for (int i = 0; i < all.Count; i++)
                {
                    Transform p = all[i];
                    if (p == null || PlayerRegistry.IsDowned(p)) continue;
                    Vector3 d = p.position - origin; d.y = 0f;
                    if (d.sqrMagnitude > r2) continue;
                    var coopPlayer = p.GetComponentInParent<CoopPlayer>();
                    if (coopPlayer != null) coopPlayer.TakeDamageRpc(damagePerTick, origin);
                }
                return;
            }

            Transform playerTf = PlayerRegistry.Player;
            if (playerTf == null) return;
            Vector3 dd = playerTf.position - origin; dd.y = 0f;
            if (dd.sqrMagnitude > r2) return;

            IDamageable target = playerTf.GetComponentInParent<IDamageable>();
            target?.TakeDamage(new DamageInfo(damagePerTick, playerTf.position, Vector3.up, gameObject));
        }
    }
}
