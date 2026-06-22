using UnityEngine;
using TMPro;
using Biofall.Core;
using Biofall.Gameplay.Mission1;

namespace Biofall.UI
{
    public sealed class InteractionPromptUI : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private TMP_Text text;
        [SerializeField] private string keyHint = "E";

        private void Awake()
        {
            if (text == null) text = GetComponentInChildren<TMP_Text>(true);
            if (root == null && text != null) root = text.gameObject;
            Hide();
        }

        private void OnEnable() => EventBus.Subscribe<InteractPromptChanged>(OnPrompt);
        private void OnDisable() => EventBus.Unsubscribe<InteractPromptChanged>(OnPrompt);

        private void OnPrompt(InteractPromptChanged e)
        {
            if (!e.Visible || string.IsNullOrEmpty(e.Prompt))
            {
                Hide();
                return;
            }
            if (text != null) text.text = $"[{keyHint}]  {e.Prompt}";
            if (root != null) root.SetActive(true);
        }

        private void Hide()
        {
            if (root != null) root.SetActive(false);
        }
    }
}
