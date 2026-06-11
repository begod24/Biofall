using UnityEngine;

namespace Biofall.GameCamera
{
    /// <summary>
    /// Fixed-angle top-down follow camera with smoothing. Keeps the player and the
    /// surrounding swarm readable. Runs in LateUpdate so it follows after movement.
    /// </summary>
    public class TopDownCameraRig : MonoBehaviour
    {
        [SerializeField] private Transform _target;
        [Tooltip("Offset from target in world space. Default = high and pulled back for an angled top-down view.")]
        [SerializeField] private Vector3 _offset = new Vector3(0f, 16f, -8f);
        [SerializeField] private float _followSmoothTime = 0.12f;
        [Tooltip("How far the camera leans toward the aim point (0 = pure follow).")]
        [SerializeField] private float _aimLeadFactor = 0f;

        private Vector3 _velocity;

        public void SetTarget(Transform target) => _target = target;

        private void LateUpdate()
        {
            if (_target == null) return;

            Vector3 desired = _target.position + _offset;
            if (_aimLeadFactor > 0f)
                desired += _target.forward * _aimLeadFactor;

            transform.position = Vector3.SmoothDamp(transform.position, desired, ref _velocity, _followSmoothTime);
            transform.LookAt(_target.position);
        }
    }
}
