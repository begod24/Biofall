using UnityEngine;
using TMPro;
using Biofall.Core;
using Biofall.Gameplay.Mission1;

namespace Biofall.UI
{
    public sealed class MissionProgressBarUI : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private RectTransform fill;
        [SerializeField] private TMP_Text label;

        private void Awake()
        {
            if (fill == null)
            {
                var t = transform.Find("Fill");
                if (t != null) fill = t as RectTransform;
            }
            if (label == null) label = GetComponentInChildren<TMP_Text>(true);
            if (root == null) root = gameObject;
            HideRootChildren();
        }

        private void OnEnable() => EventBus.Subscribe<MissionProgress>(OnProgress);
        private void OnDisable() => EventBus.Unsubscribe<MissionProgress>(OnProgress);

        private void OnProgress(MissionProgress e)
        {
            SetVisible(e.Active);
            if (!e.Active) return;

            if (fill != null) fill.localScale = new Vector3(Mathf.Clamp01(e.Value01), 1f, 1f);
            if (label != null) label.text = e.Label ?? "";
        }

        private void SetVisible(bool visible)
        {
            if (root != null && root != gameObject) root.SetActive(visible);
            else SetChildrenActive(visible);
        }

        private void HideRootChildren() => SetVisible(false);

        private void SetChildrenActive(bool active)
        {
            for (int i = 0; i < transform.childCount; i++)
                transform.GetChild(i).gameObject.SetActive(active);
        }
    }
}
