using Unity.Netcode;
using UnityEngine;
using Biofall.Core;
using Biofall.Gameplay;

namespace Biofall.Net
{
    /// <summary>
    /// Host-authoritative bridge for a networked zombie (lives only on the <c>Enemy_Coop</c> prefab;
    /// solo uses the plain <c>Enemy</c> prefab and never has this). NGO spawning does NOT fire the
    /// pooling <c>IPoolable.OnSpawned</c>, so this drives the lifecycle explicitly:
    ///   • SERVER → runs the full <see cref="Enemy"/> brain (HP, movement, central tick, animation),
    ///     owns the authoritative <see cref="Health"/>, and NGO-despawns the corpse. Damage from any
    ///     player arrives via <see cref="DamageRpc"/>; the resulting blood/flash/death is mirrored to
    ///     remote clients with <see cref="ReactClientRpc"/>.
    ///   • CLIENT → a puppet: the server-authoritative <see cref="NetworkTransform"/> moves the body,
    ///     local AI/HP/despawn are off, and the Animator speed is inferred from the replicated motion.
    /// </summary>
    [RequireComponent(typeof(NetworkObject))]
    [RequireComponent(typeof(Enemy))]
    [RequireComponent(typeof(Health))]
    public sealed class CoopEnemy : NetworkBehaviour
    {
        private Enemy _enemy;
        private Health _health;
        private EnemyMovement _movement;
        private Animator _animator;
        private Transform _tf;
        private Vector3 _lastPos;

        // Replicated HP fraction (0..1) — drives the floating health bar on EVERY client, including
        // late-joiners. Server-authoritative; clients only read it. Replaces the old transient
        // RPC-only bar update, so a dropped packet or a puppet that spawned mid-fight still shows the
        // right bar to everyone (the RPC below now carries only the blood/flash cosmetics).
        private readonly NetworkVariable<float> _hp01 = new(
            1f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private static readonly int SpeedId = Animator.StringToHash("Speed");

        private void Awake()
        {
            _enemy = GetComponent<Enemy>();
            _health = GetComponent<Health>();
            _movement = GetComponent<EnemyMovement>();
            _animator = GetComponentInChildren<Animator>();
            _tf = transform;
        }

        public override void OnNetworkSpawn()
        {
            _lastPos = _tf.position;

            if (IsServer)
            {
                _enemy.OnSpawned();   // NGO-spawn doesn't fire IPoolable — init the brain manually
                _enemy.Aggro();       // spawned off-screen → march in immediately
                _enemy.DespawnRequested += ServerDespawn;
                _enemy.AttackTriggered += OnServerAttack;
                _health.Damaged += OnServerDamaged;
                _health.Died += OnServerDied;
            }
            else
            {
                // Client puppet — the server drives the transform; kill local sim so nothing fights it.
                _enemy.SuppressDespawn = true;
                if (_movement != null) _movement.enabled = false;
                if (_animator != null) { _animator.Rebind(); _animator.Update(0f); }

                // Bar follows the replicated HP. Apply the current value once for a puppet that spawned
                // already damaged (late join / out-of-view enemy hit before we saw it).
                _hp01.OnValueChanged += OnHpReplicated;
                if (_hp01.Value < 1f) _enemy.SetHealthBar(_hp01.Value);
            }
        }

        public override void OnNetworkDespawn()
        {
            if (IsServer)
            {
                _enemy.DespawnRequested -= ServerDespawn;
                _enemy.AttackTriggered -= OnServerAttack;
                _health.Damaged -= OnServerDamaged;
                _health.Died -= OnServerDied;
            }
            else
            {
                _hp01.OnValueChanged -= OnHpReplicated;
            }
        }

        // Client: the server moved the replicated HP fraction → update this puppet's floating bar.
        private void OnHpReplicated(float previous, float current) => _enemy.SetHealthBar(current);

        // ---- server-side authority ----

        private void OnServerDamaged(DamageInfo info, float current)
        {
            // current > 0 here (Died is raised separately). Replicate the HP fraction (drives every
            // client's bar via _hp01) and replay the blood + flash cosmetics.
            _hp01.Value = _health.Max > 0f ? current / _health.Max : 0f;
            ReactClientRpc(info.HitPoint, info.HitDirection, false);
        }

        private void OnServerDied()
        {
            _hp01.Value = 0f;
            ReactClientRpc(_tf.position, Vector3.up, true);
        }

        private void OnServerAttack() => AttackClientRpc();

        private void ServerDespawn()
        {
            if (NetworkObject != null && NetworkObject.IsSpawned) NetworkObject.Despawn(true);
        }

        /// <summary>Any player's weapon → server applies the authoritative damage.</summary>
        [Rpc(SendTo.Server)]
        public void DamageRpc(float amount, Vector3 point, Vector3 dir)
        {
            _health.TakeDamage(new DamageInfo(amount, point, dir, null));
        }

        // ---- client cosmetics ----

        [Rpc(SendTo.NotServer)]
        private void ReactClientRpc(Vector3 point, Vector3 dir, bool died)
        {
            if (died) { _enemy.PlayDeathFx(); return; }
            _enemy.PlayHitFx(new DamageInfo(0f, point, dir, null));
            // HP bar is driven by the replicated _hp01 (see OnHpReplicated), not this cosmetic RPC.
        }

        [Rpc(SendTo.NotServer)]
        private void AttackClientRpc() => _enemy.PlayAttackFx();

        private void Update()
        {
            if (IsServer) return; // server drives the brain; clients only animate the puppet

            float dt = Time.deltaTime;
            Vector3 p = _tf.position;
            float speed = (p - _lastPos).magnitude / Mathf.Max(1e-4f, dt);
            _lastPos = p;

            if (_animator != null) _animator.SetFloat(SpeedId, speed > 0.15f ? 1f : 0f);
            _enemy.ClientFxTick(dt);
        }
    }
}
