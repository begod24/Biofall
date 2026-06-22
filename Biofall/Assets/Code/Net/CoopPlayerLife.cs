using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using Biofall.Core;
using Biofall.Gameplay;

namespace Biofall.Net
{
    /// <summary>Networked life state of a co-op player, visible to every peer.</summary>
    public enum LifeState : byte
    {
        Alive,   // up and fighting
        Downed,  // HP hit 0 — incapacitated, bleeding out, can be revived by a teammate
        Dead     // bled out (or no rescuer) — out until the squad extracts or wipes
    }

    /// <summary>
    /// Phase E — co-op DOWNED / REVIVE. Lives on the co-op player prefab (solo never has it, and
    /// everything is gated on a live session so the single-player death path is untouched).
    ///
    /// Player HP is owner-authoritative (each client owns its own <see cref="Health"/>), so this
    /// component bridges that to a server-authoritative <b>life state</b> everyone can see:
    ///   • The owner watches its local <see cref="Health.Died"/>. In co-op that means DOWNED, not
    ///     Game Over — it asks the server (<see cref="RequestDownRpc"/>) to flip the shared state.
    ///   • The server owns the <see cref="LifeState"/> NetworkVariable + the bleed-out timer, and
    ///     validates revives (<see cref="CompleteReviveRpc"/>, sent by the reviver's own life).
    ///   • Every peer mirrors the state into <see cref="PlayerRegistry"/> (downed bodies are excluded
    ///     from "is the squad here / alive" checks) and the owner reacts locally (freeze on down,
    ///     heal + unfreeze on revive) and raises the HUD events.
    /// When nobody is left up, the server calls a team wipe.
    /// </summary>
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

        /// <summary>Every spawned co-op life on THIS machine (own + remote replicas). Reviver scanning
        /// reads this to find downed teammates locally.</summary>
        public static readonly List<CoopPlayerLife> All = new(4);

        private static readonly int DownedBoolId = Animator.StringToHash("Downed");
        private static bool s_wiped; // server: fire the team-wipe once per run

        private const float ReviveHeartbeatTimeout = 0.5f; // no heartbeat for this long → rescue lapsed

        private readonly NetworkVariable<LifeState> _state = new(
            LifeState.Alive,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        // Player HP is owner-authoritative & local, so other peers can't see it directly. The owner
        // mirrors its 0..1 health fraction here so the squad HUD can show every teammate's HP.
        private readonly NetworkVariable<float> _hp01 = new(
            1f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Owner);

        /// <summary>This player's HP as a 0..1 fraction, visible to every peer (squad HUD).</summary>
        public float Health01 => _hp01.Value;

        private Health _health;
        private Animator _animator;
        private float _bleedElapsed;          // server: accrued bleed time (pauses while being revived)
        private float _reviveHeartbeatServer; // server: last time a reviver reported holding this body
        private TopDownCamera _cam;           // owner: cached camera for spectating when dead
        private Transform _spectateTarget;    // owner: teammate we're currently watching

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
                PushHealth(); // seed the replicated fraction
            }

            if (IsServer && _state.Value == LifeState.Alive)
                s_wiped = false; // fresh body up → a new run can wipe again

            // Late-join / re-sync: mirror the already-replicated state without firing transitions.
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

            // A leaver might have been the last one standing — re-evaluate the squad.
            if (IsServer) CheckTeamWipe();
        }

        // ---- owner: local HP death → ask the server to put us DOWN ----

        private void OnOwnerHealthDied()
        {
            PushHealth();
            if (_state.Value == LifeState.Alive)
                RequestDownRpc();
        }

        // Owner mirrors its local HP fraction into the replicated variable for the squad HUD.
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

        // ---- reviver (its OWN life) → server completes a revive on the target ----

        /// <summary>Called by the reviver on their own (owned) life once the hold finishes. The server
        /// re-validates everything before bringing the target back up.</summary>
        [Rpc(SendTo.Server)]
        public void CompleteReviveRpc(ulong targetNetworkObjectId)
        {
            if (_state.Value != LifeState.Alive) return; // a downed/dead player can't revive
            if (!NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(targetNetworkObjectId, out var no))
                return;
            var target = no.GetComponent<CoopPlayerLife>();
            if (target == null || target._state.Value != LifeState.Downed) return;

            float maxSqr = (reviveRange + 1.5f) * (reviveRange + 1.5f); // small server-side slack
            if ((target.transform.position - transform.position).sqrMagnitude > maxSqr) return;

            target.SetStateServer(LifeState.Alive);
        }

        /// <summary>Reviver heartbeat (sent on their OWN owned life while holding E) → pauses the
        /// target's bleed-out so a rescue that started in time can't be lost to the timer.</summary>
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

        // ---- server state authority ----

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
            else // Alive (revived) → the run can wipe again
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
            // Pause the bleed-out while a teammate is actively reviving (heartbeat), so a rescue
            // started in time always finishes.
            bool beingRevived = Time.time - _reviveHeartbeatServer < ReviveHeartbeatTimeout;
            if (!beingRevived) _bleedElapsed += Time.deltaTime;
            if (_bleedElapsed >= bleedOutSeconds) SetStateServer(LifeState.Dead);
        }

        private void CheckTeamWipe()
        {
            if (!IsServer || s_wiped || All.Count == 0) return;
            for (int i = 0; i < All.Count; i++)
                if (All[i] != null && All[i]._state.Value == LifeState.Alive)
                    return; // someone is still up

            s_wiped = true;
            TeamWipeRpc();
        }

        [Rpc(SendTo.Everyone)]
        private void TeamWipeRpc() => EventBus.Publish(new TeamWiped());

        // ---- presentation (all peers + owner reactions) ----

        private void OnStateChanged(LifeState previous, LifeState current)
        {
            PlayerRegistry.SetDowned(transform, current != LifeState.Alive);
            ApplyPose(current); // every peer shows the knocked-down / standing pose

            // A TEAMMATE going down (seen on every peer) drives the on-screen marker + "man down" SFX.
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

        // Drives the animator on EVERY peer: Downed=true → "Knocked Down" pose; false → "Getting Up"
        // → Locomotion (transitions live in the controller). Replaces the old Die-trigger/Rebind hack.
        private void ApplyPose(LifeState s)
        {
            if (_animator != null) _animator.SetBool(DownedBoolId, s != LifeState.Alive);
        }

        private void GoDownOwner()
        {
            GetComponent<CoopPlayer>()?.SetControllable(false); // disabling PlayerController unregisters us…
            PlayerRegistry.SetLocal(transform);                 // …so re-add the body and keep it local
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
            // Stay frozen (already disabled from the downed state); the squad plays on without us.
            GetComponent<CoopPlayer>()?.SetControllable(false);
            BeginSpectate();
            EventBus.Publish(new PlayerEliminated());
        }

        // Dead-but-team-alive → spectate a living teammate until they extract (or the squad wipes).
        private void BeginSpectate()
        {
            if (_cam == null) _cam = FindFirstObjectByType<TopDownCamera>();
            _spectateTarget = PlayerRegistry.NearestAlive(transform.position);
            if (_cam != null && _spectateTarget != null) _cam.SetTarget(_spectateTarget);
        }

        private void UpdateSpectator()
        {
            // Keep the camera on a living teammate; re-pick if the one we watched went down.
            if (_spectateTarget != null && !PlayerRegistry.IsDowned(_spectateTarget)) return;
            BeginSpectate();
        }
    }
}
