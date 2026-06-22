using UnityEngine;
using TMPro;

namespace Biofall.UI
{
    [RequireComponent(typeof(TMP_Text))]
    public sealed class TitleGlowPulse : MonoBehaviour
    {
        [SerializeField] private TMP_Text target;

        [Header("Pulse")]
        [Tooltip("Seconds for one full breathe (in + out) cycle.")]
        [SerializeField] private float period = 2.5f;

        [Header("Glow")]
        [SerializeField] private Color glowColor = new Color(0.8f, 0.12f, 0.16f, 1f);
        [SerializeField] private float glowPowerMin = 0.05f;
        [SerializeField] private float glowPowerMax = 0.55f;
        [SerializeField] private float glowOuterMin = 0.10f;
        [SerializeField] private float glowOuterMax = 0.70f;

        private Material _mat;
        private static readonly int GlowColorId = Shader.PropertyToID("_GlowColor");
        private static readonly int GlowPowerId = Shader.PropertyToID("_GlowPower");
        private static readonly int GlowOuterId = Shader.PropertyToID("_GlowOuter");
        private static readonly int GlowInnerId = Shader.PropertyToID("_GlowInner");

        private void Awake()
        {
            if (target == null) target = GetComponent<TMP_Text>();
            if (target == null) return;

            _mat = target.fontMaterial;
            _mat.EnableKeyword("GLOW_ON");
            _mat.SetColor(GlowColorId, glowColor);
            _mat.SetFloat(GlowInnerId, 0.05f);
        }

        private void Update()
        {
            if (_mat == null) return;

            float phase = Time.unscaledTime * (Mathf.PI * 2f / Mathf.Max(0.01f, period));
            float t = Mathf.Sin(phase) * 0.5f + 0.5f;

            _mat.SetColor(GlowColorId, glowColor);
            _mat.SetFloat(GlowPowerId, Mathf.Lerp(glowPowerMin, glowPowerMax, t));
            _mat.SetFloat(GlowOuterId, Mathf.Lerp(glowOuterMin, glowOuterMax, t));
        }
    }
}
