using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Biofall.Net;

namespace Biofall.UI
{
    public sealed class CoopSquadHUD : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [Tooltip("Row root objects (shown/hidden per connected player). Up to 4.")]
        [SerializeField] private GameObject[] rows;
        [SerializeField] private TMP_Text[] nameLabels;
        [Tooltip("HP fill bars — scaled on X (left-pivoted), like the other bars.")]
        [SerializeField] private Transform[] hpFills;
        [SerializeField] private TMP_Text[] statusLabels;

        [Header("Status colours")]
        [SerializeField] private Color aliveColor = new(0.55f, 0.9f, 0.55f, 1f);
        [SerializeField] private Color downedColor = new(0.95f, 0.75f, 0.3f, 1f);
        [SerializeField] private Color deadColor = new(0.7f, 0.3f, 0.3f, 1f);

        private static readonly List<CoopPlayerLife> _sorted = new(4);

        private void Update()
        {
            bool coop = NetSession.InCoop && CoopPlayerLife.All.Count > 0;
            if (panel != null) panel.SetActive(coop);
            if (!coop || rows == null) return;

            _sorted.Clear();
            _sorted.AddRange(CoopPlayerLife.All);
            _sorted.Sort((a, b) => a.OwnerClientId.CompareTo(b.OwnerClientId));

            for (int i = 0; i < rows.Length; i++)
            {
                bool has = i < _sorted.Count && _sorted[i] != null;
                if (rows[i] != null) rows[i].SetActive(has);
                if (!has) continue;

                var life = _sorted[i];

                if (nameLabels != null && i < nameLabels.Length && nameLabels[i] != null)
                    nameLabels[i].text = "P" + (i + 1) + (life.IsOwner ? " (YOU)" : "");

                if (hpFills != null && i < hpFills.Length && hpFills[i] != null)
                {
                    Vector3 s = hpFills[i].localScale;
                    s.x = life.State == LifeState.Alive ? Mathf.Clamp01(life.Health01) : 0f;
                    hpFills[i].localScale = s;
                }

                if (statusLabels != null && i < statusLabels.Length && statusLabels[i] != null)
                {
                    switch (life.State)
                    {
                        case LifeState.Alive:  statusLabels[i].text = "ALIVE"; statusLabels[i].color = aliveColor; break;
                        case LifeState.Downed: statusLabels[i].text = "DOWN";  statusLabels[i].color = downedColor; break;
                        default:               statusLabels[i].text = "DEAD";  statusLabels[i].color = deadColor; break;
                    }
                }
            }
        }
    }
}
