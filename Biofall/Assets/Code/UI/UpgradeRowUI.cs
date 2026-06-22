using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Biofall.Core;

namespace Biofall.UI
{
    public sealed class UpgradeRowUI : MonoBehaviour
    {
        private TMP_Text _name, _level, _cost, _desc;
        private Button _buy;
        private UpgradeData _data;
        private Action<UpgradeData> _onBuy;
        private bool _wired;

        private void EnsureRefs()
        {
            if (_wired) return;
            _wired = true;
            _name = FindText("Name");
            _level = FindText("Level");
            _cost = FindText("Cost");
            _desc = FindText("Desc");
            var b = transform.Find("BuyButton");
            if (b != null) _buy = b.GetComponent<Button>();
        }

        private TMP_Text FindText(string child)
        {
            var t = transform.Find(child);
            return t != null ? t.GetComponent<TMP_Text>() : null;
        }

        public void Bind(UpgradeData data, Action<UpgradeData> onBuy)
        {
            EnsureRefs();
            _data = data;
            _onBuy = onBuy;
            if (_buy != null)
            {
                _buy.onClick.RemoveAllListeners();
                _buy.onClick.AddListener(() => _onBuy?.Invoke(_data));
            }
            Refresh();
        }

        public void Refresh()
        {
            EnsureRefs();
            if (_data == null) return;

            int level = PlayerProgression.GetLevel(_data);
            int cost = _data.CostForNext(level);
            bool maxed = cost < 0;

            if (_name != null) _name.text = _data.displayName;
            if (_desc != null) _desc.text = _data.description;
            if (_level != null) _level.text = "LV " + level + " / " + _data.MaxLevel;
            if (_cost != null) _cost.text = maxed ? "MAX" : cost + " BS";
            if (_buy != null) _buy.interactable = !maxed && PlayerProgression.CanPurchase(_data);
        }
    }
}
