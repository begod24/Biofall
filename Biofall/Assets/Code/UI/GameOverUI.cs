using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using Biofall.Core;
using Biofall.Net;

namespace Biofall.UI
{
    /// <summary>
    /// Observer: shows a Game Over panel on <see cref="PlayerDied"/> with Restart and Main Menu.
    /// Restart also works via Enter/Space. Pure UI — it only listens, it never drives gameplay.
    /// </summary>
    public sealed class GameOverUI : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button mainMenuButton;

        [Header("Audio")]
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioClip gameOverSfx;
        [Range(0f, 1f)] [SerializeField] private float gameOverVolume = 0.5f;

        [Header("Timing")]
        [Tooltip("Delay (seconds, real time) before the Game Over panel appears.")]
        [SerializeField] private float showDelay = 3f;

        private bool _shown;

        private void Awake()
        {
            if (panel != null) panel.SetActive(false);
            if (restartButton != null) restartButton.onClick.AddListener(Restart);
            if (mainMenuButton != null) mainMenuButton.onClick.AddListener(ToMainMenu);
        }

        private void OnEnable()
        {
            EventBus.Subscribe<PlayerDied>(OnPlayerDied);
            EventBus.Subscribe<TeamWiped>(OnTeamWiped);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<PlayerDied>(OnPlayerDied);
            EventBus.Unsubscribe<TeamWiped>(OnTeamWiped);
        }

        private void OnPlayerDied(PlayerDied _)
        {
            // Solo only — in co-op PlayerDied isn't published (HP 0 = downed, see CoopPlayerLife).
            // Let the death play out for a moment before showing the panel.
            if (showDelay > 0f) StartCoroutine(ShowAfterDelay());
            else Show();
        }

        // CO-OP: the whole squad is down/dead — this is the real Game Over for the run.
        private void OnTeamWiped(TeamWiped _)
        {
            if (_shown) return;
            if (showDelay > 0f) StartCoroutine(ShowAfterDelay());
            else Show();
        }

        private IEnumerator ShowAfterDelay()
        {
            yield return new WaitForSecondsRealtime(showDelay);
            Show();
        }

        private void Show()
        {
            _shown = true;
            if (panel != null) panel.SetActive(true);
            UiOverlay.Active = true;     // release the OS cursor for the buttons
            Cursor.visible = true;
            // Co-op death = downed/revive (Phase E); a lone local Restart would desync the networked
            // mission, so disable it and let the player leave via Main Menu. Solo keeps Restart.
            if (restartButton != null) restartButton.interactable = !NetSession.InCoop;
            if (sfxSource != null && gameOverSfx != null)
                sfxSource.PlayOneShot(gameOverSfx, gameOverVolume);
        }

        private void Update()
        {
            if (!_shown || NetSession.InCoop) return; // Enter/Space restart is solo-only
            var keyboard = Keyboard.current;
            if (keyboard != null && (keyboard.enterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame))
                Restart();
        }

        private void Restart()
        {
            if (NetSession.InCoop) return; // guarded: co-op restart is host-driven (Phase E)
            Time.timeScale = 1f;
            UiOverlay.Active = false;
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
