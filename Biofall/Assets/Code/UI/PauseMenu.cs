using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using Biofall.Core;
using Biofall.Net;

namespace Biofall.UI
{
    public sealed class PauseMenu : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button mainMenuButton;
        [Tooltip("In-pause Settings sub-panel (same controls as the main menu).")]
        [SerializeField] private PauseSettings settings;

        private bool _paused;
        private bool _locked;

        private void Awake()
        {
            Time.timeScale = 1f;
            if (panel != null) panel.SetActive(false);
            if (resumeButton != null) resumeButton.onClick.AddListener(Resume);
            if (settingsButton != null) settingsButton.onClick.AddListener(OpenSettings);
            if (restartButton != null) restartButton.onClick.AddListener(Restart);
            if (mainMenuButton != null) mainMenuButton.onClick.AddListener(ToMainMenu);
            if (settings != null) settings.Closed += OnSettingsClosed;
        }

        private void OnDestroy()
        {
            if (settings != null) settings.Closed -= OnSettingsClosed;
        }

        private void OpenSettings()
        {
            if (panel != null) panel.SetActive(false);
            if (settings != null) settings.Open();
        }

        private void OnSettingsClosed()
        {
            if (panel != null) panel.SetActive(true);
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
                if (settings != null && settings.IsOpen) settings.Close();
                else if (_paused) Resume();
                else Pause();
            }
        }

        private void Pause()
        {
            _paused = true;
            if (!NetSession.InCoop) Time.timeScale = 0f;
            UiOverlay.Active = true;
            Cursor.visible = true;
            if (restartButton != null) restartButton.interactable = !NetSession.InCoop || NetSession.IsServer;
            if (panel != null) panel.SetActive(true);
        }

        private void Resume()
        {
            _paused = false;
            if (!NetSession.InCoop) Time.timeScale = 1f;
            UiOverlay.Active = false;
            if (settings != null && settings.IsOpen) settings.gameObject.SetActive(false);
            if (panel != null) panel.SetActive(false);
        }

        private void Restart()
        {
            Time.timeScale = 1f;
            UiOverlay.Active = false;

            if (NetSession.InCoop)
            {
                if (NetSession.IsServer && CoopSession.Instance != null) CoopSession.Instance.StartGame();
                return;
            }

            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private void ToMainMenu()
        {
            Time.timeScale = 1f;
            UiOverlay.Active = false;

            if (NetSession.InCoop && NetworkBootstrap.Instance != null)
            {
                NetworkBootstrap.Instance.LeaveToMainMenu();
                return;
            }

            SceneManager.LoadScene(GameScenes.MainMenu);
        }
    }
}
