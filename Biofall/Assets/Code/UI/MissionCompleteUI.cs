using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Biofall.Core;
using Biofall.Gameplay.Mission1;
using Biofall.Net;

namespace Biofall.UI
{
    /// <summary>
    /// Observer: shows the "MISSION COMPLETE" panel on <see cref="MissionCompleted"/>, styled
    /// like the Game Over / Pause panels. Offers Replay and Main Menu. Pure UI — only listens.
    /// </summary>
    public sealed class MissionCompleteUI : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private Button replayButton;
        [SerializeField] private Button mainMenuButton;

        [Header("Audio")]
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioClip completeSfx;
        [Range(0f, 1f)] [SerializeField] private float completeVolume = 0.6f;

        [Header("Timing")]
        [Tooltip("Delay (real seconds) before the panel appears.")]
        [SerializeField] private float showDelay = 1.2f;

        private void Awake()
        {
            if (panel != null) panel.SetActive(false);
            if (replayButton != null) replayButton.onClick.AddListener(Replay);
            if (mainMenuButton != null) mainMenuButton.onClick.AddListener(ToMainMenu);
        }

        private void OnEnable() => EventBus.Subscribe<MissionCompleted>(OnCompleted);
        private void OnDisable() => EventBus.Unsubscribe<MissionCompleted>(OnCompleted);

        private void OnCompleted(MissionCompleted _)
        {
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
            if (panel != null) panel.SetActive(true);
            UiOverlay.Active = true;
            Cursor.visible = true;
            // Co-op: only the host can replay the shared mission (networked reload); a client without
            // authority can't drive it, so its Replay is disabled and it leaves via Main Menu instead.
            if (replayButton != null) replayButton.interactable = !NetSession.InCoop || NetSession.IsServer;
            if (sfxSource != null && completeSfx != null)
                sfxSource.PlayOneShot(completeSfx, completeVolume);
        }

        private void Replay()
        {
            Time.timeScale = 1f;
            UiOverlay.Active = false;

            if (NetSession.InCoop)
            {
                // Host-authoritative networked reload of the mission for the whole squad.
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
