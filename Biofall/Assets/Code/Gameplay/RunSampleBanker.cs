using UnityEngine;
using Biofall.Core;
using Biofall.Gameplay.Mission1;

namespace Biofall.Gameplay
{
    public sealed class RunSampleBanker : MonoBehaviour
    {
        private bool _banked;

        private void OnEnable() => EventBus.Subscribe<MissionCompleted>(OnCompleted);
        private void OnDisable() => EventBus.Unsubscribe<MissionCompleted>(OnCompleted);

        private void OnCompleted(MissionCompleted _)
        {
            if (_banked) return;
            _banked = true;
            PlayerProgression.DepositRunSamples(CurrencyWallet.Total);
        }
    }
}
