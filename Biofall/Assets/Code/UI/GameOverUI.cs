using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using Biofall.Core;

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

        private void OnEnable() => EventBus.Subscribe<PlayerDied>(OnPlayerDied);
        private void OnDisable() => EventBus.Unsubscribe<PlayerDied>(OnPlayerDied);

        private void OnPlayerDied(PlayerDied _)
        {
            // Let the death play out for a moment before showing the panel.
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
            if (sfxSource != null && gameOverSfx != null)
                sfxSource.PlayOneShot(gameOverSfx, gameOverVolume);
        }

        private void Update()
        {
            if (!_shown) return;
            var keyboard = Keyboard.current;
            if (keyboard != null && (keyboard.enterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame))
                Restart();
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
