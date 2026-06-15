using System.Collections;
using UnityEngine;

namespace Biofall.UI
{
    /// <summary>
    /// Main-menu music loop: fades in slowly, plays through, fades out near the end, then waits
    /// <see cref="gapSeconds"/> before playing again. Kept quiet via <see cref="targetVolume"/>.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public sealed class MenuMusic : MonoBehaviour
    {
        [SerializeField] private AudioSource source;
        [Range(0f, 1f)] [SerializeField] private float targetVolume = 0.4f;
        [SerializeField] private float fadeInTime = 3f;
        [SerializeField] private float fadeOutTime = 3f;
        [Tooltip("Silence between the end of one play and the next.")]
        [SerializeField] private float gapSeconds = 30f;

        private void Awake()
        {
            if (source == null) source = GetComponent<AudioSource>();
            source.loop = false;
            source.playOnAwake = false;
            source.volume = 0f;
        }

        private void OnEnable() => StartCoroutine(Loop());
        private void OnDisable() => StopAllCoroutines();

        private IEnumerator Loop()
        {
            if (source.clip == null) yield break;

            while (true)
            {
                source.volume = 0f;
                source.Play();
                yield return Fade(0f, targetVolume, fadeInTime);

                float steady = Mathf.Max(0f, source.clip.length - fadeInTime - fadeOutTime);
                yield return new WaitForSeconds(steady);

                yield return Fade(source.volume, 0f, fadeOutTime);
                source.Stop();

                yield return new WaitForSeconds(gapSeconds);
            }
        }

        private IEnumerator Fade(float from, float to, float duration)
        {
            if (duration <= 0f) { source.volume = to; yield break; }

            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                source.volume = Mathf.Lerp(from, to, t / duration);
                yield return null;
            }
            source.volume = to;
        }
    }
}
