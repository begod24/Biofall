using UnityEngine;
using UnityEngine.UI;

namespace Biofall.UI
{
    public class HealthBarUI : MonoBehaviour
    {
        [SerializeField] private Image fill;
        [SerializeField] private float lerpSpeed = 8f;

        private float target;
        private float current;

        public void SetValue(float normalized)
        {
            target = Mathf.Clamp01(normalized);
        }

        private void Update()
        {
            if (fill == null) return;
            current = Mathf.Lerp(current, target, Time.deltaTime * lerpSpeed);
            fill.fillAmount = current;
        }
    }
}
