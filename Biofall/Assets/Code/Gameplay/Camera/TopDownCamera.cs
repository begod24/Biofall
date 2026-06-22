using UnityEngine;
using Biofall.Core;

namespace Biofall.Gameplay
{
    public sealed class TopDownCamera : MonoBehaviour
    {
        [Header("Target")]
        [Tooltip("Optional. If empty, uses PlayerRegistry.Player.")]
        [SerializeField] private Transform target;

        [Header("Rig")]
        [Tooltip("Downward tilt in degrees (Zombie Shooter ~60).")]
        [SerializeField] private float pitch = 60f;
        [Tooltip("Distance from the focus point along the view direction.")]
        [SerializeField] private float distance = 14f;
        [Tooltip("Follow smoothing (higher = snappier).")]
        [SerializeField] private float followLerp = 10f;

        [Header("Cursor lookahead")]
        [Tooltip("How far the framing leans toward the cursor (0..1 of cursor offset).")]
        [SerializeField, Range(0f, 1f)] private float lookaheadAmount = 0.3f;
        [Tooltip("Max world distance the focus can lean toward the cursor.")]
        [SerializeField] private float maxLookahead = 4f;

        [Header("Shake (on player hit)")]
        [SerializeField] private float shakeAmplitude = 0.35f;
        [SerializeField] private float shakeDecay = 1.6f;

        private InputReader _input;
        private Camera _camera;
        private float _shake;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            _input = FindAnyObjectByType<InputReader>();
            transform.rotation = Quaternion.Euler(pitch, 0f, 0f);
        }

        public void SetTarget(Transform t) => target = t;

        private void OnEnable()
        {
            EventBus.Subscribe<PlayerDamaged>(OnPlayerDamaged);
            EventBus.Subscribe<CameraShake>(OnCameraShake);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<PlayerDamaged>(OnPlayerDamaged);
            EventBus.Unsubscribe<CameraShake>(OnCameraShake);
        }

        private void OnPlayerDamaged(PlayerDamaged e)
        {
            float intensity = GameSettings.CameraShakeIntensity;
            if (intensity <= 0f) return;
            if (e.Amount > 0f) _shake = shakeAmplitude * intensity;
        }

        private void OnCameraShake(CameraShake e)
        {
            float intensity = GameSettings.CameraShakeIntensity;
            if (intensity <= 0f) return;
            float amplitude = e.Amplitude * intensity;
            if (amplitude > _shake) _shake = amplitude;
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                if (!PlayerRegistry.HasPlayer) return;
                target = PlayerRegistry.Player;
            }

            Quaternion rotation = Quaternion.Euler(pitch, 0f, 0f);
            Vector3 viewDir = rotation * Vector3.forward;

            Vector3 focus = target.position + ComputeLookahead(rotation);
            Vector3 desiredPosition = focus - viewDir * distance;

            transform.position = Vector3.Lerp(transform.position, desiredPosition, followLerp * Time.deltaTime);

            if (_shake > 0f)
            {
                Vector2 jitter = Random.insideUnitCircle * _shake;
                transform.position += new Vector3(jitter.x, 0f, jitter.y);
                _shake = Mathf.MoveTowards(_shake, 0f, shakeDecay * Time.deltaTime);
            }

            transform.rotation = rotation;
        }

        private Vector3 ComputeLookahead(Quaternion rotation)
        {
            if (_input == null || _camera == null || lookaheadAmount <= 0f)
                return Vector3.zero;

            Ray ray = _camera.ScreenPointToRay(_input.PointerScreenPosition);
            Plane ground = new Plane(Vector3.up, new Vector3(0f, target.position.y, 0f));
            if (!ground.Raycast(ray, out float dist)) return Vector3.zero;

            Vector3 cursorPoint = ray.GetPoint(dist);
            Vector3 offset = (cursorPoint - target.position) * lookaheadAmount;
            offset.y = 0f;
            return Vector3.ClampMagnitude(offset, maxLookahead);
        }
    }
}
