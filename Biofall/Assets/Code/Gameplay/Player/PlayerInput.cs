using UnityEngine;
using Biofall.Core;

namespace Biofall.Gameplay
{
    public sealed class PlayerInput : MonoBehaviour
    {
        private InputReader _reader;

        private InputReader Reader
        {
            get
            {
                if (_reader == null) _reader = FindAnyObjectByType<InputReader>();
                return _reader;
            }
        }

        public Vector2 Move => Reader != null ? Reader.Move : Vector2.zero;

        public Vector2 PointerScreenPosition => Reader != null ? Reader.PointerScreenPosition : Vector2.zero;

        public bool FireHeld => Reader != null && Reader.FireHeld;
        public bool FirePressed => Reader != null && Reader.FirePressed;
        public bool ReloadPressed => Reader != null && Reader.ReloadPressed;

        public bool GrenadePressed => Reader != null && Reader.GrenadePressed;

        public bool InteractPressed => Reader != null && Reader.InteractPressed;

        public bool InteractHeld => Reader != null && Reader.InteractHeld;

        public int WeaponSlot => Reader != null ? Reader.WeaponSlot : 0;

        private void Awake()
        {
            _reader = FindAnyObjectByType<InputReader>();
        }
    }
}
