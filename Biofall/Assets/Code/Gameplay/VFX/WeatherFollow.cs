using UnityEngine;
using Biofall.Core;

namespace Biofall.Gameplay
{
    public sealed class WeatherFollow : MonoBehaviour
    {
        [SerializeField] private float height = 16f;

        private Transform _tf;

        private void Awake() => _tf = transform;

        private void LateUpdate()
        {
            if (!PlayerRegistry.HasPlayer) return;
            Vector3 p = PlayerRegistry.Player.position;
            _tf.position = new Vector3(p.x, height, p.z);
        }
    }
}
