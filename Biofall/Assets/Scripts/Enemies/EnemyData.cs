using UnityEngine;

namespace Biofall.Enemies
{
    /// <summary>
    /// Data-driven stats for an enemy archetype (Walker, Runner, Brute, Spitter, Bloater).
    /// New roster entries are authored as assets; the controller reads from here so there
    /// are no hardcoded gameplay numbers.
    /// </summary>
    [CreateAssetMenu(menuName = "Biofall/Enemy Data", fileName = "EnemyData")]
    public class EnemyData : ScriptableObject
    {
        [Header("Identity")]
        public string EnemyName = "Walker";

        [Header("Health")]
        public float MaxHealth = 30f;

        [Header("Movement")]
        public float MoveSpeed = 3f;
        [Tooltip("How strongly the zombie steers toward the player (1 = full).")]
        public float ChaseWeight = 1f;
        [Tooltip("How strongly it pushes off neighbors to avoid stacking.")]
        public float SeparationWeight = 0.6f;
        [Tooltip("Neighbor distance at which separation kicks in (~body diameter).")]
        public float SeparationRadius = 1.1f;
        public float TurnSpeed = 540f;

        [Header("Attack")]
        public float AttackRange = 1.4f;
        public float AttackDamage = 8f;
        public float AttackCooldown = 1f;

        [Header("Rewards")]
        public int BioSampleReward = 1;
    }
}
