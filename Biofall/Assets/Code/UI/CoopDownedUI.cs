using System.Collections;
using UnityEngine;
using TMPro;
using Biofall.Core;

namespace Biofall.UI
{
    public sealed class CoopDownedUI : MonoBehaviour
    {
        [Header("Downed banner (this player)")]
        [SerializeField] private GameObject downedPanel;
        [SerializeField] private Transform bleedFill;
        [SerializeField] private TMP_Text downedLabel;
        [SerializeField] private string downedText = "YOU ARE DOWN — HOLD ON";
        [SerializeField] private string eliminatedText = "YOU DIED — WAITING FOR YOUR SQUAD";

        [Header("Revive widget (reviving a teammate)")]
        [SerializeField] private GameObject revivePanel;
        [SerializeField] private Transform reviveFill;
        [SerializeField] private TMP_Text reviveLabel;
        [SerializeField] private string reviveHint = "HOLD E TO REVIVE";
        [SerializeField] private string revivingText = "REVIVING…";

        private Coroutine _bleed;

        private void Awake()
        {
            if (downedPanel != null) downedPanel.SetActive(false);
            if (revivePanel != null) revivePanel.SetActive(false);
        }

        private void OnEnable()
        {
            EventBus.Subscribe<PlayerDowned>(OnDowned);
            EventBus.Subscribe<PlayerRevived>(OnRevived);
            EventBus.Subscribe<PlayerEliminated>(OnEliminated);
            EventBus.Subscribe<ReviveProgress>(OnReviveProgress);
            EventBus.Subscribe<TeamWiped>(OnTeamWiped);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<PlayerDowned>(OnDowned);
            EventBus.Unsubscribe<PlayerRevived>(OnRevived);
            EventBus.Unsubscribe<PlayerEliminated>(OnEliminated);
            EventBus.Unsubscribe<ReviveProgress>(OnReviveProgress);
            EventBus.Unsubscribe<TeamWiped>(OnTeamWiped);
        }

        private void OnDowned(PlayerDowned e)
        {
            if (downedLabel != null) downedLabel.text = downedText;
            if (downedPanel != null) downedPanel.SetActive(true);
            StopBleed();
            _bleed = StartCoroutine(BleedBar(e.BleedoutSeconds));
        }

        private void OnRevived(PlayerRevived _)
        {
            StopBleed();
            if (downedPanel != null) downedPanel.SetActive(false);
        }

        private void OnEliminated(PlayerEliminated _)
        {
            StopBleed();
            SetFill(bleedFill, 0f);
            if (downedLabel != null) downedLabel.text = eliminatedText;
            if (downedPanel != null) downedPanel.SetActive(true);
        }

        private void OnTeamWiped(TeamWiped _)
        {
            StopBleed();
            if (downedPanel != null) downedPanel.SetActive(false);
            if (revivePanel != null) revivePanel.SetActive(false);
        }

        private IEnumerator BleedBar(float seconds)
        {
            float t = 0f;
            seconds = Mathf.Max(0.01f, seconds);
            while (t < seconds)
            {
                t += Time.deltaTime;
                SetFill(bleedFill, 1f - Mathf.Clamp01(t / seconds));
                yield return null;
            }
            SetFill(bleedFill, 0f);
            _bleed = null;
        }

        private void StopBleed()
        {
            if (_bleed != null) { StopCoroutine(_bleed); _bleed = null; }
            SetFill(bleedFill, 1f);
        }

        private void OnReviveProgress(ReviveProgress e)
        {
            if (revivePanel != null) revivePanel.SetActive(e.Show);
            if (!e.Show) return;
            if (reviveLabel != null) reviveLabel.text = e.Progress01 > 0f ? revivingText : reviveHint;
            SetFill(reviveFill, e.Progress01);
        }

        private static void SetFill(Transform fill, float v)
        {
            if (fill == null) return;
            Vector3 s = fill.localScale;
            s.x = Mathf.Clamp01(v);
            fill.localScale = s;
        }
    }
}
