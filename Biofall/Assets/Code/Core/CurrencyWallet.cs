namespace Biofall.Core
{
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
