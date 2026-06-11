namespace Biofall.Core
{
    /// <summary>High-level game flow states. Driven by <see cref="GameManager"/>.</summary>
    public enum GameState
    {
        Boot,
        MainMenu,
        Loading,
        MissionIntro,
        Playing,
        Paused,
        MissionSuccess,
        MissionFailed,
        Results
    }
}
