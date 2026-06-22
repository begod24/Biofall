using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Biofall.Core
{
    public sealed class BootSplash : MonoBehaviour
    {
        [Header("Wiring")]
        [Tooltip("CanvasGroup on the banner image (drives fade in/out).")]
        [SerializeField] private CanvasGroup bannerGroup;
        [Tooltip("RectTransform on the banner image (drives the zoom).")]
        [SerializeField] private RectTransform bannerRect;

        [Header("Target")]
        [SerializeField] private string nextScene = GameScenes.MainMenu;

        [Header("Timing (seconds)")]
        [SerializeField] private float punchDuration = 0.45f;
        [SerializeField] private float holdDuration = 1.1f;
        [SerializeField] private float fadeOutDuration = 0.45f;

        [Header("Zoom")]
        [Tooltip("Scale the banner starts at before the punch (smaller = stronger zoom). " +
                 "Eases up to exactly 1.0 and never past it, so the banner is always fully visible.")]
        [SerializeField] private float startScale = 0.82f;

        private void Start()
        {
            Time.timeScale = 1f;
            EventBus.Clear();

            if (bannerGroup == null) bannerGroup = GetComponentInChildren<CanvasGroup>(true);
            if (bannerRect == null && bannerGroup != null) bannerRect = bannerGroup.GetComponent<RectTransform>();

            if (bannerGroup != null) bannerGroup.alpha = 0f;
            if (bannerRect != null) bannerRect.localScale = Vector3.one * startScale;

            StartCoroutine(Run());
        }

        private IEnumerator Run()
        {
            AsyncOperation load = SceneManager.LoadSceneAsync(nextScene);
            load.allowSceneActivation = false;

            yield return Animate(punchDuration, k =>
            {
                if (bannerGroup != null) bannerGroup.alpha = Mathf.Clamp01(k * 3f);
                if (bannerRect != null)
                {
                    float e = EaseOutCubic(k);
                    bannerRect.localScale = Vector3.one * Mathf.Lerp(startScale, 1f, e);
                }
            });
            if (bannerGroup != null) bannerGroup.alpha = 1f;
            if (bannerRect != null) bannerRect.localScale = Vector3.one;

            yield return new WaitForSecondsRealtime(holdDuration);

            yield return Animate(fadeOutDuration, k =>
            {
                if (bannerGroup != null) bannerGroup.alpha = 1f - k;
            });

            while (load.progress < 0.9f) yield return null;
            load.allowSceneActivation = true;
        }

        private static IEnumerator Animate(float duration, Action<float> step)
        {
            if (duration <= 0f)
            {
                step(1f);
                yield break;
            }

            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                step(Mathf.Clamp01(t / duration));
                yield return null;
            }
            step(1f);
        }

        private static float EaseOutCubic(float x)
        {
            float p = 1f - x;
            return 1f - p * p * p;
        }
    }
}
