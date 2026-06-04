using UnityEngine;
using TMPro;

namespace Biofall.UI
{
    public class AmmoUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text label;

        public void SetText(string value)
        {
            if (label != null) label.text = value;
        }
    }
}
