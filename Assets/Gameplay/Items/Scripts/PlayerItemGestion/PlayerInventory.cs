using System;
using System.Collections.Generic;
using Bus;
using Gameplay.Controller;
using Gameplay.Items.Scripts.ItemData;
using Gameplay.Items.Scripts.ItemModules;
using MyPrint;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Gameplay.Items.Scripts.PlayerItemGestion
{
    public class PlayerInventory : MonoBusListener
    {
        public Camera PlayerCamera => _camera;
        public ItemCore CurrentItem => _currentItem;
        
        [SerializeField] private Transform _parent;
        
        [Header("Picking")]
        [SerializeField] private Camera _camera;
        [SerializeField] private float _range;
        [SerializeField] private LayerMask _layerMask;
        
        [Header("UI")]
        [Header("Throw")]
        [SerializeField] private GameObject _throwUI;
        [SerializeField] private Image _throwImage;
        
        [Header("Electic")]
        [SerializeField] private GameObject _electicUI;
        [SerializeField] private TextMeshProUGUI _electicPercent;
        
        private PlayerInput _input;
        private ItemCore _currentItem;
        
        void Awake()
        {
            _input = GetComponent<PlayerInput>();
            
            EnableBar(false);
            EnableElectricInfo(false);
        }

        private void OnEnable()
        {
            _input.OnInteractInput += PickUpItem;
        }

        private void OnDisable()
        {
            _input.OnInteractInput -= PickUpItem;
        }
        
        private void PickUpItem()
        {
            bool hasItem = HasItemInHand();
            
            if (!hasItem)
            {
                if (Physics.Raycast(_camera.transform.position, _camera.transform.forward, out RaycastHit hit, _range ,_layerMask))
                {
                    if (hit.transform.TryGetComponent(out ItemPickup item))
                    {
                        GrabItem(item);
                        
                        InvokeEvent(new OnInteractItemInWorld());
                    }
                }
            }
        }
        
        public void GrabItem(ItemPickup pickup)
        {
            if (HasItemInHand()) return;

            EquipInstance(pickup.Instance);
            Destroy(pickup.gameObject);
        }
        
        private void EquipInstance(ItemInstance instance)
        {
            GameObject go = new GameObject(instance.Data.itemName);

            go.transform.SetParent(_parent, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;

            ItemCore core = go.AddComponent<ItemCore>();

            GameObject visual = Instantiate(instance.Data.visualPrefab, core.transform);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;

            core.SetInstance(instance, this);

            _currentItem = core;
            _currentItem.SubscribeToInput(_input);
        }

        public ItemPickup DropItem(Vector3 position, Quaternion rotation)
        {
            if (!HasItemInHand())
                return null;

            ItemInstance instance = _currentItem.Instance;

            ItemPickup item = Instantiate(instance.Data.pickUpPrefab, position, rotation);
            item.Setup(instance);

            return item;
        }
        
        public bool HasItemInHand()
        {
            return _currentItem != null;
        }
        
        public void DestroyItemInHand()
        {
            if (_currentItem == null)
                return;

            _currentItem.ThrowItem();
            
            _currentItem.UnsubscribeFromInput(_input);

            Destroy(_currentItem.gameObject);
            _currentItem = null;
        }
        
        public void EnableBar(bool state) => _throwUI.SetActive(state);
        
        public void UpdateBar(float t)
        {
            _throwImage.fillAmount = t;
        }
        
        public void EnableElectricInfo(bool state) => _electicUI.SetActive(state);
        
        public void UpdateElectricInfo(float percent)
        {
            _electicPercent.text = (int)percent + " %";
        }
    }
}