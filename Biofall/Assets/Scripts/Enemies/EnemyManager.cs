using System.Collections.Generic;
using Biofall.AI;
using Biofall.Combat;
using Biofall.Utilities;
using UnityEngine;

namespace Biofall.Enemies
{
    /// <summary>
    /// Owns the whole zombie swarm and ticks it in ONE batched loop instead of letting
    /// hundreds of MonoBehaviours each run Update(). Each frame it rebuilds a
    /// <see cref="SpatialHashGrid"/> and feeds every zombie a cheap separation vector so
    /// the crowd reads as a mass without O(n^2) cost. This is the system that makes
    /// 100+ on-screen enemies viable.
    /// </summary>
    public class EnemyManager : MonoSingleton<EnemyManager>
    {
        [Header("Target")]
        [Tooltip("If left empty, the player is found by tag at startup.")]
        [SerializeField] private Transform _target;
        [SerializeField] private string _playerTag = "Player";

        [Header("Separation grid")]
        [Tooltip("Cell size for the spatial hash. ~= separation radius of a typical zombie.")]
        [SerializeField] private float _cellSize = 1.2f;
        [SerializeField] private float _separationRadius = 1.1f;

        private readonly List<EnemyController> _active = new(256);
        private readonly List<Vector3> _positions = new(256);
        private SpatialHashGrid _grid;

        public Transform Target => _target;
        public IDamageable TargetDamageable { get; private set; }
        public int ActiveCount => _active.Count;

        protected override void OnSingletonAwake()
        {
            _grid = new SpatialHashGrid(_cellSize);
            ResolveTarget();
        }

        private void ResolveTarget()
        {
            if (_target == null)
            {
                var player = GameObject.FindGameObjectWithTag(_playerTag);
                if (player != null) _target = player.transform;
            }
            if (_target != null) TargetDamageable = _target.GetComponentInChildren<IDamageable>();
        }

        public void SetTarget(Transform target)
        {
            _target = target;
            TargetDamageable = target != null ? target.GetComponentInChildren<IDamageable>() : null;
        }

        public void Register(EnemyController enemy)
        {
            if (enemy != null && !_active.Contains(enemy)) _active.Add(enemy);
        }

        public void Unregister(EnemyController enemy)
        {
            // Swap-remove: order doesn't matter, avoids shifting the whole list.
            int i = _active.IndexOf(enemy);
            if (i < 0) return;
            int last = _active.Count - 1;
            _active[i] = _active[last];
            _active.RemoveAt(last);
        }

        private void Update()
        {
            int count = _active.Count;
            if (count == 0) return;

            float dt = Time.deltaTime;

            // 1) Snapshot positions and (re)build the grid.
            _positions.Clear();
            _grid.Clear();
            for (int i = 0; i < count; i++)
            {
                Vector3 p = _active[i].Position;
                _positions.Add(p);
                _grid.Insert(i, p);
            }

            // 2) Tick each zombie with its separation vector.
            for (int i = 0; i < count; i++)
            {
                Vector3 separation = _grid.ComputeSeparation(i, _positions, _separationRadius);
                _active[i].Tick(dt, separation);
            }
        }
    }
}
