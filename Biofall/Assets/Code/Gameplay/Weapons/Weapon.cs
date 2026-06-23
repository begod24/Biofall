using UnityEngine;
using Biofall.Core;
using Biofall.Net;

namespace Biofall.Gameplay
{
    [RequireComponent(typeof(AudioSource))]
    [RequireComponent(typeof(AmmoSystem))]
    public sealed class Weapon : MonoBehaviour
    {
        [SerializeField] private WeaponData data;
        [SerializeField] private Transform muzzle;
        [SerializeField] private AudioSource audioSource;
        [Tooltip("Layers the hitscan can hit.")]
        [SerializeField] private LayerMask hitMask = ~0;

        private PlayerInput _input;
        private AmmoSystem _ammo;
        private PlayerAim _aim;
        private Animator _animator;
        private OwnerNetworkAnimator _netAnimator;
        private WeaponController _weaponController;
        private CoopPlayer _coopPlayer;

        private float _nextFireTime;
        private bool _reloading;
        private float _reloadTimer;

        private float _heldTime;
        private int _burstShotsLeft;
        private float _nextBurstTime;

        private static readonly int FireId = Animator.StringToHash("Fire");
        private static readonly int ReloadId = Animator.StringToHash("Reload");

        public bool InfiniteAmmo => data != null && data.infiniteAmmo;

        private void Awake()
        {
            if (muzzle == null)
            {
                Transform found = transform.Find("Muzzle");
                if (found != null) muzzle = found;
            }
            if (audioSource == null) audioSource = GetComponent<AudioSource>();
            audioSource.playOnAwake = false;

            _ammo = GetComponent<AmmoSystem>();
            if (_ammo != null && data != null) _ammo.SetInfinite(data.infiniteAmmo);
            _input = GetComponentInParent<PlayerInput>();
            _aim = GetComponentInParent<PlayerAim>();
            _animator = GetComponentInParent<Animator>();
            _netAnimator = GetComponentInParent<OwnerNetworkAnimator>();
            _weaponController = GetComponentInParent<WeaponController>();
            _coopPlayer = GetComponentInParent<CoopPlayer>();
        }

        private void PlayAnimTrigger(int hash)
        {
            if (_netAnimator != null) _netAnimator.SetTrigger(hash);
            else if (_animator != null) _animator.SetTrigger(hash);
        }

        private void Update()
        {
            if (Time.timeScale <= 0f) return;
            if (_input == null || data == null) return;

            if (_reloading)
            {
                _reloadTimer -= Time.deltaTime;
                if (_reloadTimer <= 0f)
                {
                    _ammo?.Reload();
                    _reloading = false;
                }
                return;
            }

            if (_input.ReloadPressed)
            {
                TryStartReload();
                return;
            }

            HandleFire();
        }

        private void HandleFire()
        {
            switch (data.fireMode)
            {
                case WeaponData.FireMode.Single:
                    if (_input.FirePressed && Time.time >= _nextFireTime) TryFire();
                    break;

                case WeaponData.FireMode.Auto:
                    if (_input.FireHeld && Time.time >= _nextFireTime) TryFire();
                    break;

                case WeaponData.FireMode.Burst:
                    HandleBurst();
                    break;
            }
        }

        private void HandleBurst()
        {
            if (_input.FirePressed)
            {
                _heldTime = 0f;
                if (Time.time >= _nextFireTime) TryFire();
            }
            else if (_input.FireHeld)
            {
                _heldTime += Time.deltaTime;
                if (_heldTime < data.holdToBurst) return;

                if (_burstShotsLeft <= 0 && Time.time >= _nextBurstTime)
                    _burstShotsLeft = data.burstCount;

                if (_burstShotsLeft > 0 && Time.time >= _nextFireTime)
                {
                    if (TryFire())
                    {
                        _burstShotsLeft--;
                        if (_burstShotsLeft <= 0) _nextBurstTime = Time.time + data.burstCooldown;
                    }
                    else _burstShotsLeft = 0;
                }
            }
            else
            {
                _heldTime = 0f;
                _burstShotsLeft = 0;
            }
        }

