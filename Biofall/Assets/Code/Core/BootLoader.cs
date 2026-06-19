using UnityEngine;
using UnityEngine.SceneManagement;

namespace Biofall.Core
{
    /// <summary>
    /// Entry point. The Boot scene is intentionally tiny so it loads instantly; it resets global
    /// state and then hands off to the main menu. Keeping a dedicated boot scene means the heavy
    /// gameplay scene is never the first thing loaded (faster startup, clean init seam).
    /// </summary>
    public sealed class BootLoader : MonoBehaviour
    {
        [SerializeField] private string nextScene = GameScenes.MainMenu;

        private void Start()
        {
            Time.timeScale = 1f;
            EventBus.Clear(); // safe baseline before anything subscribes
            SceneManager.LoadScene(nextScene);
        }
    }
}
