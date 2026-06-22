namespace Biofall.Gameplay
{
    public sealed class SingleFire : IFireStrategy
    {
        public bool ShouldFire(bool firePressed, bool fireHeld) => firePressed;
    }
}
