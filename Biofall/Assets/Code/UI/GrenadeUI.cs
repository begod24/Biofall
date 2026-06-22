using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Biofall.Core;

namespace Biofall.UI
{
    public sealed class GrenadeUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text text;
        [SerializeField] private Graphic icon;

        private void Awake()
        {
            if (text == null) text = GetComponent<TMP_Text>();
        }

        private void OnEnable() => EventBus.Subscribe<GrenadeCountChanged>(OnChanged);
        private void OnDisable() => EventBus.Unsubscribe<GrenadeCountChanged>(OnChanged);

        private void OnChanged(GrenadeCountChanged e)
        {
            if (text != null) text.text = $"GRENADE x{e.Current}";
            if (icon != null)
            {
                Color c = icon.color;
                c.a = e.Current > 0 ? 1f : 0.3f;
                icon.color = c;
            }
        }
    }
}
