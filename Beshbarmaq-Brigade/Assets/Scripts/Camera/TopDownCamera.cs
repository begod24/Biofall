using UnityEngine;

namespace Biofall.Cameras
{
    public class TopDownCamera : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 offset = new Vector3(0f, 14f, -7f);
        [SerializeField] private float positionDamping = 6f;
        [SerializeField] private float pitch = 60f;
        [SerializeField] private float yaw = 0f;

        public void SetTarget(Transform t)
        {
            target = t;
        }

        private void LateUpdate()
        {
            if (target == null) return;
            Vector3 desired = target.position + offset;
            transform.position = Vector3.Lerp(transform.position, desired, Time.deltaTime * positionDamping);
            transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
        }
    }
}
