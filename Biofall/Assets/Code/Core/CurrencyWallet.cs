namespace Biofall.Core
{
    /// <summary>
    /// Bio Samples wallet (static, like <see cref="PlayerRegistry"/>). Per-run currency: pickups
    /// call <see cref="Add"/>, the HUD listens to <see cref="BioSamplesChanged"/>. Reset on each
    /// gameplay-scene boot (cross-mission persistence is the later upgrade system).
    /// </summary>
    public static class CurrencyWallet
    {
        public static int Total { get; private set; }

        public static void Add(int amount)
        {
            if (amount == 0) return;
            Total += amount;
            EventBus.Publish(new BioSamplesChanged(Total, amount));
        }

        public static void Reset()
        {
            Total = 0;
            EventBus.Publish(new BioSamplesChanged(0, 0));
        }
    }
}
