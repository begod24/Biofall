using UnityEngine;

namespace Biofall.Spawning
{
    public class EnemySpawner : MonoBehaviour
    {
        [SerializeField] private float radius = 1f;
        [SerializeField] private Color gizmoColor = new Color(1f, 0.2f, 0.2f, 0.8f);

        public Vector3 GetSpawnPoint()
        {
            Vector2 rnd = Random.insideUnitCircle * radius;
            return transform.position + new Vector3(rnd.x, 0f, rnd.y);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = gizmoColor;
            Gizmos.DrawWireSphere(transform.position, radius);
            Gizmos.DrawLine(transform.position, transform.position + Vector3.up * 2f);
        }
    }
}
