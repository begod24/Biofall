using UnityEngine;

namespace Biofall.Core
{

    public readonly struct CameraShake
    {
        public readonly float Amplitude;

        public CameraShake(float amplitude)
        {
            Amplitude = amplitude;
        }
    }

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

    public readonly struct PlayerDied { }

    public readonly struct AmmoChanged
    {
        public readonly int InMagazine;
        public readonly int InReserve;
        public readonly bool Infinite;

        public AmmoChanged(int inMagazine, int inReserve, bool infinite = false)
        {
            InMagazine = inMagazine;
            InReserve = inReserve;
            Infinite = infinite;
        }
    }

    public readonly struct GrenadeCountChanged
    {
        public readonly int Current;
        public readonly int Max;

        public GrenadeCountChanged(int current, int max)
        {
            Current = current;
            Max = max;
        }
    }

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

    public readonly struct TargetDied
    {
        public readonly GameObject Target;

        public TargetDied(GameObject target)
        {
            Target = target;
        }
    }

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

    public readonly struct PlayerDowned
    {
        public readonly float BleedoutSeconds;
        public PlayerDowned(float bleedoutSeconds) { BleedoutSeconds = bleedoutSeconds; }
    }

    public readonly struct PlayerRevived { }

    public readonly struct PlayerEliminated { }

    public readonly struct ReviveProgress
    {
        public readonly bool Show;
        public readonly float Progress01;
        public ReviveProgress(bool show, float progress01) { Show = show; Progress01 = progress01; }
    }

    public readonly struct TeamWiped { }

    public readonly struct TeammateDowned
    {
        public readonly Transform Player;
        public TeammateDowned(Transform player) { Player = player; }
    }
}
