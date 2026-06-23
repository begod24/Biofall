using UnityEngine;

namespace Biofall.Gameplay
{
    // Stationary acid-spitter ("Spitter"/зонёр). Reuses the Screamer model + animator but, instead
    // of a screaming shockwave, lobs a lingering acid pool at the player's feet that hurts over time.
    // Make it stationary by setting moveSpeed = 0 and a large attackRange (its spit range).
    [CreateAssetMenu(menuName = "Biofall/Spitter Data", fileName = "EN_Spitter")]
    public sealed class SpitterData : EnemyData
    {
        [Header("Acid spit")]
        [Tooltip("Pooled acid-pool hazard dropped where the Spitter aims. Damages players standing in it.")]
        public GameObject acidPoolPrefab;
        [Tooltip("Optional pooled splat/glob VFX shown at the spit's landing point.")]
        public GameObject spitVfxPrefab;
        [Tooltip("Radius (metres) of the acid pool that hurts the player.")]
        public float poolRadius = 2.6f;
        [Tooltip("Seconds the acid pool lingers before drying up.")]
        public float poolLifetime = 5f;
        [Tooltip("Damage applied each tick to a player standing in the acid.")]
        public float acidDamagePerTick = 4f;
        [Tooltip("Seconds between damage ticks while inside the acid.")]
        public float acidTickInterval = 0.5f;
        [Tooltip("Delay from the attack animation starting to the acid landing (telegraph / wind-up).")]
        public float spitWindup = 0.5f;
        [Tooltip("Metres ahead of the target's facing the spit leads (0 = lands right on the player).")]
        public float aimLead = 0f;

        [Header("Spit projectile (visible glob)")]
        [Tooltip("Pooled acid glob lobbed at the player; it bursts into the acid pool on impact. " +
                 "If null, the pool is dropped straight at the target instead.")]
        public GameObject spitProjectilePrefab;
        [Tooltip("Seconds the glob takes to reach the target.")]
        public float spitFlightTime = 0.8f;
        [Tooltip("Peak height (metres) of the lob arc.")]
        public float spitArcHeight = 2.5f;
        [Tooltip("Height above the Spitter's feet the glob launches from (mouth height).")]
        public float spitOriginHeight = 1.7f;

        [Header("Spit SFX")]
        public AudioClip spitSfx;
        [Range(0f, 1f)] public float spitVolume = 0.7f;
    }
}
