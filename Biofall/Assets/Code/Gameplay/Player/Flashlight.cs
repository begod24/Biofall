using UnityEngine;

namespace Biofall.Gameplay
{
    [RequireComponent(typeof(Light))]
    public sealed class Flashlight : MonoBehaviour
    {
        [SerializeField] private PlayerAim aim;
        [Tooltip("0 = horizontal beam into the distance, higher = angled down toward nearby ground.")]
        [SerializeField, Range(0f, 1f)] private float downTilt = 0.22f;

        private Transform _tf;

        private void Awake()
        {
            _tf = transform;
            if (aim == null) aim = GetComponentInParent<PlayerAim>();
        }

        private void LateUpdate()
        {
            if (aim == null) return;

            Vector3 flat = aim.AimPoint - _tf.position;
            flat.y = 0f;
            if (flat.sqrMagnitude < 0.0001f) return;

            Vector3 dir = flat.normalized;
            dir.y = -downTilt;
            _tf.rotation = Quaternion.LookRotation(dir);
        }
    }
}
