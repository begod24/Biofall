using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Biofall.Core;

namespace Biofall.UI
{
    public sealed class UpgradeShopUI : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private TMP_Text bankedText;
        [Tooltip("Container with a VerticalLayoutGroup; rows are added under it.")]
        [SerializeField] private Transform rowsParent;
        [Tooltip("An inactive template row with named children (Name/Level/Cost/Desc/BuyButton).")]
        [SerializeField] private GameObject rowTemplate;
        [SerializeField] private Button openButton;
        [SerializeField] private Button closeButton;

        private readonly List<UpgradeRowUI> _rows = new();
        private bool _built;

        private void Awake()
        {
            if (openButton != null) openButton.onClick.AddListener(Open);
            if (closeButton != null) closeButton.onClick.AddListener(Close);
            if (panel != null) panel.SetActive(false);
        }

        private void OnEnable() => PlayerProgression.Changed += OnChanged;
        private void OnDisable() => PlayerProgression.Changed -= OnChanged;

        public void Open()
        {
            Build();
            RefreshAll();
            if (panel != null) panel.SetActive(true);
        }

        public void Close()
        {
            if (panel != null) panel.SetActive(false);
        }

        private void Build()
        {
            if (_built) return;
            _built = true;

            var catalog = PlayerProgression.Catalog;
            if (catalog == null || catalog.Upgrades == null || rowTemplate == null || rowsParent == null) return;

            rowTemplate.SetActive(false);
            foreach (var data in catalog.Upgrades)
            {
                if (data == null) continue;
                var go = Instantiate(rowTemplate, rowsParent);
                go.name = "Row_" + data.id;
                go.SetActive(true);
                var row = go.GetComponent<UpgradeRowUI>();
                if (row == null) row = go.AddComponent<UpgradeRowUI>();
                row.Bind(data, Buy);
                _rows.Add(row);
            }
        }

        private void Buy(UpgradeData data)
        {
            PlayerProgression.TryPurchase(data);
        }

        private void OnChanged() => RefreshAll();

        private void RefreshAll()
        {
            if (bankedText != null) bankedText.text = PlayerProgression.BankedSamples + " BS";
            for (int i = 0; i < _rows.Count; i++)
                if (_rows[i] != null) _rows[i].Refresh();
        }
    }
}
