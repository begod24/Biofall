using UnityEngine;

namespace Biofall.Gameplay
{
    [System.Serializable]
    public class LootEntry
    {
        public string label = "drop";
        public GameObject prefab;
        [Tooltip("Networked variant (NetworkObject + CoopPickup) spawned in co-op instead of 'prefab'. " +
                 "Same chances/counts as solo. Leave empty to skip this drop in co-op.")]
        public GameObject coopPrefab;
        [Range(0f, 1f)] public float chance = 0.3f;
        [Tooltip("How many to spawn when the roll succeeds (inclusive range).")]
        public int minCount = 1;
        public int maxCount = 1;
        [Tooltip("Leave empty = drops from every enemy. Set = only this enemy type rolls this entry.")]
        public EnemyData onlyFor;
        [Tooltip("Mark Bio Sample drops. A LootService with 'Drop Bio Samples' off skips these " +
                 "(used in WaveMode — pure arcade, no currency farming).")]
        public bool isBioSample;
    }

    [CreateAssetMenu(menuName = "Biofall/Loot Config", fileName = "LT_Campaign")]
    public sealed class LootConfig : ScriptableObject
    {
        public LootEntry[] entries;
    }
}
