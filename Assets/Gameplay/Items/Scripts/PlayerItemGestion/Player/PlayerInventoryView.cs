using Bus;
using Unity.Netcode;
using UnityEngine;

namespace Gameplay.Items
{
    public class PlayerInventoryView : MonoBusListener
    {
        [Header("References")]
        [SerializeField] private PlayerInventory _inventory;
        
        [Header("UI")]
        [SerializeField] private ItemSlotUI _slotPrefabUI;
        [SerializeField] private Transform _parentInventory;
        [SerializeField] private Sprite _emptyIcon;
        private ItemSlotUI[] _slotsUIList;
        private bool _uiDirty;
        
        [Header("SFX")]
        [SerializeField, SoundName] private string _pickUpSound;
        [SerializeField, SoundName] private string _dropSound;

        void OnEnable()
        {
            _inventory.OnSetupInventory += SetUpInventory;
            _inventory.OnSlotSelectedChanged += SelectedSlotChanged;
            _inventory.OnUpdateSlots += UpdateSlots;
            _inventory.OnPickupItem += PickupItem;
            _inventory.OnDropItem += DropItem;
        }
        
        void OnDisable()
        {
            _inventory.OnSetupInventory -= SetUpInventory;
            _inventory.OnSlotSelectedChanged -= SelectedSlotChanged;
            _inventory.OnUpdateSlots -= UpdateSlots;
            _inventory.OnPickupItem -= PickupItem;
            _inventory.OnDropItem -= DropItem;
        }

        private void SelectedSlotChanged(bool enable)
        {
            _uiDirty = enable;
        }

        private void SetUpInventory(int numberSlots)
        {
            _slotsUIList = new ItemSlotUI[numberSlots];
            
            for (int i = 0; i < _slotsUIList.Length; i++)
            {
                _slotsUIList[i] = Instantiate(_slotPrefabUI, _parentInventory);
                _slotsUIList[i].SetIcon(_emptyIcon);
            }
                
            _uiDirty = true;
        }
        
        private void UpdateSlots(int selected, int id)
        {
            if (selected < 0 || selected >= _slotsUIList.Length) return;

            for (int i = 0; i < _slotsUIList.Length; i++)
                _slotsUIList[i].SetSelectedImage(i == selected);

            Sprite icon = _emptyIcon;

            if (id != -1)
            {
                SO_Item data = _inventory.DataBase.GetItemData(id);
                if (data != null && data.Icon != null)
                    icon = data.Icon;
                else
                    Debug.LogWarning($"L'item {id} n'a pas d'icône assignée.", this);
            }
            
            _slotsUIList[selected].SetIcon(icon);
        }

        private void PickupItem()
        { }
        
        private void DropItem()
        { }
    }
}