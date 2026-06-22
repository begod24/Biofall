using UnityEngine;
using Biofall.Core;
using Biofall.Gameplay;
using Biofall.UI;

namespace Biofall.Net
{
    /// <summary>
    /// Phase E — the reviver side. Sits on the co-op player and only does anything for the local
    /// OWNER while it is alive. Each frame it looks for the nearest DOWNED teammate in range and, if
    /// the player holds E (<see cref="PlayerInput.InteractHeld"/>), fills a revive timer. When the
    /// hold completes it asks the server (via its own <see cref="CoopPlayerLife.CompleteReviveRpc"/>)
    /// to bring that teammate back up. It only feeds the HUD through <see cref="ReviveProgress"/> —
    /// the actual state change is server-authoritative. Solo never runs (gated on the session).
    /// </summary>
    [RequireComponent(typeof(CoopPlayerLife))]
    public sealed class CoopReviveInteractor : MonoBehaviour
    {
        private CoopPlayerLife _life;
        private PlayerInput _input;

        private const float HeartbeatInterval = 0.2f; // how often we tell the server "still reviving"

        private ulong _targetId;
        private bool _hasTarget;
        private float _progress;
        private float _heartbeat;
        private bool _showing;

        private void Awake()
        {
            _life = GetComponent<CoopPlayerLife>();
            _input = GetComponent<PlayerInput>();
        }

        private void Update()
        {
            if (!NetSession.InCoop || _life == null || !_life.IsOwner) { Clear(); return; }
            if (UiOverlay.Active || Time.timeScale <= 0f) { Clear(); return; }
            if (!_life.IsAlive) { Clear(); return; } // can't revive while you're down yourself

            CoopPlayerLife target = FindNearestDowned();
            if (target == null) { Clear(); return; }

            // Reset progress if we switched to a different downed teammate.
            if (!_hasTarget || target.NetworkObjectId != _targetId)
            {
                _targetId = target.NetworkObjectId;
                _hasTarget = true;
                _progress = 0f;
            }

            bool holding = _input != null && _input.InteractHeld;
            if (holding)
            {
                // Tell the server we're on it so the target's bleed-out pauses mid-rescue.
                _heartbeat -= Time.deltaTime;
                if (_heartbeat <= 0f)
                {
                    _life.ReviveHeartbeatRpc(target.NetworkObjectId);
                    _heartbeat = HeartbeatInterval;
                }

                // The reviver's own "Field Medic" upgrade shortens the hold (persistent progression).
                float hold = _life.ReviveHoldSeconds * PlayerProgression.ReviveHoldMultiplier;
                _progress += Time.deltaTime / Mathf.Max(0.1f, hold);
                if (_progress >= 1f)
                {
                    _life.CompleteReviveRpc(target.NetworkObjectId);
                    Clear();
                    return;
                }
            }
            else
            {
                _progress = 0f;   // released → start over (no partial credit)
                _heartbeat = 0f;  // resume bleed promptly if they stop reviving
            }

            Publish(true, _progress);
        }

        private CoopPlayerLife FindNearestDowned()
        {
            CoopPlayerLife best = null;
            float bestSqr = float.MaxValue;
            Vector3 here = transform.position;

            var all = CoopPlayerLife.All;
            for (int i = 0; i < all.Count; i++)
            {
                CoopPlayerLife life = all[i];
                if (life == null || life == _life || !life.IsDowned) continue;

                float range = life.ReviveRange;
                float sqr = (life.transform.position - here).sqrMagnitude;
                if (sqr <= range * range && sqr < bestSqr) { bestSqr = sqr; best = life; }
            }
            return best;
        }

        private void Publish(bool show, float progress)
        {
            _showing = true;
            EventBus.Publish(new ReviveProgress(show, progress));
        }

        private void Clear()
        {
            _hasTarget = false;
            _progress = 0f;
            if (_showing)
            {
                _showing = false;
                EventBus.Publish(new ReviveProgress(false, 0f));
            }
        }
    }
}