        private bool TryFire()
        {
            if (!data.infiniteAmmo && _ammo != null && !_ammo.TryConsume(1)) return false;
            _nextFireTime = Time.time + 1f / Mathf.Max(0.01f, data.fireRate);

            Vector3 origin = muzzle != null ? muzzle.position : transform.position;
            Vector3 baseDir = GetAimDirection(origin);

            // One trigger pull = one round, but it can spit several pellets in a cone (shotgun).
            int pellets = Mathf.Max(1, data.pelletsPerShot);
            for (int i = 0; i < pellets; i++)
            {
                Vector3 dir = pellets > 1 ? ApplySpread(baseDir, data.spreadAngle) : baseDir;
                FirePellet(origin, dir);
            }

            // Muzzle flash, SFX, animation and network FX fire once per shot, not per pellet.
            SpawnFromPool(data.muzzleFlashPrefab, origin, Quaternion.LookRotation(baseDir));
            if (NetSession.InCoop && _coopPlayer != null && _weaponController != null)
                _coopPlayer.BroadcastFireFx(_weaponController.ActiveSlot, origin, baseDir);

            if (data.shootSfx != null) audioSource.PlayOneShot(data.shootSfx);
            PlayAnimTrigger(FireId);
            EventBus.Publish(new WeaponFired(origin, baseDir));
            return true;
        }

        private void FirePellet(Vector3 origin, Vector3 direction)
        {
            if (Physics.Raycast(origin, direction, out RaycastHit hit, data.range, hitMask, QueryTriggerInteraction.Ignore))
            {
                if (NetSession.InCoop)
                {
                    var coopEnemy = hit.collider.GetComponentInParent<CoopEnemy>();
                    if (coopEnemy != null) coopEnemy.DamageRpc(data.damage, hit.point, direction);
                }
                else
                {
                    IDamageable target = hit.collider.GetComponentInParent<IDamageable>();
                    target?.TakeDamage(new DamageInfo(data.damage, hit.point, direction, gameObject));
                }
            }

            SpawnTracer(origin, Quaternion.LookRotation(direction));
        }

        // Random horizontal deflection inside the spread cone (top-down, so spread stays on the XZ plane).
        private static Vector3 ApplySpread(Vector3 direction, float spreadAngle)
        {
            if (spreadAngle <= 0f) return direction;
            float half = spreadAngle * 0.5f;
            return Quaternion.AngleAxis(Random.Range(-half, half), Vector3.up) * direction;
        }

        private void PlayFireFx(Vector3 origin, Quaternion rotation)
        {
            SpawnFromPool(data.muzzleFlashPrefab, origin, rotation);
            SpawnTracer(origin, rotation);
        }

        public void PlayRemoteFireFx(Vector3 origin, Vector3 direction)
        {
            if (data == null) return;
            Quaternion rot = direction.sqrMagnitude > 1e-4f ? Quaternion.LookRotation(direction) : transform.rotation;
            PlayFireFx(origin, rot);
        }

        private Vector3 GetAimDirection(Vector3 origin)
        {
            if (_aim != null)
            {
                Vector3 toAim = _aim.AimPoint - origin;
                toAim.y = 0f;
                if (toAim.sqrMagnitude > 0.0001f) return toAim.normalized;
            }

            Vector3 forward = transform.root.forward;
            forward.y = 0f;
            return forward.sqrMagnitude > 0.0001f ? forward.normalized : Vector3.forward;
        }

        private void SpawnTracer(Vector3 pos, Quaternion rot)
        {
            GameObject tracer = SpawnFromPool(data.bulletPrefab, pos, rot);
            if (tracer != null && tracer.TryGetComponent(out Bullet bullet))
                bullet.Launch(data.bulletSpeed, data.range / Mathf.Max(1f, data.bulletSpeed));
        }

        private static GameObject SpawnFromPool(GameObject prefab, Vector3 pos, Quaternion rot)
        {
            if (prefab == null || PoolService.Instance == null) return null;
            return PoolService.Instance.Spawn(prefab, pos, rot);
        }

        private void TryStartReload()
        {
            if (data.infiniteAmmo) return;
            if (_ammo == null) return;
            if (_ammo.Rounds >= _ammo.MagazineSize || _ammo.Reserve <= 0) return;

            _reloading = true;
            _reloadTimer = data.reloadTime;
            _burstShotsLeft = 0;
            if (data.reloadSfx != null) audioSource.PlayOneShot(data.reloadSfx);
            PlayAnimTrigger(ReloadId);
        }
    }
}
