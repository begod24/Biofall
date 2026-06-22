using UnityEngine;

namespace Biofall.Core
{
    [DefaultExecutionOrder(-1000)]
    public sealed class Bootstrap : MonoBehaviour
    {
        [Header("Wiring")]
        [SerializeField] private InputReader inputReader;

        [Header("Debug")]
        [Tooltip("Log raw input to the console (Phase 0 checkpoint).")]
        [SerializeField] private bool logInput = true;

        private bool _wasMoving;

        private void Awake()
        {
            EventBus.Clear();
            CurrencyWallet.Reset();

            EnsurePoolService();

            if (inputReader == null)
                inputReader = FindAnyObjectByType<InputReader>();

            Debug.Log("[Biofall] Bootstrap ready — EventBus cleared, PoolService live.");
        }

        private void EnsurePoolService()
        {
            if (PoolService.Instance != null) return;
            var go = new GameObject("PoolService");
            go.AddComponent<PoolService>();
        }

        private void Update()
        {
            if (!logInput || inputReader == null) return;

            if (inputReader.FirePressed)
                Debug.Log($"[Input] Fire @ screen {inputReader.PointerScreenPosition}");

            if (inputReader.ReloadPressed)
                Debug.Log("[Input] Reload");

            bool moving = inputReader.Move != Vector2.zero;
            if (moving != _wasMoving)
            {
                Debug.Log(moving ? $"[Input] Move start {inputReader.Move}" : "[Input] Move stop");
                _wasMoving = moving;
            }
        }
    }
}
