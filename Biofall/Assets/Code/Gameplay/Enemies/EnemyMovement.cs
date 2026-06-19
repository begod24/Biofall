using UnityEngine;
using UnityEngine.AI;

namespace Biofall.Gameplay
{
    /// <summary>
    /// NavMesh-driven movement WITHOUT a NavMeshAgent (so one central <see cref="EnemyManager"/> tick
    /// scales to 100–150 enemies). Two states:
    ///   • Wander — pick a random reachable point on the NavMesh and walk there, then pause and repeat.
    ///   • Chase  — once aggroed (handled by <see cref="Enemy"/>), path to the player, repathed on a
    ///     throttled, staggered timer.
    /// The enemy always walks along NavMesh path corners (which lie on the mesh), and every step the
    /// resulting position is snapped back onto the NavMesh — so it physically can't enter a wall and
    /// no wall colliders are needed. Driven by <see cref="Enemy.Tick"/> (no Update of its own).
    /// </summary>
    public sealed class EnemyMovement : MonoBehaviour
    {
        private EnemyData _data;
        private Transform _tf;

        private NavMeshPath _path;
        private readonly Vector3[] _corners = new Vector3[24];
        private int _cornerCount;
        private int _cornerIndex;

        private float _repathTimer;
        private Vector3 _wanderTarget;
        private bool _hasWanderTarget;
        private float _wanderPauseTimer;

        private Vector3 _knockback;

        private const float CornerReached = 0.6f;   // distance to advance to the next corner
        private const float DestReached = 1.2f;      // distance that counts as "arrived"
        private const float NavClamp = 2f;            // how far we sample to snap back onto the NavMesh

        public void Init(EnemyData data)
        {
            _data = data;
            _tf = transform;
            _path ??= new NavMeshPath();
            _cornerCount = 0;
            _cornerIndex = 0;
            _hasWanderTarget = false;
            _knockback = Vector3.zero;
            // stagger so enemies don't all repath / pick targets on the same frame
            _repathTimer = Random.Range(0f, data.repathInterval);
            _wanderPauseTimer = Random.Range(0f, data.wanderPauseMax);
        }

        /// <summary>Add a stagger impulse (e.g. from being shot). Decays to zero over a few frames.</summary>
        public void AddKnockback(Vector3 impulse)
        {
            impulse.y = 0f;
            _knockback += impulse;
        }

        /// <summary>
        /// Move one tick. <paramref name="chasing"/> picks the state: chase <paramref name="playerPos"/>
        /// or wander on its own. <paramref name="separation"/> is the boids push from
        /// <see cref="EnemyManager"/>. Returns true when chasing AND within attack range.
        /// </summary>
        public bool Tick(bool chasing, Vector3 playerPos, Vector3 separation, float dt, out bool moving)
        {
            Vector3 pos = _tf.position;
            separation.y = 0f;

            // Knockback first, layered on top of everything, then clamped to the mesh below.
            if (_knockback.sqrMagnitude > 0.0001f)
            {
                pos += _knockback * dt;
                _knockback = Vector3.Lerp(_knockback, Vector3.zero, 10f * dt);
            }

            if (chasing)
            {
                // In attack range: stop, let neighbours fan us out, face the player.
                Vector3 toPlayer = playerPos - pos; toPlayer.y = 0f;
                if (toPlayer.sqrMagnitude <= _data.attackRange * _data.attackRange)
                {
                    if (separation.sqrMagnitude > 0.0001f)
                        pos = ClampToNavMesh(pos + separation * (_data.moveSpeed * dt), pos);
                    _tf.position = pos;
                    FaceTowards(toPlayer, dt);
                    moving = separation.sqrMagnitude > 0.01f;
                    return true;
                }

                _repathTimer -= dt;
                if (_repathTimer <= 0f)
                {
                    _repathTimer = _data.repathInterval;
                    SetPath(pos, playerPos);
                }
            }
            else
            {
                // Wander: pick a new reachable point after the idle pause, repath on arrival.
                if (!_hasWanderTarget)
                {
                    _wanderPauseTimer -= dt;
                    if (_wanderPauseTimer <= 0f && PickWanderTarget(pos))
                    {
                        SetPath(pos, _wanderTarget);
                        _hasWanderTarget = _cornerCount > 0;
                        if (!_hasWanderTarget) _wanderPauseTimer = 0.3f; // bad point, retry soon
                    }
                }
                else if (ReachedDestination(pos))
                {
                    _hasWanderTarget = false;
                    _wanderPauseTimer = Random.Range(_data.wanderPauseMin, _data.wanderPauseMax);
                    _cornerCount = 0;
                }
            }

            // Follow the current path, blend in separation, snap to the NavMesh.
            Vector3 dir = FollowPath(pos);
            Vector3 move = dir + separation * _data.separationWeight;
            move.y = 0f;
            if (move.sqrMagnitude > 1f) move.Normalize();

            if (move.sqrMagnitude > 0.0001f)
            {
                pos = ClampToNavMesh(pos + move * (_data.moveSpeed * dt), pos);
                moving = true;
            }
            else moving = _knockback.sqrMagnitude > 0.01f;

            _tf.position = pos;
            FaceTowards(dir, dt);
            return false;
        }

        // ---- path helpers ----

        private void SetPath(Vector3 from, Vector3 to)
        {
            if (NavMesh.CalculatePath(from, to, NavMesh.AllAreas, _path) &&
                _path.status != NavMeshPathStatus.PathInvalid)
            {
                _cornerCount = _path.GetCornersNonAlloc(_corners);
                _cornerIndex = _cornerCount > 1 ? 1 : 0; // 0 is our own position
            }
            else
            {
                _cornerCount = 0;
            }
        }

        /// <summary>Direction toward the current path corner (XZ), advancing as we reach each one.</summary>
        private Vector3 FollowPath(Vector3 pos)
        {
            if (_cornerCount < 2) return Vector3.zero;

            Vector3 corner = _corners[Mathf.Min(_cornerIndex, _cornerCount - 1)];
            Vector3 flat = new Vector3(corner.x, pos.y, corner.z);
            if ((flat - pos).sqrMagnitude < CornerReached * CornerReached && _cornerIndex < _cornerCount - 1)
            {
                _cornerIndex++;
                corner = _corners[_cornerIndex];
                flat = new Vector3(corner.x, pos.y, corner.z);
            }

            Vector3 dir = flat - pos; dir.y = 0f;
            return dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector3.zero;
        }

        private bool ReachedDestination(Vector3 pos)
        {
            if (_cornerCount == 0) return true;
            Vector3 last = _corners[_cornerCount - 1];
            Vector3 d = new Vector3(last.x - pos.x, 0f, last.z - pos.z);
            return d.sqrMagnitude <= DestReached * DestReached;
        }

        private bool PickWanderTarget(Vector3 pos)
        {
            Vector2 r = Random.insideUnitCircle * _data.wanderRadius;
            Vector3 candidate = pos + new Vector3(r.x, 0f, r.y);
            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, _data.wanderRadius, NavMesh.AllAreas))
            {
                _wanderTarget = hit.position;
                return true;
            }
            return false;
        }

        /// <summary>Snap a desired position back onto the NavMesh; falls back to the old position.</summary>
        private Vector3 ClampToNavMesh(Vector3 desired, Vector3 fallback)
        {
            if (NavMesh.SamplePosition(desired, out NavMeshHit hit, NavClamp, NavMesh.AllAreas))
                return hit.position;
            return fallback;
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
