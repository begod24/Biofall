using UnityEngine;
using Biofall.Core;
using Biofall.Gameplay;
using Biofall.UI;

namespace Biofall.Net
{
    [RequireComponent(typeof(CoopPlayerLife))]
    public sealed class CoopReviveInteractor : MonoBehaviour
    {
        private CoopPlayerLife _life;
        private PlayerInput _input;

        private const float HeartbeatInterval = 0.2f;

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
            if (!_life.IsAlive) { Clear(); return; }

            CoopPlayerLife target = FindNearestDowned();
            if (target == null) { Clear(); return; }

            if (!_hasTarget || target.NetworkObjectId != _targetId)
            {
                _targetId = target.NetworkObjectId;
                _hasTarget = true;
                _progress = 0f;
            }

            bool holding = _input != null && _input.InteractHeld;
            if (holding)
            {
                _heartbeat -= Time.deltaTime;
                if (_heartbeat <= 0f)
                {
                    _life.ReviveHeartbeatRpc(target.NetworkObjectId);
                    _heartbeat = HeartbeatInterval;
                }

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
                _progress = 0f;
                _heartbeat = 0f;
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
