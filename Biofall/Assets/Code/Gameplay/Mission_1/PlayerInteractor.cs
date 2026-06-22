using System.Collections.Generic;
using UnityEngine;
using Biofall.Core;
using Biofall.UI;

namespace Biofall.Gameplay.Mission1
{
    public readonly struct InteractPromptChanged
    {
        public readonly string Prompt;
        public readonly bool Visible;

        public InteractPromptChanged(string prompt, bool visible)
        {
            Prompt = prompt;
            Visible = visible;
        }
    }

    [RequireComponent(typeof(PlayerInput))]
    public sealed class PlayerInteractor : MonoBehaviour
    {
        [Tooltip("How close (metres) the player must be to use an interactable.")]
        [SerializeField] private float interactRange = 3f;

        private static readonly List<IInteractable> s_registry = new(16);

        private PlayerInput _input;
        private IInteractable _current;
        private bool _promptVisible;

        public static void Register(IInteractable it)
        {
            if (it != null && !s_registry.Contains(it)) s_registry.Add(it);
        }

        public static void Unregister(IInteractable it)
        {
            s_registry.Remove(it);
        }

        private void Awake() => _input = GetComponent<PlayerInput>();

        private void Update()
        {
            if (UiOverlay.Active || Time.timeScale <= 0f)
            {
                SetPrompt(null, false);
                _current = null;
                return;
            }

            _current = FindNearest();

            if (_current != null)
            {
                SetPrompt(_current.Prompt, true);
                if (_input.InteractPressed)
                    _current.Interact(gameObject);
            }
            else
            {
                SetPrompt(null, false);
            }
        }

        private IInteractable FindNearest()
        {
            IInteractable best = null;
            float bestSqr = interactRange * interactRange;
            Vector3 here = transform.position;

            for (int i = 0; i < s_registry.Count; i++)
            {
                IInteractable it = s_registry[i];
                if (it == null || !it.CanInteract) continue;

                float sqr = (it.Position - here).sqrMagnitude;
                if (sqr <= bestSqr)
                {
                    bestSqr = sqr;
                    best = it;
                }
            }
            return best;
        }

        private void SetPrompt(string prompt, bool visible)
        {
            if (visible == _promptVisible && (!visible || prompt == _lastPrompt)) return;
            _promptVisible = visible;
            _lastPrompt = prompt;
            EventBus.Publish(new InteractPromptChanged(prompt, visible));
        }

        private string _lastPrompt;

        private void OnDisable()
        {
            if (_promptVisible)
            {
                _promptVisible = false;
                EventBus.Publish(new InteractPromptChanged(null, false));
            }
        }
    }
}
