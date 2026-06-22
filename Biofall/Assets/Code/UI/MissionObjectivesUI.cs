using UnityEngine;
using TMPro;
using Biofall.Core;
using Biofall.Gameplay.Mission1;

namespace Biofall.UI
{
    public sealed class MissionObjectivesUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text mainText;
        [SerializeField] private TMP_Text subText;

        private void Awake()
        {
            if (mainText == null)
            {
                var t = transform.Find("Main");
                if (t != null) mainText = t.GetComponent<TMP_Text>();
            }
            if (subText == null)
            {
                var t = transform.Find("Sub");
                if (t != null) subText = t.GetComponent<TMP_Text>();
            }
        }

        private void OnEnable() => EventBus.Subscribe<MissionPhaseChanged>(OnPhase);
        private void OnDisable() => EventBus.Unsubscribe<MissionPhaseChanged>(OnPhase);

        private void OnPhase(MissionPhaseChanged e)
        {
            switch (e.Phase)
            {
                case MissionPhase.FindGenerator:
                    SetText("OBJECTIVE: TURN ON BEACON",
                            "• Find and turn on the Generator");
                    break;
                case MissionPhase.ActivateBeacon:
                    SetText("OBJECTIVE: TURN ON BEACON",
                            "<color=#6BCB77>✓ Generator powered</color>\n• Activate the Beacon");
                    break;
                case MissionPhase.DefendBeacon:
                    SetText("OBJECTIVE: TURN ON BEACON",
                            "<color=#6BCB77>✓ Generator powered</color>\n• Defend the Beacon while it charges");
                    break;
                case MissionPhase.Extract:
                    SetText("OBJECTIVE: EXTRACTION",
                            "<color=#6BCB77>✓ Beacon online</color>\n• Reach the Extraction point");
                    break;
                case MissionPhase.Completed:
                    SetText("MISSION COMPLETE", "");
                    break;
            }
        }

        private void SetText(string main, string sub)
        {
            if (mainText != null) mainText.text = main;
            if (subText != null) subText.text = sub;
        }
    }
}
