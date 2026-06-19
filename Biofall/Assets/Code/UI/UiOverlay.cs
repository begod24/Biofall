namespace Biofall.UI
{
    /// <summary>
    /// Tiny shared flag: true while a full-screen menu overlay (pause / game over) is showing.
    /// Lets the crosshair release the OS cursor for menus without coupling the systems together.
    /// </summary>
    public static class UiOverlay
    {
        public static bool Active;
    }
}
