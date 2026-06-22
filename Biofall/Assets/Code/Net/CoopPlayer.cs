using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using Biofall.Core;
using Biofall.Gameplay;
using Biofall.Gameplay.Mission1;

namespace Biofall.Net
{
    /// <summary>
    /// Lives only on the CO-OP player prefab variant (solo uses the plain Player prefab and never
    /// has this). On spawn it decides who drives this body:
    ///   • Owner  → register as the local player + point the camera at it; all the normal player
    ///     systems keep running, so the owner controls it exactly like in solo.
    ///   • Remote → disable the local simulation components (input/motor/aim/weapons), so this
    ///     machine doesn't drive someone else's character — the body just follows its
    ///     owner-authoritative <see cref="ClientNetworkTransform"/>.
    /// Keeps every player registered in <see cref="PlayerRegistry"/> for enemy targeting (Phase C).
    /// </summary>
    [RequireComponent(typeof(NetworkObject))]
    public sealed class CoopPlayer : NetworkBehaviour
    {
        private const string MissionSpawnAnchorName = "CoopSpawnPoint";
        private const string MissionFloorName = "Plane";
        private const float SpawnGroundProbeHeight = 12f;
        private const float SpawnGroundProbeDistance = 40f;
        private const float SpawnHeightPadding = 0.05f;

        private static readonly Vector3 MissionSpawnFallback = new(-2.1622f, 0.05f, 19.6f);

        private static readonly Vector3[] MissionSpawnOffsets =
        {
            Vector3.zero,
            new Vector3(2f, 0f, 0f),
            new Vector3(-2f, 0f, 0f),
            new Vector3(0f, 0f, 2f)
        };

        private Coroutine _ownerSceneRefresh;

        public override void OnNetworkSpawn()
        {
            if (IsOwner)
            {
                SetLocalSimulation(false);
                PlayerRegistry.SetLocal(transform);
                // Apply persistent upgrades to OUR body only (remotes are puppets, see else-branch).
                GetComponent<PlayerLoadout>()?.Apply();
                SceneManager.sceneLoaded -= OnSceneLoaded;
                SceneManager.sceneLoaded += OnSceneLoaded;
                SubscribeNetworkSceneEvents();
                RefreshOwnerForActiveScene();
            }
            else
            {
                SetLocalSimulation(false);
                // PlayerController.OnDisable just unregistered us — re-add so we stay a valid target.
                PlayerRegistry.Register(transform);
            }
        }

        public override void OnNetworkDespawn()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            UnsubscribeNetworkSceneEvents();
            if (_ownerSceneRefresh != null)
            {
                StopCoroutine(_ownerSceneRefresh);
                _ownerSceneRefresh = null;
            }
            PlayerRegistry.Unregister(transform);
        }

        /// <summary>
        /// Server → this player's OWNER: apply enemy melee damage locally. Player HP is owner-
        /// authoritative (each client owns its own <see cref="Health"/>), so the server can't write a
        /// remote player's HP directly — it asks the owner, whose local Health.TakeDamage then drives
        /// PlayerHealthReporter → HUD / camera shake / low-health FX exactly like solo.
        /// </summary>
        [Rpc(SendTo.Owner)]
        public void TakeDamageRpc(float amount, Vector3 from)
        {
            var health = GetComponent<Health>();
            if (health == null) return;
            Vector3 dir = transform.position - from;
            dir.y = 0f;
            health.TakeDamage(new DamageInfo(amount, transform.position, dir.normalized, null));
        }

        /// <summary>
        /// CO-OP: the OWNER broadcasts a shot so teammates replay this player's muzzle flash + tracer.
        /// The hitscan + damage are owner/server-side (see <see cref="Weapon"/>); this is purely visual.
        /// </summary>
        public void BroadcastFireFx(int weaponSlot, Vector3 origin, Vector3 direction)
        {
            if (!IsOwner) return;
            FireFxRpc(weaponSlot, origin, direction);
        }

        // Unreliable: this is purely cosmetic (muzzle flash + tracer) and fires on every bullet, so it
        // must not clog the reliable channel — a dropped tracer is invisible, but reliable spam on WiFi
        // causes head-of-line blocking that freezes everyone's movement/anim. The hitscan + damage are
        // owner/server-authoritative and unaffected.
        [Rpc(SendTo.NotOwner, Delivery = RpcDelivery.Unreliable)]
        private void FireFxRpc(int weaponSlot, Vector3 origin, Vector3 direction)
        {
            var wc = GetComponentInChildren<WeaponController>(true);
            if (wc == null) return;
            var weapon = wc.WeaponAt(weaponSlot);
            if (weapon != null) weapon.PlayRemoteFireFx(origin, direction);
        }

        /// <summary>
        /// Toggle full owner control of this body. Used by <see cref="CoopPlayerLife"/> to freeze the
        /// player while downed/dead and restore it on revive. Only meaningful on the owner (remote
        /// replicas are already puppets). Single source of which components count as "control".
        /// </summary>
        public void SetControllable(bool enabled) => SetLocalSimulation(enabled);

