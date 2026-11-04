using System.Collections.Generic;
using Gameplay.Controller;
using MyPrint;
using TMPro;
using UnityEngine;
using UnityEngine.PlayerLoop;
using UnityEngine.UI;

namespace Gameplay.Items
{
    public class OtherInventoryUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text _nameLabel;
        [SerializeField] private Transform _slotsRoot;
        [SerializeField] private ItemSlotUI _slotPrefab;
        
        [Header("Icon")]
        [SerializeField] private Image _iconPlayer;
        [SerializeField] private Sprite[] _playerIconList;
        [SerializeField] private Sprite _playerKOIconList;
        
        private PlayerInventory _inventory;
        private readonly List<Image> _icons = new();

        private PlayerController _controller; 
        
        public void Initialize(PlayerInventory inventory, PlayerController controller)
        {
            _inventory = inventory;
            _controller = controller;

            BuildSlots(inventory.SlotCount);
    
            _controller.OnPersonalisationChanged += RefreshIdentity;
            _controller.Health.OnChangedDown += UpdateIcon;
            _inventory.OnSlotsChanged += UpdateUI;

            RefreshIdentity();
            UpdateUI();
        }

        private void RefreshIdentity()
        {
            SetName();
            UpdateIcon(_controller.IsDown);
        }

        private void UpdateIcon(bool isKO)
        {
            _iconPlayer.sprite = isKO ? _playerKOIconList : _playerIconList[_controller.PlayerColorId];
        }

        private void BuildSlots(int count)
        {
            for (int i = 0; i < count; i++)
            {
                ItemSlotUI bg = Instantiate(_slotPrefab, _slotsRoot);
                bg.transform.localScale /= 2;
                Image icon = bg.GetComponent<ItemSlotUI>().Icon;

                _icons.Add(icon);
            }
        }

        private void SetName()
        {
            if (_nameLabel != null && _controller != null)
            {
                _nameLabel.text = _controller.PlayerName;
            }
        }

        
        private void UpdateUI()
        {
            if (_inventory == null) return;
            
            for (int i = 0; i < _icons.Count; i++)
            {
                int id = _inventory.GetSlotItemId(i);
                bool hasItem = id != -1;

                _icons[i].enabled = hasItem;
                if (hasItem)
                    _icons[i].sprite = _inventory.DataBase.GetItemData(id).Icon;
            }
        }

        private void OnDestroy()
        {
            _inventory.OnSlotsChanged -= UpdateUI;
            _controller.OnPersonalisationChanged -= SetName;
        }
    }
}