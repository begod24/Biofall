using UnityEngine;
using Biofall.Core;

namespace Biofall.Gameplay
{
    /// <summary>
    /// A weapon's runtime behaviour (Encapsulation / Observer). Reads player intent, applies the
    /// data-driven fire mode (Single / Auto / Burst) + fire-rate + ammo + reload, then fires a
    /// HITSCAN along the player's AIM, spawns a pooled muzzle flash + tracer, plays SFX, drives the
    /// player Animator (Fire/Reload), and announces <see cref="WeaponFired"/>. Stats live in
    /// <see cref="WeaponData"/>; ammo is this weapon's own <see cref="AmmoSystem"/> (same GameObject).
    /// </summary>
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

        private float _nextFireTime;
        private bool _reloading;
        private float _reloadTimer;

        // burst state
        private float _heldTime;
        private int _burstShotsLeft;
        private float _nextBurstTime;

        private static readonly int FireId = Animator.StringToHash("Fire");
        private static readonly int ReloadId = Animator.StringToHash("Reload");

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
            _input = GetComponentInParent<PlayerInput>();
            _aim = GetComponentInParent<PlayerAim>();
            _animator = GetComponentInParent<Animator>();
        }

        private void Update()
        {
            if (Time.timeScale <= 0f) return; // paused
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
                // tap → a single shot
                _heldTime = 0f;
                if (Time.time >= _nextFireTime) TryFire();
            }
            else if (_input.FireHeld)
            {
                _heldTime += Time.deltaTime;
                if (_heldTime < data.holdToBurst) return; // still a tap, wait

                if (_burstShotsLeft <= 0 && Time.time >= _nextBurstTime)
                    _burstShotsLeft = data.burstCount; // start a new burst

                if (_burstShotsLeft > 0 && Time.time >= _nextFireTime)
                {
                    if (TryFire())
                    {
                        _burstShotsLeft--;
                        if (_burstShotsLeft <= 0) _nextBurstTime = Time.time + data.burstCooldown;
                    }
                    else _burstShotsLeft = 0; // out of ammo
                }
            }
            else
            {
                _heldTime = 0f;
                _burstShotsLeft = 0;
            }
        }

        /// <summary>Fire one round if ammo allows. Returns true if a shot actually went out.</summary>
        private bool TryFire()
        {
            if (_ammo != null && !_ammo.TryConsume(1)) return false;
            _nextFireTime = Time.time + 1f / Mathf.Max(0.01f, data.fireRate);

            Vector3 origin = muzzle != null ? muzzle.position : transform.position;
            Vector3 direction = GetAimDirection(origin);
            Quaternion rotation = Quaternion.LookRotation(direction);

            if (Physics.Raycast(origin, direction, out RaycastHit hit, data.range, hitMask, QueryTriggerInteraction.Ignore))
            {
                IDamageable target = hit.collider.GetComponentInParent<IDamageable>();
                target?.TakeDamage(new DamageInfo(data.damage, hit.point, direction, gameObject));
            }

            SpawnFromPool(data.muzzleFlashPrefab, origin, rotation);
            SpawnTracer(origin, rotation);

            if (data.shootSfx != null) audioSource.PlayOneShot(data.shootSfx);
            if (_animator != null) _animator.SetTrigger(FireId);
            EventBus.Publish(new WeaponFired(origin, direction));
            return true;
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
            if (_ammo == null) return;
            if (_ammo.Rounds >= _ammo.MagazineSize || _ammo.Reserve <= 0) return;

            _reloading = true;
            _reloadTimer = data.reloadTime;
            _burstShotsLeft = 0;
            if (data.reloadSfx != null) audioSource.PlayOneShot(data.reloadSfx);
            if (_animator != null) _animator.SetTrigger(ReloadId);
        }
    }
}
