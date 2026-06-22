using UnityEngine;
using Biofall.Core;
using Biofall.Net;

namespace Biofall.Gameplay.Mission1
{
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

        private bool _unlocked;
        private bool _activated;
        private bool _charged;
        private float _progress;

        public bool IsCharging => _activated && !_charged;

        public bool CanInteract => _unlocked && !_activated;
        public string Prompt => prompt;
        public Vector3 Position => transform.position;

        public void Interact(GameObject interactor)
        {
            if (!CanInteract) return;
            if (NetSession.InCoop && !NetSession.IsServer)
            {
                CoopMission.Instance?.RequestBeaconRpc();
                return;
            }
            ServerInteract();
        }

        public void ServerInteract()
        {
            if (!CanInteract) return;
            EventBus.Publish(new BeaconActivated());
            EventBus.Publish(new MissionProgress(barLabel, 0f, true));
        }

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
                EventBus.Publish(new MissionProgress("REGROUP AT THE BEACON", _progress, true));
            }
        }

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
