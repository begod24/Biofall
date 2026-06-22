using UnityEngine;
using TMPro;
using Biofall.Core;

namespace Biofall.UI
{
    public sealed class HealthBarUI : MonoBehaviour
    {
        [SerializeField] private RectTransform fill;
        [SerializeField] private TMP_Text label;

        private void Awake()
        {
            if (fill == null)
            {
                Transform fillTransform = transform.Find("Fill");
                if (fillTransform != null) fill = fillTransform as RectTransform;
            }
            if (label == null) label = GetComponentInChildren<TMP_Text>();
        }

        private void OnEnable()
        {
            EventBus.Subscribe<PlayerDamaged>(OnPlayerDamaged);
            EventBus.Subscribe<PlayerDied>(OnPlayerDied);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<PlayerDamaged>(OnPlayerDamaged);
            EventBus.Unsubscribe<PlayerDied>(OnPlayerDied);
        }

        private void OnPlayerDamaged(PlayerDamaged e)
        {
            float ratio = e.Max > 0f ? Mathf.Clamp01(e.Current / e.Max) : 0f;
            if (fill != null) fill.localScale = new Vector3(ratio, 1f, 1f);
            if (label != null) label.text = $"{Mathf.CeilToInt(e.Current)} / {Mathf.CeilToInt(e.Max)}";
        }

        private void OnPlayerDied(PlayerDied e)
        {
            if (fill != null) fill.localScale = new Vector3(0f, 1f, 1f);
            if (label != null) label.text = "DEAD";
        }
    }
}
