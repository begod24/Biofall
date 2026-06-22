using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using Biofall.Core;
using Biofall.Gameplay;

namespace Biofall.Net
{
    public enum LifeState : byte
    {
        Alive,
        Downed,
        Dead
    }

    [RequireComponent(typeof(NetworkObject))]
    public sealed class CoopPlayerLife : NetworkBehaviour
    {
        [Header("Downed / revive (tunable)")]
        [Tooltip("Seconds a downed player survives before bleeding out to Dead, if not revived.")]
        [SerializeField] private float bleedOutSeconds = 30f;
        [Tooltip("HP a revived player comes back with.")]
        [SerializeField] private float revivedHealth = 50f;
        [Tooltip("How close a teammate must be to revive this body.")]
        [SerializeField] private float reviveRange = 2.2f;
        [Tooltip("Seconds a teammate must hold the revive before this body gets back up.")]
        [SerializeField] private float reviveHoldSeconds = 4f;

        public static readonly List<CoopPlayerLife> All = new(4);

        private static readonly int DownedBoolId = Animator.StringToHash("Downed");
        private static bool s_wiped;

        private const float ReviveHeartbeatTimeout = 0.5f;

        private readonly NetworkVariable<LifeState> _state = new(
            LifeState.Alive,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private readonly NetworkVariable<float> _hp01 = new(
            1f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Owner);

        public float Health01 => _hp01.Value;

        private Health _health;
        private Animator _animator;
        private float _bleedElapsed;
        private float _reviveHeartbeatServer;
        private TopDownCamera _cam;
        private Transform _spectateTarget;

        public LifeState State => _state.Value;
        public bool IsAlive => _state.Value == LifeState.Alive;
        public bool IsDowned => _state.Value == LifeState.Downed;
        public float ReviveRange => reviveRange;
        public float ReviveHoldSeconds => reviveHoldSeconds;

        public override void OnNetworkSpawn()
        {
            All.Add(this);
            _health = GetComponent<Health>();
            _animator = GetComponentInChildren<Animator>();

            _state.OnValueChanged += OnStateChanged;

            if (IsOwner && _health != null)
            {
                _health.Died += OnOwnerHealthDied;
                _health.Damaged += OnOwnerHealthChanged;
                _health.Healed += OnOwnerHealthHealed;
                PushHealth();
            }

            if (IsServer && _state.Value == LifeState.Alive)
                s_wiped = false;

            ApplyInitial(_state.Value);
        }

        public override void OnNetworkDespawn()
        {
            All.Remove(this);
            _state.OnValueChanged -= OnStateChanged;
            if (_health != null)
            {
                _health.Died -= OnOwnerHealthDied;
                _health.Damaged -= OnOwnerHealthChanged;
                _health.Healed -= OnOwnerHealthHealed;
            }
            PlayerRegistry.SetDowned(transform, false);

            if (IsServer) CheckTeamWipe();
        }

        private void OnOwnerHealthDied()
        {
            PushHealth();
            if (_state.Value == LifeState.Alive)
                RequestDownRpc();
        }

        private void OnOwnerHealthChanged(DamageInfo _, float __) => PushHealth();
        private void OnOwnerHealthHealed(float __, float ___) => PushHealth();

        private void PushHealth()
        {
            if (!IsOwner || _health == null) return;
            _hp01.Value = _health.Max > 0f ? Mathf.Clamp01(_health.Current / _health.Max) : 0f;
        }

        [Rpc(SendTo.Server)]
        private void RequestDownRpc()
        {
            if (_state.Value != LifeState.Alive) return;
            SetStateServer(LifeState.Downed);
        }

        [Rpc(SendTo.Server)]
        public void CompleteReviveRpc(ulong targetNetworkObjectId)
        {
            if (_state.Value != LifeState.Alive) return;
            if (!NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(targetNetworkObjectId, out var no))
                return;
            var target = no.GetComponent<CoopPlayerLife>();
            if (target == null || target._state.Value != LifeState.Downed) return;

            float maxSqr = (reviveRange + 1.5f) * (reviveRange + 1.5f);
            if ((target.transform.position - transform.position).sqrMagnitude > maxSqr) return;

            target.SetStateServer(LifeState.Alive);
        }

        [Rpc(SendTo.Server)]
        public void ReviveHeartbeatRpc(ulong targetNetworkObjectId)
        {
            if (_state.Value != LifeState.Alive) return;
            if (!NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(targetNetworkObjectId, out var no))
                return;
            var target = no.GetComponent<CoopPlayerLife>();
            if (target != null && target._state.Value == LifeState.Downed)
                target._reviveHeartbeatServer = Time.time;
        }

        private void SetStateServer(LifeState s)
        {
            if (!IsServer) return;
            _state.Value = s;

            if (s == LifeState.Downed)
            {
                _bleedElapsed = 0f;
                _reviveHeartbeatServer = -10f;
                CheckTeamWipe();
            }
            else if (s == LifeState.Dead)
            {
                CheckTeamWipe();
            }
            else
            {
                s_wiped = false;
            }
        }

        private void Update()
        {
            if (IsServer) ServerBleedTick();
            if (IsOwner && _state.Value == LifeState.Dead) UpdateSpectator();
        }

        private void ServerBleedTick()
        {
            if (_state.Value != LifeState.Downed) return;
            bool beingRevived = Time.time - _reviveHeartbeatServer < ReviveHeartbeatTimeout;
            if (!beingRevived) _bleedElapsed += Time.deltaTime;
            if (_bleedElapsed >= bleedOutSeconds) SetStateServer(LifeState.Dead);
        }

        private void CheckTeamWipe()
        {
            if (!IsServer || s_wiped || All.Count == 0) return;
            for (int i = 0; i < All.Count; i++)
                if (All[i] != null && All[i]._state.Value == LifeState.Alive)
                    return;

            s_wiped = true;
            TeamWipeRpc();
        }

        [Rpc(SendTo.Everyone)]
        private void TeamWipeRpc() => EventBus.Publish(new TeamWiped());

        private void OnStateChanged(LifeState previous, LifeState current)
        {
            PlayerRegistry.SetDowned(transform, current != LifeState.Alive);
            ApplyPose(current);

            if (!IsOwner && previous == LifeState.Alive && current == LifeState.Downed)
                EventBus.Publish(new TeammateDowned(transform));

            if (!IsOwner) return;

            switch (current)
            {
                case LifeState.Downed: GoDownOwner(); break;
                case LifeState.Alive:  ReviveOwner();  break;
                case LifeState.Dead:   EliminateOwner(); break;
            }
        }

        private void ApplyInitial(LifeState s)
        {
            bool down = s != LifeState.Alive;
            PlayerRegistry.SetDowned(transform, down);
            ApplyPose(s);
            if (IsOwner && down)
                GetComponent<CoopPlayer>()?.SetControllable(false);
        }

        private void ApplyPose(LifeState s)
        {
            if (_animator != null) _animator.SetBool(DownedBoolId, s != LifeState.Alive);
        }

        private void GoDownOwner()
        {
            GetComponent<CoopPlayer>()?.SetControllable(false);
            PlayerRegistry.SetLocal(transform);
            PlayerRegistry.SetDowned(transform, true);
            EventBus.Publish(new PlayerDowned(bleedOutSeconds));
        }

        private void ReviveOwner()
        {
            _health?.Revive(revivedHealth);
            GetComponent<CoopPlayer>()?.SetControllable(true);
            PlayerRegistry.SetLocal(transform);
            PlayerRegistry.SetDowned(transform, false);
            EventBus.Publish(new PlayerRevived());
        }

        private void EliminateOwner()
        {
            GetComponent<CoopPlayer>()?.SetControllable(false);
            BeginSpectate();
            EventBus.Publish(new PlayerEliminated());
        }

        private void BeginSpectate()
        {
            if (_cam == null) _cam = FindFirstObjectByType<TopDownCamera>();
            _spectateTarget = PlayerRegistry.NearestAlive(transform.position);
            if (_cam != null && _spectateTarget != null) _cam.SetTarget(_spectateTarget);
        }

        private void UpdateSpectator()
        {
            if (_spectateTarget != null && !PlayerRegistry.IsDowned(_spectateTarget)) return;
            BeginSpectate();
        }
    }
}
