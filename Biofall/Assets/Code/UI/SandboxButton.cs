using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Biofall.UI
{
    /// <summary>
    /// Drop-in button handler that loads a scene by name (used to hang the sandbox on the menu's
    /// CONTINUE button without modifying MainMenuUI). The scene must be in Build Settings.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public sealed class SandboxButton : MonoBehaviour
    {
        [SerializeField] private string sceneName = "NewLevel";

        private void Awake() => GetComponent<Button>().onClick.AddListener(Load);

        private void Load()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(sceneName);
        }
    }
}
