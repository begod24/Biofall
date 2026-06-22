using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Biofall.UI
{
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
