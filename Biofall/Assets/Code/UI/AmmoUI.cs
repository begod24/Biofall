using UnityEngine;
using UnityEngine.UI;
using Biofall.Core;

namespace Biofall.UI
{
    /// <summary>
    /// Observer: shows "magazine / reserve" from <see cref="AmmoChanged"/>.
    /// Pure listener — it never touches the ammo system.
    /// </summary>
    public sealed class AmmoUI : MonoBehaviour
    {
        [SerializeField] private Text text;

        private void Awake()
        {
            if (text == null) text = GetComponent<Text>();
            if (text != null && text.font == null)
                text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        private void OnEnable()
        {
            EventBus.Subscribe<AmmoChanged>(OnAmmoChanged);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<AmmoChanged>(OnAmmoChanged);
        }

        private void OnAmmoChanged(AmmoChanged e)
        {
            if (text != null) text.text = $"{e.InMagazine} / {e.InReserve}";
        }
    }
}
