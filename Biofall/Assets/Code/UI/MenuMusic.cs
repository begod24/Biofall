using System.Collections;
using UnityEngine;
using Biofall.Core;

namespace Biofall.UI
{
    [RequireComponent(typeof(AudioSource))]
    public sealed class MenuMusic : MonoBehaviour
    {
        [SerializeField] private AudioSource source;
        [Range(0f, 1f)] [SerializeField] private float targetVolume = 0.4f;
        [SerializeField] private float fadeInTime = 3f;
        [SerializeField] private float fadeOutTime = 3f;
        [Tooltip("Silence between the end of one play and the next.")]
        [SerializeField] private float gapSeconds = 30f;

        private float _fade01;

        private void Awake()
        {
            if (source == null) source = GetComponent<AudioSource>();
            source.loop = false;
            source.playOnAwake = false;
            source.volume = 0f;
        }

        private void OnEnable()
        {
            GameSettings.MusicVolumeChanged += ApplyVolume;
            StartCoroutine(Loop());
        }

        private void OnDisable()
        {
            GameSettings.MusicVolumeChanged -= ApplyVolume;
            StopAllCoroutines();
        }

        private void ApplyVolume()
        {
            if (source != null) source.volume = _fade01 * targetVolume * GameSettings.MusicVolume;
        }

        private IEnumerator Loop()
        {
            if (source.clip == null) yield break;

            while (true)
            {
                _fade01 = 0f; ApplyVolume();
                source.Play();
                yield return Fade(0f, 1f, fadeInTime);

                float steady = Mathf.Max(0f, source.clip.length - fadeInTime - fadeOutTime);
                yield return new WaitForSeconds(steady);

                yield return Fade(_fade01, 0f, fadeOutTime);
                source.Stop();

                yield return new WaitForSeconds(gapSeconds);
            }
        }

        private IEnumerator Fade(float from, float to, float duration)
        {
            if (duration <= 0f) { _fade01 = to; ApplyVolume(); yield break; }

            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                _fade01 = Mathf.Lerp(from, to, t / duration);
                ApplyVolume();
                yield return null;
            }
            _fade01 = to; ApplyVolume();
        }
    }
}