        private void SetLocalSimulation(bool enabled)
        {
            if (!enabled) SetEnabled<PlayerController>(false);

            SetEnabled<PlayerInput>(enabled);
            SetEnabled<PlayerMotor>(enabled);
            SetEnabled<PlayerAim>(enabled);
            SetEnabled<WeaponController>(enabled);
            SetEnabled<GrenadeThrower>(enabled);
            SetEnabled<PlayerInteractor>(enabled);

            // Animation is replicated by OwnerNetworkAnimator: only the OWNER may drive the Animator
            // params/triggers, so disable the local drivers on remote replicas — otherwise a remote
            // body would animate from THIS machine's input and fight the synced values.
            SetEnabled<PlayerAnimator>(enabled);
            SetEnabled<PlayerDeath>(enabled);

            foreach (var weapon in GetComponentsInChildren<Weapon>(true))
                weapon.enabled = enabled;

            if (enabled) SetEnabled<PlayerController>(true);
        }

        private void SetEnabled<T>(bool enabled) where T : Behaviour
        {
            var c = GetComponent<T>();
            if (c != null) c.enabled = enabled;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (!IsOwner) return;
            RefreshOwnerForActiveScene();
        }

        private void OnNetworkSceneEvent(SceneEvent sceneEvent)
        {
            if (!IsOwner) return;
            if (sceneEvent.SceneName != GameScenes.MissionCoop) return;

            if (sceneEvent.SceneEventType == SceneEventType.LoadComplete ||
                sceneEvent.SceneEventType == SceneEventType.LoadEventCompleted ||
                sceneEvent.SceneEventType == SceneEventType.SynchronizeComplete)
            {
                RefreshOwnerForActiveScene();
            }
        }

        private void RefreshOwnerForActiveScene()
        {
            if (_ownerSceneRefresh != null) StopCoroutine(_ownerSceneRefresh);
            _ownerSceneRefresh = StartCoroutine(RefreshOwnerForActiveSceneRoutine());
        }

        private IEnumerator RefreshOwnerForActiveSceneRoutine()
        {
            SetLocalSimulation(false);
            ResetMotion();
            PlayerRegistry.SetLocal(transform);

            // Let Unity and NGO finish scene activation before we override the persisted player state.
            yield return null;
            yield return null;

            if (SceneManager.GetActiveScene().name != GameScenes.MissionCoop)
            {
                BindLocalCamera();
                _ownerSceneRefresh = null;
                yield break;
            }

            for (int i = 0; i < 3; i++)
            {
                MoveToSpawn(ResolveMissionSpawnPosition(OwnerClientId));
                BindLocalCamera();
                yield return null;
            }

            SetLocalSimulation(true);
            PlayerRegistry.SetLocal(transform);
            _ownerSceneRefresh = null;
        }

        private void MoveToSpawn(Vector3 position)
        {
            var controller = GetComponent<CharacterController>();
            bool controllerWasEnabled = controller != null && controller.enabled;

            if (controller != null) controller.enabled = false;
            ResetMotion();
            transform.SetPositionAndRotation(position, Quaternion.identity);
            if (controller != null) controller.enabled = controllerWasEnabled;
        }

        private void ResetMotion()
        {
            var motor = GetComponent<PlayerMotor>();
            if (motor != null) motor.ResetVerticalVelocity();
        }

        public static Vector3 ResolveMissionSpawnPosition(ulong ownerClientId)
        {
            Vector3 position = MissionSpawnFallback;
            Vector3 offset = MissionSpawnOffsets[(int)(ownerClientId % (ulong)MissionSpawnOffsets.Length)];

            GameObject anchor = GameObject.Find(MissionSpawnAnchorName);
            if (anchor != null)
            {
                position = anchor.transform.position;
            }
            else
            {
                GameObject floor = GameObject.Find(MissionFloorName);
                if (floor != null && floor.TryGetComponent(out Renderer renderer))
                {
                    Bounds bounds = renderer.bounds;
                    position = new Vector3(bounds.center.x, bounds.max.y + SpawnHeightPadding, bounds.center.z);
                }
            }

            position += offset;

            if (NavMesh.SamplePosition(position, out NavMeshHit navHit, 8f, NavMesh.AllAreas))
                position = navHit.position + Vector3.up * SpawnHeightPadding;

            Vector3 rayOrigin = position + Vector3.up * SpawnGroundProbeHeight;
            if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, SpawnGroundProbeDistance, ~0, QueryTriggerInteraction.Ignore))
                position.y = hit.point.y + SpawnHeightPadding;

            return position;
        }

        private void BindLocalCamera()
        {
            var cam = FindFirstObjectByType<TopDownCamera>();
            if (cam != null) cam.SetTarget(transform);
        }

        private void SubscribeNetworkSceneEvents()
        {
            var nm = NetworkManager != null ? NetworkManager : Unity.Netcode.NetworkManager.Singleton;
            if (nm == null || nm.SceneManager == null) return;
            nm.SceneManager.OnSceneEvent -= OnNetworkSceneEvent;
            nm.SceneManager.OnSceneEvent += OnNetworkSceneEvent;
        }

        private void UnsubscribeNetworkSceneEvents()
        {
            var nm = NetworkManager != null ? NetworkManager : Unity.Netcode.NetworkManager.Singleton;
            if (nm == null || nm.SceneManager == null) return;
            nm.SceneManager.OnSceneEvent -= OnNetworkSceneEvent;
        }
    }
}
