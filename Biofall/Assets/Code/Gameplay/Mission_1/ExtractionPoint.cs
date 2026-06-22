using UnityEngine;
using Biofall.Core;
using Biofall.Net;

namespace Biofall.Gameplay.Mission1
{
    /// <summary>
    /// Mission 1 — final objective. Opens once the beacon is charged (listens for
    /// <see cref="BeaconCharged"/>). The player must stand inside <see cref="extractRadius"/>
    /// for <see cref="extractTime"/> seconds; the HUD counts down. Stepping out resets the
    /// countdown. When it reaches zero it publishes <see cref="MissionCompleted"/>.
    /// </summary>
    public sealed class ExtractionPoint : MonoBehaviour
    {
        [Header("Extraction")]
        [Tooltip("Seconds the player must hold the point.")]
        [SerializeField] private float extractTime = 5f;
        [SerializeField] private float extractRadius = 3f;

        [Header("Visuals")]
        [Tooltip("Shown only once extraction is open (e.g. a glowing pad / arrow).")]
        [SerializeField] private GameObject openVfx;
        [SerializeField] private AudioSource loopSource;

        private bool _open;
        private bool _done;
        private float _elapsed;
        private int _lastShownSecond = -1;
        private string _label;

        private void Awake()
        {
            if (openVfx != null) openVfx.SetActive(false);
        }

        private void OnEnable() => EventBus.Subscribe<BeaconCharged>(OnBeaconCharged);
        private void OnDisable() => EventBus.Unsubscribe<BeaconCharged>(OnBeaconCharged);

        private void OnBeaconCharged(BeaconCharged _)
        {
            _open = true;
            if (openVfx != null) openVfx.SetActive(true);
            if (loopSource != null) { loopSource.loop = true; loopSource.Play(); }
        }

        private void Update()
        {
            // CO-OP clients don't run the countdown — the server owns it and mirrors progress.
            if (NetSession.InCoop && !NetSession.IsServer) return;
            if (!_open || _done) return;

            bool inZone = AllPlayersInZone(extractRadius);

            if (inZone)
            {
                _elapsed += Time.deltaTime;
                PublishCountdown();
                if (_elapsed >= extractTime)
                    Complete();
            }
            else if (_elapsed > 0f)
            {
                // Left the pad — reset the countdown.
                _elapsed = 0f;
                _lastShownSecond = -1;
                EventBus.Publish(new MissionProgress("REACH THE EXTRACTION", 0f, true));
            }
        }

        private void PublishCountdown()
        {
            int remaining = Mathf.Max(0, Mathf.CeilToInt(extractTime - _elapsed));
            if (remaining != _lastShownSecond)
            {
                _lastShownSecond = remaining;
                _label = "EXTRACTING IN " + remaining;
            }
            EventBus.Publish(new MissionProgress(_label, Mathf.Clamp01(_elapsed / extractTime), true));
        }

        /// <summary>Team extract: every UP (non-downed) player must be on the pad (solo = the one
        /// player). Downed/dead teammates don't block extraction — the survivors can still leave,
        /// and stepping onto the pad won't be possible for a body that can't move.</summary>
        private bool AllPlayersInZone(float radius)
        {
            var all = PlayerRegistry.All;
            if (PlayerRegistry.AliveCount == 0) return false;
            float r2 = radius * radius;
            for (int i = 0; i < all.Count; i++)
            {
                Transform p = all[i];
                if (p == null || PlayerRegistry.IsDowned(p)) continue;
                if ((p.position - transform.position).sqrMagnitude > r2) return false;
            }
            return true;
        }

        private void Complete()
        {
            _done = true;
            EventBus.Publish(new MissionProgress(null, 1f, false));
            EventBus.Publish(new MissionCompleted());
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.2f, 1f, 0.4f, 0.6f);
            Gizmos.DrawWireSphere(transform.position, extractRadius);
        }
    }
}
