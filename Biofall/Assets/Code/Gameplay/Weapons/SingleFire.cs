namespace Biofall.Gameplay
{
    /// <summary>Semi-auto: one shot per trigger press (the pistol).</summary>
    public sealed class SingleFire : IFireStrategy
    {
        public bool ShouldFire(bool firePressed, bool fireHeld) => firePressed;
    }
}
