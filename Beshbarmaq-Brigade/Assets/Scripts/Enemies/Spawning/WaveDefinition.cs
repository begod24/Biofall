using UnityEngine;
using System.Collections.Generic;
using Biofall.Enemies;

namespace Biofall.Spawning
{
    [System.Serializable]
    public class WaveEntry
    {
        public EnemyBase enemyPrefab;
        public int count = 5;
        public float spawnInterval = 0.5f;
    }

    [CreateAssetMenu(fileName = "WaveDefinition", menuName = "Biofall/Wave Definition")]
    public class WaveDefinition : ScriptableObject
    {
        public string waveName = "Wave";
        public float startDelay = 2f;
        public List<WaveEntry> entries = new();
    }
}
