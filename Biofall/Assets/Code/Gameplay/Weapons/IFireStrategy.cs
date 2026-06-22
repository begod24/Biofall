namespace Biofall.Gameplay
{
    public interface IFireStrategy
    {
        bool ShouldFire(bool firePressed, bool fireHeld);
    }
}
