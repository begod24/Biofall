using UnityEngine;
using Biofall.Core;
using Biofall.Net;

namespace Biofall.Gameplay.Mission1
{
    /// <summary>
    /// Mission 1 — main objective. Unlocked once the generator is on (it listens for
    /// <see cref="GeneratorActivated"/>). The player presses E at the beacon to switch it
    /// on: the red signal field (custom shader) lights up, the horde intensifies, and the
    /// charge bar fills ONLY while the player stays inside <see cref="defenseRadius"/>
    /// (it freezes, never drops, when they step out). At full charge it publishes
    /// <see cref="BeaconCharged"/>. Self-contained; the director handles wave intensity.
    /// </summary>
    public sealed class BeaconStation : MonoBehaviour, IInteractable
    {
        [Header("Defense")]
        [Tooltip("Seconds of in-zone time needed to fully charge the beacon.")]
        [SerializeField] private float defenseTime = 45f;
        [Tooltip("Radius around the beacon the player must hold to make progress.")]
        [SerializeField] private float defenseRadius = 7f;
        [SerializeField] private string prompt = "Activate Beacon";
        [SerializeField] private string barLabel = "DEFEND THE BEACON";

        [Header("Visuals")]
        [Tooltip("The red signal field VFX (beam + ground ring). Hidden until activated.")]
        [SerializeField] private GameObject signalField;
        [SerializeField] private AudioSource loopSource;
        [SerializeField] private AudioClip activateSfx;
        [Range(0f, 1f)] [SerializeField] private float activateVolume = 0.8f;

        private bool _unlocked;   // generator powered → beacon can be switched on
        private bool _activated;  // player switched it on → charging
        private bool _charged;    // fully charged
        private float _progress;  // 0..1

        /// <summary>True while the beacon is charging — the director ramps spawns during this.</summary>
        public bool IsCharging => _activated && !_charged;

        // IInteractable ---------------------------------------------------
        public bool CanInteract => _unlocked && !_activated;
        public string Prompt => prompt;
        public Vector3 Position => transform.position;

        public void Interact(GameObject interactor)
        {
            if (!CanInteract) return;
            // CO-OP client: ask the server to switch the beacon on.
            if (NetSession.InCoop && !NetSession.IsServer)
            {
                CoopMission.Instance?.RequestBeaconRpc();
                return;
            }
            ServerInteract();
        }

        /// <summary>Authority-side (server/solo) beacon switch-on.</summary>
        public void ServerInteract()
        {
            if (!CanInteract) return;
            EventBus.Publish(new BeaconActivated());            // fact → _activated + VFX (here + mirrored)
            EventBus.Publish(new MissionProgress(barLabel, 0f, true));
        }
        // -----------------------------------------------------------------

        private void Awake()
        {
            if (signalField != null) signalField.SetActive(false);
        }

        private void OnEnable()
        {
            PlayerInteractor.Register(this);
            EventBus.Subscribe<GeneratorActivated>(OnGeneratorActivated);
            EventBus.Subscribe<BeaconActivated>(OnBeaconActivatedFact);
            EventBus.Subscribe<BeaconCharged>(OnBeaconChargedFact);
        }

        private void OnDisable()
        {
            PlayerInteractor.Unregister(this);
            EventBus.Unsubscribe<GeneratorActivated>(OnGeneratorActivated);
            EventBus.Unsubscribe<BeaconActivated>(OnBeaconActivatedFact);
            EventBus.Unsubscribe<BeaconCharged>(OnBeaconChargedFact);
        }

        private void OnGeneratorActivated(GeneratorActivated _) => _unlocked = true;

        // Visuals/state driven by the facts so every peer (server + mirrored clients) stays in sync.
        private void OnBeaconActivatedFact(BeaconActivated _)
        {
            if (_activated) return;
            _activated = true;
            if (signalField != null) signalField.SetActive(true);
            if (loopSource != null) { loopSource.loop = true; loopSource.Play(); }
            if (activateSfx != null) AudioSource.PlayClipAtPoint(activateSfx, transform.position, activateVolume);
        }

        private void OnBeaconChargedFact(BeaconCharged _) => _charged = true;

        private void Update()
        {
            // CO-OP clients don't run the charge timer — the server owns it and mirrors progress.
            if (NetSession.InCoop && !NetSession.IsServer) return;
            if (!_activated || _charged) return;

            if (AnyPlayerInZone(defenseRadius))
            {
                _progress = Mathf.Min(1f, _progress + Time.deltaTime / defenseTime);
                EventBus.Publish(new MissionProgress(barLabel, _progress, true));

                if (_progress >= 1f)
                    Charged();
            }
            else
            {
                // Out of the zone: freeze the bar, but nudge the label so the player
                // knows progress is paused. (Value held, not dropped.)
                EventBus.Publish(new MissionProgress("RETURN TO THE BEACON", _progress, true));
            }
        }

        /// <summary>Defense holds while AT LEAST ONE player is inside the zone (co-op teammates share it).</summary>
        private bool AnyPlayerInZone(float radius)
        {
            float r2 = radius * radius;
            var all = PlayerRegistry.All;
            for (int i = 0; i < all.Count; i++)
            {
                Transform p = all[i];
                if (p != null && (p.position - transform.position).sqrMagnitude <= r2) return true;
            }
            return false;
        }

        private void Charged()
        {
            if (_charged) return;
            EventBus.Publish(new MissionProgress(barLabel, 1f, false));
            EventBus.Publish(new BeaconCharged());
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.2f, 0.15f, 0.6f);
            Gizmos.DrawWireSphere(transform.position, defenseRadius);
        }
    }
}
