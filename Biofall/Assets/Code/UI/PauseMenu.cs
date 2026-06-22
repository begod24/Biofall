using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using Biofall.Core;
using Biofall.Net;

namespace Biofall.UI
{
    /// <summary>
    /// Escape-driven pause. Shows a panel with Resume / Restart / Main Menu. In SOLO it freezes the
    /// game (Time.timeScale = 0); in CO-OP it does NOT freeze time (a host can't stop a live networked
    /// match for everyone, and a client freezing locally would desync) — the panel is an overlay only.
    /// Restart / Main Menu are co-op-aware (host-driven networked reload; LeaveToMainMenu tears the
    /// session), mirroring <see cref="GameOverUI"/> / <see cref="MissionCompleteUI"/>. Disables itself
    /// once the player has died (Game Over owns the screen then). Observer: only listens to PlayerDied.
    /// </summary>
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
            // Back from settings → return to the pause panel.
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
                if (settings != null && settings.IsOpen) settings.Close(); // back to pause panel
                else if (_paused) Resume();
                else Pause();
            }
        }

        private void Pause()
        {
            _paused = true;
            if (!NetSession.InCoop) Time.timeScale = 0f; // co-op: never freeze a live networked match
            UiOverlay.Active = true;
            Cursor.visible = true;
            // Only the host can restart the shared mission; a client's Restart is disabled (no authority).
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
                // Host-authoritative networked reload of the mission for the whole squad. A client has
                // no authority to restart (its button is disabled in Pause), so this is a safe no-op.
                if (NetSession.IsServer && CoopSession.Instance != null) CoopSession.Instance.StartGame();
                return;
            }

            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private void ToMainMenu()
        {
            Time.timeScale = 1f;
            UiOverlay.Active = false;

            // Co-op: tear the session down properly (the host leaving disconnects everyone back to the
            // menu) instead of locally loading a scene while NGO is still live.
            if (NetSession.InCoop && NetworkBootstrap.Instance != null)
            {
                NetworkBootstrap.Instance.LeaveToMainMenu();
                return;
            }

            SceneManager.LoadScene(GameScenes.MainMenu);
        }
    }
}
