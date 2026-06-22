using System.Collections;
using UnityEngine;
using Biofall.Core;
using Biofall.Net;

namespace Biofall.Gameplay.Mission1
{
    /// <summary>
    /// Mission 1 — first objective. The player walks to the generator and presses E once;
    /// a progress bar then fills on its own over <see cref="chargeTime"/> and the generator
    /// powers up (light + hum). On completion it publishes <see cref="GeneratorActivated"/>,
    /// which the <see cref="MissionDirector"/> turns into the next phase. Self-contained.
    /// </summary>
    public sealed class GeneratorStation : MonoBehaviour, IInteractable
    {
        [Header("Charge")]
        [Tooltip("Seconds for the bar to fill after the player presses E.")]
        [SerializeField] private float chargeTime = 3f;
        [SerializeField] private string prompt = "Turn On Generator";
        [SerializeField] private string barLabel = "POWERING GENERATOR";

        [Header("Powered feedback (all optional)")]
        [Tooltip("Light(s) switched on once the generator is powered.")]
        [SerializeField] private Light[] poweredLights;
        [Tooltip("Objects enabled once powered (e.g. running-fan VFX).")]
        [SerializeField] private GameObject[] enableWhenPowered;
        [SerializeField] private AudioSource humSource;
        [SerializeField] private AudioClip startupSfx;
        [Range(0f, 1f)] [SerializeField] private float startupVolume = 0.7f;

        private bool _activated;
        private bool _charging;

        // IInteractable ---------------------------------------------------
        public bool CanInteract => !_activated && !_charging;
        public string Prompt => prompt;
        public Vector3 Position => transform.position;

        public void Interact(GameObject interactor)
        {
            if (!CanInteract) return;
            // CO-OP client: the server owns the mission — request the charge instead of running it.
            if (NetSession.InCoop && !NetSession.IsServer)
            {
                CoopMission.Instance?.RequestGeneratorRpc();
                return;
            }
            ServerInteract();
        }

        /// <summary>Authority-side (server/solo) start of the charge. Called directly in solo, or by
        /// <see cref="CoopMission"/> on the server when a client requests it.</summary>
        public void ServerInteract()
        {
            if (!CanInteract) return;
            StartCoroutine(ChargeRoutine());
        }
        // -----------------------------------------------------------------

        private void Awake()
        {
            // Start unpowered.
            SetLights(false);
            if (enableWhenPowered != null)
                foreach (var go in enableWhenPowered)
                    if (go != null) go.SetActive(false);
        }

        private void OnEnable()
        {
            PlayerInteractor.Register(this);
            // Visuals are driven by the FACT so they fire on every peer (server publishes locally;
            // co-op clients receive it mirrored through CoopMission).
            EventBus.Subscribe<GeneratorActivated>(OnActivatedFact);
        }

        private void OnDisable()
        {
            PlayerInteractor.Unregister(this);
            EventBus.Unsubscribe<GeneratorActivated>(OnActivatedFact);
        }

        private IEnumerator ChargeRoutine()
        {
            _charging = true;
            float t = 0f;
            // Hide the prompt the moment charging starts (CanInteract is already false).
            while (t < chargeTime)
            {
                t += Time.deltaTime;
                EventBus.Publish(new MissionProgress(barLabel, Mathf.Clamp01(t / chargeTime), true));
                yield return null;
            }

            EventBus.Publish(new MissionProgress(barLabel, 1f, false));
            _charging = false;
            EventBus.Publish(new GeneratorActivated()); // fact → director advances + powered visuals
        }

        private void OnActivatedFact(GeneratorActivated _)
        {
            if (_activated) return;
            _activated = true;

            SetLights(true);
            if (enableWhenPowered != null)
                foreach (var go in enableWhenPowered)
                    if (go != null) go.SetActive(true);

            if (humSource != null)
            {
                humSource.loop = true;
                humSource.Play();
            }
            if (startupSfx != null)
                AudioSource.PlayClipAtPoint(startupSfx, transform.position, startupVolume);
        }

        private void SetLights(bool on)
        {
            if (poweredLights == null) return;
            foreach (var l in poweredLights)
                if (l != null) l.enabled = on;
        }
    }
}
