using UnityEngine;
using UnityEngine.InputSystem;

namespace Biofall.UI
{
    /// <summary>
    /// Replaces the hardware cursor with a crosshair sprite that tracks the mouse.
    /// Pure presentation: hides the OS cursor while active and snaps its RectTransform
    /// to the pointer each frame. Place on the crosshair Image inside a Screen Space –
    /// Overlay canvas (so screen pixels map 1:1 to RectTransform.position).
    /// </summary>
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
            // Hide the OS cursor during gameplay, but release it while a menu overlay is up.
            if (hideHardwareCursor) Cursor.visible = UiOverlay.Active;

            var mouse = Mouse.current;
            if (mouse == null) return;

            _rect.position = mouse.position.ReadValue();
        }
    }
}
