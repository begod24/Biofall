using UnityEngine;
using TMPro;
using Biofall.Core;

namespace Biofall.UI
{
    public sealed class CurrencyUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text text;

        private void Awake()
        {
            if (text == null) text = GetComponent<TMP_Text>();
            Refresh(CurrencyWallet.Total);
        }

        private void OnEnable()
        {
            EventBus.Subscribe<BioSamplesChanged>(OnChanged);
            Refresh(CurrencyWallet.Total);
        }

        private void OnDisable() => EventBus.Unsubscribe<BioSamplesChanged>(OnChanged);

        private void OnChanged(BioSamplesChanged e) => Refresh(e.Total);

        private void Refresh(int total)
        {
            if (text != null) text.text = $"BIO  {total}";
        }
    }
}
