using UnityEngine;
using Biofall.Core;
using Biofall.Gameplay.Mission1;

namespace Biofall.Gameplay
{
    /// <summary>
    /// On mission success, banks the run's collected Bio Samples (<see cref="CurrencyWallet.Total"/>)
    /// into the persistent <see cref="PlayerProgression"/> pool so they can be spent on upgrades between
    /// missions. Listens to the LOCAL <see cref="MissionCompleted"/> event, which fires on EVERY peer in
    /// co-op (mirrored by CoopMission) — so each player banks their own wallet → progression stays
    /// per-player. Fires once per run. Pure observer: it only reads the wallet and writes the save.
    /// </summary>
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
