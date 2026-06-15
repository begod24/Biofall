using UnityEngine;
using UnityEngine.UI;

namespace Biofall.UI
{
    /// <summary>
    /// Smoothed frames-per-second readout. Independent of gameplay and the EventBus —
    /// it just samples frame time and refreshes the label on a fixed interval.
    /// </summary>
    public sealed class FpsCounterUI : MonoBehaviour
    {
        [SerializeField] private Text text;
        [Tooltip("How often the displayed value refreshes (seconds).")]
        [SerializeField] private float updateInterval = 0.5f;

        private float _accumulatedTime;
        private int _frames;
        private float _timer;

        private void Awake()
        {
            if (text == null) text = GetComponent<Text>();
            if (text != null && text.font == null)
                text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        private void Update()
        {
            _accumulatedTime += Time.unscaledDeltaTime;
            _frames++;
            _timer += Time.unscaledDeltaTime;

            if (_timer < updateInterval) return;

            float fps = _accumulatedTime > 0f ? _frames / _accumulatedTime : 0f;
            if (text != null) text.text = $"FPS {Mathf.RoundToInt(fps)}";

            _accumulatedTime = 0f;
            _frames = 0;
            _timer = 0f;
        }
    }
}
