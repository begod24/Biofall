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

            if (AllAlivePlayersInZone(defenseRadius))
            {
                _progress = Mathf.Min(1f, _progress + Time.deltaTime / defenseTime);
                EventBus.Publish(new MissionProgress(barLabel, _progress, true));

                if (_progress >= 1f)
                    Charged();
            }
            else
            {
                // Someone is outside the ring: freeze the bar and tell the squad to regroup.
                // (Value held, not dropped.) Solo: this is just "RETURN TO THE BEACON".
                EventBus.Publish(new MissionProgress("REGROUP AT THE BEACON", _progress, true));
            }
        }

        /// <summary>Defense holds only while EVERY up (non-downed) player is inside the zone — both
        /// teammates must stand the beacon together (co-op decision). A downed teammate doesn't block
        /// it (they can't stand), so a lone survivor can keep holding while the other is rescued.
        /// Solo = the single player must be in the ring (unchanged).</summary>
        private bool AllAlivePlayersInZone(float radius)
        {
            if (PlayerRegistry.AliveCount == 0) return false;
            float r2 = radius * radius;
            var all = PlayerRegistry.All;
            for (int i = 0; i < all.Count; i++)
            {
                Transform p = all[i];
                if (p == null || PlayerRegistry.IsDowned(p)) continue;
                if ((p.position - transform.position).sqrMagnitude > r2) return false;
            }
            return true;
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
