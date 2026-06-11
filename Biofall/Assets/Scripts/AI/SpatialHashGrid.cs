using System.Collections.Generic;
using UnityEngine;

namespace Biofall.AI
{
    /// <summary>
    /// O(n) uniform spatial hash over the XZ plane used for cheap zombie-vs-zombie
    /// separation. Rebuilt each frame by <see cref="Enemies.EnemyManager"/>. Buckets store
    /// indices into the manager's parallel position array, so no per-zombie allocations.
    /// </summary>
    public sealed class SpatialHashGrid
    {
        private readonly float _cellSize;
        private readonly float _invCellSize;
        private readonly Dictionary<long, List<int>> _cells = new();
        // Reuse emptied lists instead of allocating new ones each frame.
        private readonly Stack<List<int>> _listPool = new();

        public SpatialHashGrid(float cellSize)
        {
            _cellSize = Mathf.Max(0.1f, cellSize);
            _invCellSize = 1f / _cellSize;
        }

        public void Clear()
        {
            foreach (var kvp in _cells)
            {
                kvp.Value.Clear();
                _listPool.Push(kvp.Value);
            }
            _cells.Clear();
        }

        public void Insert(int index, Vector3 position)
        {
            long key = KeyOf(position);
            if (!_cells.TryGetValue(key, out var list))
            {
                list = _listPool.Count > 0 ? _listPool.Pop() : new List<int>(8);
                _cells.Add(key, list);
            }
            list.Add(index);
        }

        /// <summary>
        /// Sum normalized push-away vectors from neighbors within <paramref name="radius"/>,
        /// scanning only the 3x3 cells around <paramref name="self"/>.
        /// </summary>
        public Vector3 ComputeSeparation(int self, IReadOnlyList<Vector3> positions, float radius)
        {
            Vector3 selfPos = positions[self];
            float radiusSqr = radius * radius;
            Vector3 push = Vector3.zero;

            int cx = Mathf.FloorToInt(selfPos.x * _invCellSize);
            int cz = Mathf.FloorToInt(selfPos.z * _invCellSize);

            for (int ox = -1; ox <= 1; ox++)
            for (int oz = -1; oz <= 1; oz++)
            {
                long key = KeyOf(cx + ox, cz + oz);
                if (!_cells.TryGetValue(key, out var list)) continue;

                for (int i = 0; i < list.Count; i++)
                {
                    int other = list[i];
                    if (other == self) continue;

                    Vector3 diff = selfPos - positions[other];
                    diff.y = 0f;
                    float dSqr = diff.sqrMagnitude;
                    if (dSqr > radiusSqr || dSqr < 1e-6f) continue;

                    float d = Mathf.Sqrt(dSqr);
                    // Closer neighbors push harder (weight by how far inside the radius they are).
                    push += diff / d * (1f - d / radius);
                }
            }

            return push;
        }

        private long KeyOf(Vector3 p) => KeyOf(Mathf.FloorToInt(p.x * _invCellSize), Mathf.FloorToInt(p.z * _invCellSize));

        private static long KeyOf(int x, int z) => ((long)x << 32) ^ (uint)z;
    }
}
