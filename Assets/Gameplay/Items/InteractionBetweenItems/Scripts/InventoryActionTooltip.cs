using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Gameplay.Items
{
    public class InventoryActionTooltip : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private GameObject _parentUI;
        [SerializeField] private Image _icon;
        [SerializeField] private TextMeshProUGUI _text;

        void Awake()
        {
            _parentUI.SetActive(false);
        }
        
        public void Setup(ActionInInventory_SO inventoryAction)
        {
            _parentUI.SetActive(!inventoryAction.Tooltip.Equals(string.Empty));
            
            if(_icon != null)
                _icon.sprite = inventoryAction.Icon;
            
            _text.text = inventoryAction.Tooltip;
        }
    }
}