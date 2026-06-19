namespace Biofall.Gameplay.Mission1
{
    // ------------------------------------------------------------------
    // Mission 1 event catalogue. Like the global GameEvents, each event is
    // a tiny readonly struct published through the EventBus (zero garbage).
    // The MissionDirector owns the flow; stations publish "facts" and the
    // director answers with MissionPhaseChanged. UI only listens.
    // ------------------------------------------------------------------

    /// <summary>Ordered stages of Mission 1.</summary>
    public enum MissionPhase
    {
        FindGenerator,  // start: find & power the generator
        ActivateBeacon, // generator on → walk to the beacon and switch it on
        DefendBeacon,   // beacon switched on → hold the zone while it charges
        Extract,        // beacon charged → reach the extraction point
        Completed       // extracted
    }

    /// <summary>The mission advanced to a new phase. UI + stations react to this.</summary>
    public readonly struct MissionPhaseChanged
    {
        public readonly MissionPhase Phase;
        public MissionPhaseChanged(MissionPhase phase) { Phase = phase; }
    }

    /// <summary>Fact: the player finished powering the generator.</summary>
    public readonly struct GeneratorActivated { }

    /// <summary>Fact: the player switched the beacon on — its defense charge begins now.</summary>
    public readonly struct BeaconActivated { }

    /// <summary>Fact: the beacon finished charging (defense survived).</summary>
    public readonly struct BeaconCharged { }

    /// <summary>Fact: the player held the extraction point long enough — mission won.</summary>
    public readonly struct MissionCompleted { }

    /// <summary>
    /// Drives the shared on-screen progress bar (generator charge / beacon defense /
    /// extraction countdown). Publish with active=false to hide it. <see cref="Label"/>
    /// should be a cached string so per-frame publishing stays allocation-free.
    /// </summary>
    public readonly struct MissionProgress
    {
        public readonly string Label;
        public readonly float Value01;
        public readonly bool Active;

        public MissionProgress(string label, float value01, bool active)
        {
            Label = label;
            Value01 = value01;
            Active = active;
        }
    }
}
