using UnityEngine;
using UnityEngine.AI;

namespace Biofall.Gameplay
{
    /// <summary>
    /// Hybrid chase movement. Default: steer straight at the target on the XZ plane. If a throttled
    /// line-of-sight check finds an obstacle blocking the way, fall back to a NavMesh path. Uses the
    /// stateless <see cref="NavMesh.CalculatePath"/> into a reusable <see cref="NavMeshPath"/> (no
    /// NavMeshAgent), with corners cached alloc-free — so it scales to many enemies. Driven by
    /// <see cref="Enemy.Tick"/> (no Update of its own).
    /// </summary>
    public sealed class EnemyMovement : MonoBehaviour
    {
        private EnemyData _data;
        private Transform _tf;

        private NavMeshPath _path;
        private readonly Vector3[] _corners = new Vector3[16];
        private int _cornerCount;
        private int _cornerIndex;
        private bool _usePath;
        private float _blockedTimer;
        private Vector3 _knockback;

        public void Init(EnemyData data)
        {
            _data = data;
            _tf = transform;
            _path ??= new NavMeshPath();
            _usePath = false;
            _cornerCount = 0;
            _knockback = Vector3.zero;
            _blockedTimer = Random.Range(0f, data.blockedCheckInterval); // stagger checks across enemies
        }

        /// <summary>Add a stagger impulse (e.g. from being shot). Decays to zero over a few frames.</summary>
        public void AddKnockback(Vector3 impulse)
        {
            impulse.y = 0f;
            _knockback += impulse;
        }

        /// <summary>
        /// Move one tick toward <paramref name="targetPos"/>, blended with a boids
        /// <paramref name="separation"/> push (from <see cref="EnemyManager"/>). Returns true if
        /// within attack range.
        /// </summary>
        public bool Tick(Vector3 targetPos, Vector3 separation, float dt, out bool moving)
        {
            // Apply & decay knockback first so it layers on top of chase/separation.
            if (_knockback.sqrMagnitude > 0.0001f)
            {
                _tf.position += _knockback * dt;
                _knockback = Vector3.Lerp(_knockback, Vector3.zero, 10f * dt);
            }

            Vector3 pos = _tf.position;
            Vector3 flatTarget = new Vector3(targetPos.x, pos.y, targetPos.z);
            float sqrToTarget = (flatTarget - pos).sqrMagnitude;

            separation.y = 0f;

            if (sqrToTarget <= _data.attackRange * _data.attackRange)
            {
                // In range: hold position but still let neighbours nudge us apart so the horde
                // fans around the player instead of stacking on one point.
                if (separation.sqrMagnitude > 0.0001f)
                    _tf.position = pos + separation * (_data.moveSpeed * dt);
                FaceTowards(flatTarget - pos, dt);
                moving = separation.sqrMagnitude > 0.01f;
                return true;
            }

            Vector3 chase = ResolveDirection(pos, flatTarget, dt);
            Vector3 move = chase + separation * _data.separationWeight;
            move.y = 0f;
            if (move.sqrMagnitude > 1f) move.Normalize(); // cap combined speed

            if (move.sqrMagnitude > 0.0001f)
                _tf.position = pos + move * (_data.moveSpeed * dt);
            FaceTowards(chase, dt); // face the chase direction, not the separation jitter
            moving = true;
            return false;
        }

        private Vector3 ResolveDirection(Vector3 pos, Vector3 target, float dt)
        {
            _blockedTimer -= dt;
            if (_blockedTimer <= 0f)
            {
                _blockedTimer = _data.blockedCheckInterval;
                RefreshPathState(pos, target);
            }

            if (_usePath && _cornerCount > 1)
            {
                Vector3 corner = _corners[Mathf.Min(_cornerIndex, _cornerCount - 1)];
                corner.y = pos.y;
                if ((corner - pos).sqrMagnitude < 0.25f && _cornerIndex < _cornerCount - 1)
                    _cornerIndex++;

                Vector3 cornerDir = corner - pos;
                cornerDir.y = 0f;
                if (cornerDir.sqrMagnitude > 0.0001f) return cornerDir.normalized;
            }

            Vector3 straight = target - pos;
            straight.y = 0f;
            return straight.sqrMagnitude > 0.0001f ? straight.normalized : Vector3.zero;
        }

        private void RefreshPathState(Vector3 pos, Vector3 target)
        {
            Vector3 to = target - pos;
            to.y = 0f;
            float dist = to.magnitude;

            bool blocked = _data.obstacleMask.value != 0 && dist > 0.01f
                && Physics.Raycast(pos + Vector3.up * 0.5f, to / dist, dist, _data.obstacleMask, QueryTriggerInteraction.Ignore);

            if (blocked && NavMesh.CalculatePath(pos, target, NavMesh.AllAreas, _path) && _path.status != NavMeshPathStatus.PathInvalid)
            {
                _cornerCount = _path.GetCornersNonAlloc(_corners);
                _cornerIndex = _cornerCount > 1 ? 1 : 0;
                _usePath = _cornerCount > 1;
            }
            else
            {
                _usePath = false;
            }
        }

        private void FaceTowards(Vector3 dir, float dt)
        {
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f) return;
            Quaternion target = Quaternion.LookRotation(dir);
            _tf.rotation = Quaternion.RotateTowards(_tf.rotation, target, _data.turnSpeed * dt);
        }
    }
}
