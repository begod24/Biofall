using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Biofall.Core;

namespace Biofall.UI
{
    /// <summary>
    /// Main menu controller: Play → gameplay scene, Credits → toggle panel, Exit → quit.
    /// Wires its own buttons in Awake (no editor UnityEvent plumbing needed).
    /// </summary>
    public sealed class MainMenuUI : MonoBehaviour
    {
        [SerializeField] private Button playButton;
        [SerializeField] private Button creditsButton;
        [SerializeField] private Button exitButton;
        [SerializeField] private Button creditsBackButton;
        [SerializeField] private GameObject creditsPanel;

        private void Awake()
        {
            Time.timeScale = 1f;
            UiOverlay.Active = false;
            Cursor.visible = true;
            if (creditsPanel != null) creditsPanel.SetActive(false);

            if (playButton != null) playButton.onClick.AddListener(Play);
            if (creditsButton != null) creditsButton.onClick.AddListener(ShowCredits);
            if (exitButton != null) exitButton.onClick.AddListener(Exit);
            if (creditsBackButton != null) creditsBackButton.onClick.AddListener(HideCredits);
        }

        private void Play() => SceneManager.LoadScene(GameScenes.Gameplay);
        private void ShowCredits() { if (creditsPanel != null) creditsPanel.SetActive(true); }
        private void HideCredits() { if (creditsPanel != null) creditsPanel.SetActive(false); }

        private void Exit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
