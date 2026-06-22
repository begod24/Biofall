using UnityEngine;
using TMPro;
using Biofall.Gameplay;

namespace Biofall.UI
{
    /// <summary>
    /// Minimal HUD label that shows the current sandbox wave ("WAVE N"). Subscribes to
    /// <see cref="WaveSpawner.WaveStarted"/>; harmless in scenes without a WaveSpawner (shows 1).
    /// Uses legacy UI Text to match the rest of the HUD.
    /// </summary>
    public sealed class WaveHud : MonoBehaviour
    {
        [SerializeField] private TMP_Text label;
        [SerializeField] private string format = "WAVE {0}";

        private void Awake()
        {
            if (label == null) label = GetComponent<TMP_Text>();
        }

        private void OnEnable()
        {
            WaveSpawner.WaveStarted += OnWave;
            Refresh(Mathf.Max(1, WaveSpawner.CurrentWave));
        }

        private void OnDisable() => WaveSpawner.WaveStarted -= OnWave;

        private void OnWave(int wave) => Refresh(wave);

        private void Refresh(int wave)
        {
            if (label != null) label.text = string.Format(format, wave);
        }
    }
}
