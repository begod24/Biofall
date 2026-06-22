using UnityEngine;
using TMPro;

namespace Biofall.UI
{
    public sealed class FpsCounterUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text text;
        [Tooltip("How often the displayed value refreshes (seconds).")]
        [SerializeField] private float updateInterval = 0.5f;

        private float _accumulatedTime;
        private int _frames;
        private float _timer;

        private void Awake()
        {
            if (text == null) text = GetComponent<TMP_Text>();
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
