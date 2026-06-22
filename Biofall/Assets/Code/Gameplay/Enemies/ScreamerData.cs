using UnityEngine;

namespace Biofall.Gameplay
{
    [CreateAssetMenu(menuName = "Biofall/Screamer Data", fileName = "EN_Screamer")]
    public sealed class ScreamerData : EnemyData
    {
        [Header("Scream wave")]
        [Tooltip("Damage dealt to the player if inside the wave radius when it lands.")]
        public float waveDamage = 10f;
        [Tooltip("Radius (metres) within which the wave hurts the player.")]
        public float waveRadius = 10f;
        [Tooltip("Pooled, pulsing dark-red ring VFX spawned at the Screamer's feet.")]
        public GameObject waveVfxPrefab;
        [Tooltip("Seconds the wave VFX takes to expand to its full radius.")]
        public float waveExpandDuration = 0.6f;
        [Tooltip("Delay from the scream starting to the first wave going out.")]
        public float waveDelay = 0.4f;
        [Tooltip("Seconds between successive waves while the scream animation plays. Each wave also " +
                 "deals damage if the player is inside the radius.")]
        public float waveInterval = 1f;

        [Header("Scream SFX")]
        [Tooltip("Played (quietly) the moment the scream begins — SFX_Scremaer.")]
        public AudioClip screamSfx;
        [Tooltip("Kept low so it isn't deafening.")]
        [Range(0f, 1f)] public float screamVolume = 0.35f;

        [Header("Camera")]
        [Tooltip("Shake amplitude requested when the wave lands.")]
        public float cameraShakeAmplitude = 0.45f;
    }
}
