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
                GetComponent<PlayerLoadout>()?.Apply();
                SceneManager.sceneLoaded -= OnSceneLoaded;
                SceneManager.sceneLoaded += OnSceneLoaded;
                SubscribeNetworkSceneEvents();
                RefreshOwnerForActiveScene();
            }
            else
            {
                SetLocalSimulation(false);
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

        [Rpc(SendTo.Owner)]
        public void TakeDamageRpc(float amount, Vector3 from)
        {
            var health = GetComponent<Health>();
            if (health == null) return;
            Vector3 dir = transform.position - from;
            dir.y = 0f;
            health.TakeDamage(new DamageInfo(amount, transform.position, dir.normalized, null));
        }

        public void BroadcastFireFx(int weaponSlot, Vector3 origin, Vector3 direction)
        {
            if (!IsOwner) return;
            FireFxRpc(weaponSlot, origin, direction);
        }

        [Rpc(SendTo.NotOwner, Delivery = RpcDelivery.Unreliable)]
        private void FireFxRpc(int weaponSlot, Vector3 origin, Vector3 direction)
        {
            var wc = GetComponentInChildren<WeaponController>(true);
            if (wc == null) return;
            var weapon = wc.WeaponAt(weaponSlot);
            if (weapon != null) weapon.PlayRemoteFireFx(origin, direction);
        }

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
