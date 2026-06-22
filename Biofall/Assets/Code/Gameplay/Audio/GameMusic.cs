using System.Collections;
using UnityEngine;
using Biofall.Core;

namespace Biofall.Gameplay
{
    [RequireComponent(typeof(AudioSource))]
    public sealed class GameMusic : MonoBehaviour
    {
        [SerializeField] private AudioSource source;
        [SerializeField] private AudioClip[] tracks;
        [Range(0f, 1f)] [SerializeField] private float targetVolume = 0.35f;
        [SerializeField] private float fadeInTime = 2f;
        [SerializeField] private float fadeOutTime = 2f;
        [Tooltip("Random silence between two tracks (seconds).")]
        [SerializeField] private float minGap = 15f;
        [SerializeField] private float maxGap = 20f;

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
            if (tracks == null || tracks.Length == 0) yield break;

            while (true)
            {
                AudioClip clip = tracks[Random.Range(0, tracks.Length)];
                if (clip == null) { yield return null; continue; }

                source.clip = clip;
                _fade01 = 0f; ApplyVolume();
                source.Play();
                yield return Fade(0f, 1f, fadeInTime);

                float steady = Mathf.Max(0f, clip.length - fadeInTime - fadeOutTime);
                yield return new WaitForSeconds(steady);

                yield return Fade(_fade01, 0f, fadeOutTime);
                source.Stop();

                yield return new WaitForSeconds(Random.Range(minGap, maxGap));
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
