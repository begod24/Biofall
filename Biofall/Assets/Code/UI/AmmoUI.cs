using UnityEngine;
using TMPro;
using Biofall.Core;

namespace Biofall.UI
{
    public sealed class AmmoUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text text;

        private void Awake()
        {
            if (text == null) text = GetComponent<TMP_Text>();
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
            if (text == null) return;
            text.text = e.Infinite ? "∞" : $"{e.InMagazine} / {e.InReserve}";
        }
    }
}
