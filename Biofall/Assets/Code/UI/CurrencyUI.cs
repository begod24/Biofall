using UnityEngine;
using UnityEngine.UI;
using Biofall.Core;

namespace Biofall.UI
{
    /// <summary>
    /// Observer: shows collected Bio Samples from <see cref="BioSamplesChanged"/>.
    /// Pure listener — never touches the wallet.
    /// </summary>
    public sealed class CurrencyUI : MonoBehaviour
    {
        [SerializeField] private Text text;

        private void Awake()
        {
            if (text == null) text = GetComponent<Text>();
            if (text != null && text.font == null)
                text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
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
