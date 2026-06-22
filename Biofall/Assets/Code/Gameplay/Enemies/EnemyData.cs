using UnityEngine;

namespace Biofall.Gameplay
{
    [CreateAssetMenu(menuName = "Biofall/Enemy Data", fileName = "EN_Enemy")]
    public class EnemyData : ScriptableObject
    {
        [Header("Health")]
        public float maxHealth = 50f;

        [Header("Movement")]
        public float moveSpeed = 2.2f;
        [Tooltip("Turn rate toward travel direction (deg/sec).")]
        public float turnSpeed = 540f;
        [Tooltip("Layers treated as obstacles for the hybrid NavMesh fallback.")]
        public LayerMask obstacleMask;
        [Tooltip("How often the straight-line-blocked check runs (seconds).")]
        public float blockedCheckInterval = 0.25f;

        [Header("Separation (boids — keeps the horde from stacking)")]
        [Tooltip("Neighbours closer than this push this zombie away.")]
        public float separationRadius = 1.4f;
        [Tooltip("How strongly neighbours push (relative to moveSpeed).")]
        public float separationWeight = 1.6f;

        [Header("AI — wander & aggro")]
        [Tooltip("Player within this distance (m) makes the enemy start chasing — permanently. " +
                 "Also the radius an already-chasing enemy uses to drag idle neighbours into the chase.")]
        public float aggroRadius = 10f;
        [Tooltip("Radius (m) for picking random wander destinations on the NavMesh while idle.")]
        public float wanderRadius = 12f;
        [Tooltip("Idle pause between two wander destinations (seconds, randomised).")]
        public float wanderPauseMin = 1f;
        public float wanderPauseMax = 3.5f;
        [Tooltip("How often a chasing enemy recomputes its NavMesh path to the player (seconds).")]
        public float repathInterval = 0.4f;

        [Header("Attack")]
        [Tooltip("Within this distance the zombie stops and attacks.")]
        public float attackRange = 1.9f;
        public float attackDamage = 10f;
        public float attackCooldown = 1.2f;
        [Tooltip("Animator trigger fired to start the attack. Zombie melee = \"Attack\"; Screamer = \"Scream\".")]
        public string attackTrigger = "Attack";

        [Header("SFX")]
        [Tooltip("Random groan clips — one is picked at random each time.")]
        public AudioClip[] groanSfx;
        public AudioClip deathSfx;
        [Tooltip("Probability per second that an idle/chasing zombie groans.")]
        [Range(0f, 2f)] public float groanChancePerSecond = 0.4f;
        [Tooltip("Volume of groan clips (kept low so a horde isn't deafening).")]
        [Range(0f, 1f)] public float groanVolume = 0.3f;
        [Range(0f, 1f)] public float deathVolume = 0.55f;
        [Tooltip("Minimum seconds between ANY two zombie groans globally (voice throttle).")]
        public float globalGroanInterval = 0.18f;

        [Header("Death")]
        [Tooltip("Seconds the death animation plays before the body returns to the pool.")]
        public float despawnDelay = 2.5f;

        [Header("Hit reaction")]
        [Tooltip("Pooled blood splatter spawned at the hit point.")]
        public GameObject bloodPrefab;
        [Tooltip("Stagger impulse when hit (small — they shuffle, not fly).")]
        public float knockbackForce = 2.5f;
        public Color hitFlashColor = new Color(1f, 0.4f, 0.4f, 1f);
        public float hitFlashDuration = 0.06f;

    }
}
