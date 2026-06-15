using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using Biofall.Core;

namespace Biofall.UI
{
    /// <summary>
    /// Escape-driven pause. Freezes the game (Time.timeScale = 0) and shows a panel with
    /// Resume / Restart / Main Menu. Disables itself once the player has died (Game Over owns
    /// the screen then). Observer: it only listens to PlayerDied; it doesn't drive gameplay.
    /// </summary>
    public sealed class PauseMenu : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button mainMenuButton;

        private bool _paused;
        private bool _locked;

        private void Awake()
        {
            Time.timeScale = 1f;
            if (panel != null) panel.SetActive(false);
            if (resumeButton != null) resumeButton.onClick.AddListener(Resume);
            if (restartButton != null) restartButton.onClick.AddListener(Restart);
            if (mainMenuButton != null) mainMenuButton.onClick.AddListener(ToMainMenu);
        }

        private void OnEnable() => EventBus.Subscribe<PlayerDied>(OnPlayerDied);
        private void OnDisable() => EventBus.Unsubscribe<PlayerDied>(OnPlayerDied);

        private void OnPlayerDied(PlayerDied _) => _locked = true;

        private void Update()
        {
            if (_locked) return;
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            {
                if (_paused) Resume();
                else Pause();
            }
        }

        private void Pause()
        {
            _paused = true;
            Time.timeScale = 0f;
            UiOverlay.Active = true;
            Cursor.visible = true;
            if (panel != null) panel.SetActive(true);
        }

        private void Resume()
        {
            _paused = false;
            Time.timeScale = 1f;
            UiOverlay.Active = false;
            if (panel != null) panel.SetActive(false);
        }

        private void Restart()
        {
            Time.timeScale = 1f;
            UiOverlay.Active = false;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private void ToMainMenu()
        {
            Time.timeScale = 1f;
            UiOverlay.Active = false;
            SceneManager.LoadScene(GameScenes.MainMenu);
        }
    }
}
