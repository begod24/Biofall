namespace Biofall.Gameplay
{
    /// <summary>
    /// Polymorphic trigger behaviour: decides whether a shot is REQUESTED this frame from the
    /// raw trigger state. Fire-rate cadence and ammo are enforced by the <see cref="Weapon"/>,
    /// not here. Swap implementations (SingleFire / future AutoFire / BurstFire) to change feel.
    /// </summary>
    public interface IFireStrategy
    {
        bool ShouldFire(bool firePressed, bool fireHeld);
    }
}
