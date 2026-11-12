using System.Resources;
using Bus;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace Gameplay.Items
{
    public class PlayerInventoryView : MonoBusListener
    {
        [Header("References")]
        [SerializeField] private PlayerSetUpBridge _bridge;
        
        [Header("UI")]
        [SerializeField] private ItemSlotUI _slotPrefabUI;
        [SerializeField] private Transform _parentInventory;
        [SerializeField] private Sprite _emptyIcon;
        private ItemSlotUI[] _slotsUIList;
        private bool _uiDirty;
        
        [Header("Tooltip")]
        [SerializeField] private GameObject _tooltipParent;
        [SerializeField] private TextMeshProUGUI _tooltipText;
        
        [Header("SFX")]
        [SerializeField, SoundName] private string _pickUpSound;
        [SerializeField, SoundName] private string _dropSound;

        void Start()
        {
            if(_bridge)
            {
                _bridge.PlayerInventory.OnSlotSelectedChanged += SelectedSlotChanged;
                _bridge.PlayerInventory.OnUpdateSlots += UpdateSlots;
                _bridge.PlayerInventory.OnPickupItem += PickupItem;
                _bridge.PlayerInventory.OnDropItem += DropItem;
                _bridge.PlayerInventory.OnTooltipChange += UpdateTooltip;
                
                SetUpInventory();
            }
            
            _tooltipParent.SetActive(false);
        }
        
        void OnDisable()
        {
            if (_bridge)
            {
                _bridge.PlayerInventory.OnSlotSelectedChanged -= SelectedSlotChanged;
                _bridge.PlayerInventory.OnUpdateSlots -= UpdateSlots;
                _bridge.PlayerInventory.OnPickupItem -= PickupItem;
                _bridge.PlayerInventory.OnDropItem -= DropItem;
                _bridge.PlayerInventory.OnTooltipChange -= UpdateTooltip;
            }
        }

        private void SelectedSlotChanged(bool enable)
        {
            _uiDirty = enable;
        }

        private void SetUpInventory()
        {
            _slotsUIList = new ItemSlotUI[_bridge.PlayerInventory.ItemCount];
            
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
                SO_Item data = _bridge.PlayerInventory.DataBase.GetItemData(id);
                if (data && data.Icon)
                    icon = data.Icon;
            }
            
            _slotsUIList[selected].SetIcon(icon);
        }

        private void PickupItem()
        { }
        
        private void DropItem()
        { }
        
        private void UpdateTooltip(bool isVisible, string txt)
        {
            _tooltipParent.SetActive(isVisible);
            _tooltipText.text = txt;
        }
    }
}