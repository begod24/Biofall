using UnityEngine;

namespace Biofall.Core
{
    // ------------------------------------------------------------------
    // Central catalogue of game events. Each event is a small readonly
    // struct so publishing through the EventBus produces zero garbage.
    // Add new events here as systems need to talk to each other.
    // ------------------------------------------------------------------

    /// <summary>Player health changed because of damage.</summary>
    public readonly struct PlayerDamaged
    {
        public readonly float Current;
        public readonly float Max;
        public readonly float Amount;

        public PlayerDamaged(float current, float max, float amount)
        {
            Current = current;
            Max = max;
            Amount = amount;
        }
    }

    /// <summary>Player died.</summary>
    public readonly struct PlayerDied { }

    /// <summary>Ammo counts changed (magazine and/or reserve).</summary>
    public readonly struct AmmoChanged
    {
        public readonly int InMagazine;
        public readonly int InReserve;

        public AmmoChanged(int inMagazine, int inReserve)
        {
            InMagazine = inMagazine;
            InReserve = inReserve;
        }
    }

    /// <summary>A weapon fired a shot — useful for VFX/SFX/recoil listeners.</summary>
    public readonly struct WeaponFired
    {
        public readonly Vector3 Origin;
        public readonly Vector3 Direction;

        public WeaponFired(Vector3 origin, Vector3 direction)
        {
            Origin = origin;
            Direction = direction;
        }
    }

    /// <summary>A target took damage.</summary>
    public readonly struct TargetDamaged
    {
        public readonly GameObject Target;
        public readonly float Current;
        public readonly float Amount;

        public TargetDamaged(GameObject target, float current, float amount)
        {
            Target = target;
            Current = current;
            Amount = amount;
        }
    }

    /// <summary>A target died.</summary>
    public readonly struct TargetDied
    {
        public readonly GameObject Target;

        public TargetDied(GameObject target)
        {
            Target = target;
        }
    }

    /// <summary>Bio Samples currency changed.</summary>
    public readonly struct BioSamplesChanged
    {
        public readonly int Total;
        public readonly int Delta;

        public BioSamplesChanged(int total, int delta)
        {
            Total = total;
            Delta = delta;
        }
    }
}
