using UnityEngine;

namespace Biofall.Gameplay
{
    /// <summary>
    /// One possible drop. <see cref="onlyFor"/> = null means it can drop from ANY enemy (shared loot
    /// like ammo/medkits/bio samples — edited in one place); set it to a specific <see cref="EnemyData"/>
    /// to make this drop type-specific (e.g. grenades with a different chance per enemy archetype).
    /// </summary>
    [System.Serializable]
    public class LootEntry
    {
        public string label = "drop";
        public GameObject prefab;
        [Range(0f, 1f)] public float chance = 0.3f;
        [Tooltip("How many to spawn when the roll succeeds (inclusive range).")]
        public int minCount = 1;
        public int maxCount = 1;
        [Tooltip("Leave empty = drops from every enemy. Set = only this enemy type rolls this entry.")]
        public EnemyData onlyFor;
    }

    /// <summary>
    /// The single source of truth for ALL enemy drops. One asset (e.g. LT_Campaign) holds every
    /// drop entry; the <see cref="LootService"/> rolls the matching ones whenever a target dies.
    /// Designed so an entire campaign's loot/balance lives here — no per-enemy prefab wiring.
    /// </summary>
    [CreateAssetMenu(menuName = "Biofall/Loot Config", fileName = "LT_Campaign")]
    public sealed class LootConfig : ScriptableObject
    {
        public LootEntry[] entries;
    }
}
