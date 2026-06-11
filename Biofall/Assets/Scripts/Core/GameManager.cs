using Biofall.Utilities;
using UnityEngine;

namespace Biofall.Core
{
    /// <summary>
    /// Owns the high-level <see cref="GameState"/> and broadcasts changes via
    /// <see cref="GameEvents.OnGameStateChanged"/>. Holds no gameplay logic itself.
    /// </summary>
    public class GameManager : MonoSingleton<GameManager>
    {
        [SerializeField] private GameState _startState = GameState.Playing;

        public GameState State { get; private set; }

        protected override void OnSingletonAwake()
        {
            SetState(_startState);
        }

        public void SetState(GameState next)
        {
            if (State == next) return;
            State = next;
            GameEvents.RaiseGameStateChanged(next);
        }

        public void TogglePause()
        {
            switch (State)
            {
                case GameState.Playing:
                    SetState(GameState.Paused);
                    Time.timeScale = 0f;
                    break;
                case GameState.Paused:
                    Time.timeScale = 1f;
                    SetState(GameState.Playing);
                    break;
            }
        }
    }
}
