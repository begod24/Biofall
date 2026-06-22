using UnityEngine;
using UnityEngine.InputSystem;

namespace Biofall.UI
{
    [RequireComponent(typeof(RectTransform))]
    public sealed class CrosshairUI : MonoBehaviour
    {
        [Tooltip("Hide the operating-system cursor while the crosshair is active.")]
        [SerializeField] private bool hideHardwareCursor = true;

        private RectTransform _rect;

        private void Awake()
        {
            _rect = (RectTransform)transform;
        }

        private void OnEnable()
        {
            if (hideHardwareCursor) Cursor.visible = false;
        }

        private void OnDisable()
        {
            if (hideHardwareCursor) Cursor.visible = true;
        }

        private void Update()
        {
            if (hideHardwareCursor) Cursor.visible = UiOverlay.Active;

            var mouse = Mouse.current;
            if (mouse == null) return;

            _rect.position = mouse.position.ReadValue();
        }
    }
}
