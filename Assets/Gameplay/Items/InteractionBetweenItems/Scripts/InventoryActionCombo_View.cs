using UnityEngine;

namespace Gameplay.Items
{
    public class InventoryActionCombo_View : MonoBehaviour
    {
        [SerializeField] private PlayerSetUpBridge _bridge;
        
        [Header("UI")]
        [SerializeField] private InventoryActionTooltip _prefabTooltip;

        private InventoryActionCombo _inventoryActionCombo;
        
        private void Start()
        {
            _bridge.PlayerInventory.OnCanComboUpdate += UpdateTooltip;
        }

        private void OnDisable()
        {
            _bridge.PlayerInventory.OnCanComboUpdate -= UpdateTooltip;
        }

        private void UpdateTooltip(bool state, ActionInInventory_SO data)
        {
            _prefabTooltip.gameObject.SetActive(state);
            
            if(data != null)
                _prefabTooltip.Setup(data);
        }
    }
}