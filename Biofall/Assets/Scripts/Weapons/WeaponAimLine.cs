using UnityEngine;

namespace Biofall.Weapons
{
    /// <summary>
    /// Draws a laser from the muzzle along the aim direction to the first thing it hits,
    /// with an optional dot at the impact point — so you can see exactly where the weapon
    /// is pointing and where a shot will land. Mirrors the weapon's range and hit mask so
    /// the line matches real bullets. Pure visual; does no damage.
    ///
    /// Lives on its own un-scaled GameObject (LineRenderer width is affected by transform
    /// scale), with the muzzle assigned separately.
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public class WeaponAimLine : MonoBehaviour
    {
        [SerializeField] private Transform _muzzle;
        [Tooltip("Optional. If set, range + hit mask are taken from this weapon so the line matches real shots.")]
        [SerializeField] private HitscanWeapon _weapon;

        [Header("Fallback (used when no weapon is assigned)")]
        [SerializeField] private float _fallbackRange = 50f;
        [SerializeField] private LayerMask _fallbackMask = ~0;

        [Header("Appearance")]
        [SerializeField] private Color _color = new Color(1f, 0.2f, 0.15f, 0.9f);
        [SerializeField] private float _width = 0.025f;
        [SerializeField] private bool _showImpactDot = true;
        [SerializeField] private float _dotSize = 0.18f;

        private LineRenderer _line;
        private Transform _dot;

        public void Configure(Transform muzzle, HitscanWeapon weapon)
        {
            _muzzle = muzzle;
            _weapon = weapon;
        }

        private void Awake()
        {
            _line = GetComponent<LineRenderer>();
            ConfigureLine();
            if (_showImpactDot) CreateDot();
        }

        private void LateUpdate()
        {
            Transform origin = _muzzle != null ? _muzzle : transform;
            Vector3 start = origin.position;
            Vector3 dir = origin.forward;

            float range = _weapon != null && _weapon.Data != null ? _weapon.Data.Range : _fallbackRange;
            int mask = _weapon != null ? _weapon.HitMask.value : _fallbackMask.value;
            QueryTriggerInteraction triggers = _weapon != null ? _weapon.TriggerInteraction : QueryTriggerInteraction.Collide;

            bool hit = Physics.Raycast(start, dir, out RaycastHit hitInfo, range, mask, triggers);
            Vector3 end = hit ? hitInfo.point : start + dir * range;

            _line.SetPosition(0, start);
            _line.SetPosition(1, end);

            if (_dot != null)
            {
                _dot.gameObject.SetActive(hit);
                if (hit) _dot.position = hitInfo.point + hitInfo.normal * 0.02f;
            }
        }

        private void ConfigureLine()
        {
            _line.useWorldSpace = true;
            _line.positionCount = 2;
            _line.widthMultiplier = _width;
            _line.numCapVertices = 2;
            _line.textureMode = LineTextureMode.Stretch;
            _line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _line.receiveShadows = false;
            _line.alignment = LineAlignment.View;

            var mat = new Material(Shader.Find("Sprites/Default"));
            _line.material = mat;
            _line.startColor = _color;
            _line.endColor = new Color(_color.r, _color.g, _color.b, _color.a * 0.4f);
        }

        private void CreateDot()
        {
            var dot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            dot.name = "AimImpactDot";
            var col = dot.GetComponent<Collider>();
            if (col != null) Destroy(col);

            var renderer = dot.GetComponent<Renderer>();
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            var dotMat = new Material(Shader.Find("Sprites/Default")) { color = _color };
            renderer.material = dotMat;

            _dot = dot.transform;
            _dot.SetParent(transform, false);
            _dot.localScale = Vector3.one * _dotSize;
        }
    }
}
