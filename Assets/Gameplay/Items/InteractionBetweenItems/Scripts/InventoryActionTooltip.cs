using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Gameplay.Items
{
    public class InventoryActionTooltip : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private Image _icon;
        [SerializeField] private TextMeshProUGUI _text;

        public void Setup(ActionInInventory_SO inventoryAction)
        {
            //_icon.sprite = inventoryAction.Icon;
            _text.text = inventoryAction.Tooltip;
        }
    }
}