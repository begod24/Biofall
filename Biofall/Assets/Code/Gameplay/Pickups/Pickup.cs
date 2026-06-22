using UnityEngine;
using Biofall.Core;
using Biofall.Net;

namespace Biofall.Gameplay
{
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

        public float CollectRadius => collectRadius + PlayerProgression.PickupRadiusBonus;

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
