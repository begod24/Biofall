using UnityEngine;

namespace Biofall.Core
{
    // ------------------------------------------------------------------
    // Central catalogue of game events. Each event is a small readonly
    // struct so publishing through the EventBus produces zero garbage.
    // Add new events here as systems need to talk to each other.
    // ------------------------------------------------------------------

    /// <summary>Request a camera shake (e.g. explosions, the Screamer's wave) independent of damage.</summary>
    public readonly struct CameraShake
    {
        public readonly float Amplitude;

        public CameraShake(float amplitude)
        {
            Amplitude = amplitude;
        }
    }

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

    /// <summary>Ammo counts changed (magazine and/or reserve). <see cref="Infinite"/> = the active
    /// weapon never runs out (HUD shows ∞ and ignores the numbers).</summary>
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

    /// <summary>Player's grenade count changed (current of max).</summary>
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

    // ------------------------------------------------------------------
    // CO-OP downed / revive (Phase E). All of these are LOCAL events on the
    // affected machine so the existing UI pattern (UI only listens) holds.
    // Solo never publishes them (the solo death path uses PlayerDied).
    // ------------------------------------------------------------------

    /// <summary>This machine's player just went DOWN (co-op). They bleed out in
    /// <see cref="BleedoutSeconds"/> unless a teammate revives them first.</summary>
    public readonly struct PlayerDowned
    {
        public readonly float BleedoutSeconds;
        public PlayerDowned(float bleedoutSeconds) { BleedoutSeconds = bleedoutSeconds; }
    }

    /// <summary>This machine's player was revived and is back in the fight.</summary>
    public readonly struct PlayerRevived { }

    /// <summary>This machine's player bled out / was fully eliminated, but the squad lives on
    /// (they wait for the survivors to extract). Distinct from a full team wipe.</summary>
    public readonly struct PlayerEliminated { }

    /// <summary>Reviver-side HUD feed: a downed teammate is in range (<see cref="Show"/>) and
    /// how far the hold-to-revive has progressed (<see cref="Progress01"/>).</summary>
    public readonly struct ReviveProgress
    {
        public readonly bool Show;
        public readonly float Progress01;
        public ReviveProgress(bool show, float progress01) { Show = show; Progress01 = progress01; }
    }

    /// <summary>Every player is down/dead at once — the run is lost. Shown as a team Game Over.</summary>
    public readonly struct TeamWiped { }

    /// <summary>A TEAMMATE (not this machine's own player) just went DOWN. Fired on every peer that
    /// sees the state change, so the squad HUD / on-screen marker / "man down" SFX can react.</summary>
    public readonly struct TeammateDowned
    {
        public readonly Transform Player;
        public TeammateDowned(Transform player) { Player = player; }
    }
}
