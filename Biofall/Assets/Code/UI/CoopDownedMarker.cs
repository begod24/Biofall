using UnityEngine;
using TMPro;
using Biofall.Core;
using Biofall.Net;

namespace Biofall.UI
{
    public sealed class CoopDownedMarker : MonoBehaviour
    {
        [SerializeField] private RectTransform marker;
        [SerializeField] private RectTransform arrow;
        [SerializeField] private TMP_Text label;
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioClip manDownSfx;
        [Tooltip("Keep the marker this many px inside the screen edges.")]
        [SerializeField] private float edgePadding = 64f;
        [Tooltip("World height above the downed body to anchor the marker.")]
        [SerializeField] private float worldHeight = 2.2f;
        [Tooltip("Arrow's default 'forward' is up; offset if your sprite points elsewhere.")]
        [SerializeField] private float arrowAngleOffset = -90f;

        private Camera _cam;

        private void OnEnable() => EventBus.Subscribe<TeammateDowned>(OnTeammateDowned);
        private void OnDisable() => EventBus.Unsubscribe<TeammateDowned>(OnTeammateDowned);

        private void OnTeammateDowned(TeammateDowned _)
        {
            if (sfxSource != null && manDownSfx != null) sfxSource.PlayOneShot(manDownSfx);
        }

        private void Update()
        {
            Transform target = NearestDowned();
            bool show = NetSession.InCoop && target != null;
            if (marker != null) marker.gameObject.SetActive(show);
            if (!show)
            {
                if (arrow != null) arrow.gameObject.SetActive(false);
                return;
            }

            if (_cam == null) _cam = Camera.main;
            if (_cam == null) return;

            Vector3 sp = _cam.WorldToScreenPoint(target.position + Vector3.up * worldHeight);
            bool behind = sp.z < 0f;
            if (behind) { sp.x = Screen.width - sp.x; sp.y = Screen.height - sp.y; }

            bool onScreen = !behind && sp.x >= 0f && sp.x <= Screen.width && sp.y >= 0f && sp.y <= Screen.height;

            Vector2 clamped = new(
                Mathf.Clamp(sp.x, edgePadding, Screen.width - edgePadding),
                Mathf.Clamp(sp.y, edgePadding, Screen.height - edgePadding));

            if (marker != null) marker.position = clamped;
            if (label != null) label.text = "DOWN";

            if (arrow != null)
            {
                arrow.gameObject.SetActive(!onScreen);
                if (!onScreen)
                {
                    Vector2 center = new(Screen.width * 0.5f, Screen.height * 0.5f);
                    Vector2 dir = (new Vector2(sp.x, sp.y) - center);
                    if (dir.sqrMagnitude < 1f) dir = Vector2.up;
                    float ang = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + arrowAngleOffset;
                    arrow.position = clamped;
                    arrow.rotation = Quaternion.Euler(0f, 0f, ang);
                }
            }
        }

        private static Transform NearestDowned()
        {
            var all = CoopPlayerLife.All;
            Transform localTf = PlayerRegistry.LocalPlayer;
            Vector3 from = localTf != null ? localTf.position : Vector3.zero;

            Transform best = null;
            float bestSqr = float.MaxValue;
            for (int i = 0; i < all.Count; i++)
            {
                var life = all[i];
                if (life == null || life.IsOwner || !life.IsDowned) continue;
                float sqr = (life.transform.position - from).sqrMagnitude;
                if (sqr < bestSqr) { bestSqr = sqr; best = life.transform; }
            }
            return best;
        }
    }
}
