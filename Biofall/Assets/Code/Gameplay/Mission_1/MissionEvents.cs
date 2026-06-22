namespace Biofall.Gameplay.Mission1
{

    public enum MissionPhase
    {
        FindGenerator,
        ActivateBeacon,
        DefendBeacon,
        Extract,
        Completed
    }

    public readonly struct MissionPhaseChanged
    {
        public readonly MissionPhase Phase;
        public MissionPhaseChanged(MissionPhase phase) { Phase = phase; }
    }

    public readonly struct GeneratorActivated { }

    public readonly struct BeaconActivated { }

    public readonly struct BeaconCharged { }

    public readonly struct MissionCompleted { }

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
