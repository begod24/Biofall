using UnityEngine;
using Biofall.Core;
using Biofall.Net;

namespace Biofall.Gameplay
{
    /// <summary>
    /// Base for pooled, ground pickups (Object Pooling). Spins/bobs its visual child, optionally
    /// auto-despawns after a lifetime (set lifetime &lt;= 0 to stay on the ground forever), and
    /// collects when the player walks close (distance check — no physics).
    /// Subclasses implement <see cref="OnCollected"/>.
    ///
    /// In CO-OP the walk-over collect + lifetime + pool-despawn are skipped here and driven by the
    /// networked <see cref="Net.CoopPickup"/> bridge instead (server-authoritative collection so the
    /// reward credits only the collecting player). This component then just animates the visual on
    /// every peer. Solo (NetSession.InCoop == false) is untouched.
    /// </summary>
    public abstract class Pickup : MonoBehaviour, IPoolable
    {
        [SerializeField] protected float collectRadius = 1.2f;
        [Tooltip("Seconds before the pickup auto-despawns. Set to 0 (or less) to never disappear.")]
        [SerializeField] private float lifetime = 12f;
        [SerializeField] private float spinSpeed = 90f;
        [SerializeField] private float bobHeight = 0.18f;
        [SerializeField] private float bobSpeed = 2.5f;
        [Tooltip("Child mesh that spins/bobs (defaults to the first child).")]
        [SerializeField] private Transform visual;

        private Transform _tf;
        private float _baseY;
        private float _timer;

        protected virtual void Awake()
        {
            _tf = transform;
            if (visual == null && _tf.childCount > 0) visual = _tf.GetChild(0);
            if (visual != null) _baseY = visual.localPosition.y;
        }

        public void OnSpawned() => _timer = lifetime;
        public void OnDespawned() { }

        /// <summary>Collect distance (incl. the local player's persistent "Scavenger" upgrade), exposed
        /// so <see cref="Net.CoopPickup"/> can reuse it in co-op — per-player there, since the bonus is
        /// read from the per-process <see cref="PlayerProgression"/>.</summary>
        public float CollectRadius => collectRadius + PlayerProgression.PickupRadiusBonus;

        /// <summary>
        /// Grant this pickup's reward on the LOCAL machine. In co-op the server-authoritative
        /// <see cref="Net.CoopPickup"/> calls this only on the player who collected it, so currency /
        /// ammo / grenades / health stay per-player. Solo collects directly via <see cref="OnCollected"/>.
        /// </summary>
        public void ApplyReward() => OnCollected();

        private void Update()
        {
            if (visual != null)
            {
                visual.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.Self);
                Vector3 lp = visual.localPosition;
                lp.y = _baseY + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
                visual.localPosition = lp;
            }

            // Co-op: CoopPickup owns collection + lifetime (server-authoritative). Visual only here.
            if (NetSession.InCoop) return;

            if (PlayerRegistry.HasPlayer)
            {
                Vector3 d = PlayerRegistry.Player.position - _tf.position;
                d.y = 0f;
                float r = CollectRadius;
                if (d.sqrMagnitude <= r * r)
                {
                    OnCollected();
                    Despawn();
                    return;
                }
            }

            if (lifetime > 0f)
            {
                _timer -= Time.deltaTime;
                if (_timer <= 0f) Despawn();
            }
        }

        protected abstract void OnCollected();

        private void Despawn()
        {
            if (PoolService.Instance != null) PoolService.Instance.Despawn(gameObject);
            else gameObject.SetActive(false);
        }
    }
}
