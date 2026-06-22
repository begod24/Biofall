using UnityEngine;
using UnityEngine.SceneManagement;

namespace Biofall.Core
{
    public sealed class BootLoader : MonoBehaviour
    {
        [SerializeField] private string nextScene = GameScenes.MainMenu;

        private void Start()
        {
            Time.timeScale = 1f;
            EventBus.Clear();
            SceneManager.LoadScene(nextScene);
        }
    }
}
