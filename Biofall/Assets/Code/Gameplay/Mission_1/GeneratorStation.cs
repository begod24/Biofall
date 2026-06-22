using System.Collections;
using UnityEngine;
using Biofall.Core;
using Biofall.Net;

namespace Biofall.Gameplay.Mission1
{
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

        public bool CanInteract => !_activated && !_charging;
        public string Prompt => prompt;
        public Vector3 Position => transform.position;

        public void Interact(GameObject interactor)
        {
            if (!CanInteract) return;
            if (NetSession.InCoop && !NetSession.IsServer)
            {
                CoopMission.Instance?.RequestGeneratorRpc();
                return;
            }
            ServerInteract();
        }

        public void ServerInteract()
        {
            if (!CanInteract) return;
            StartCoroutine(ChargeRoutine());
        }

        private void Awake()
        {
            SetLights(false);
            if (enableWhenPowered != null)
                foreach (var go in enableWhenPowered)
                    if (go != null) go.SetActive(false);
        }

        private void OnEnable()
        {
            PlayerInteractor.Register(this);
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
            while (t < chargeTime)
            {
                t += Time.deltaTime;
                EventBus.Publish(new MissionProgress(barLabel, Mathf.Clamp01(t / chargeTime), true));
                yield return null;
            }

            EventBus.Publish(new MissionProgress(barLabel, 1f, false));
            _charging = false;
            EventBus.Publish(new GeneratorActivated());
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
