using System;
using System.Collections.Generic;
using Bus;
using Gameplay.Controller;
using Gameplay.Items.Scripts.ItemData;
using Gameplay.Items.Scripts.ItemModules;
using Gameplay.Items.Scripts.ItemModules.ConcreteModules;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

namespace Gameplay.Items.Scripts.PlayerItemGestion
{
    public class PlayerInventory : MonoBusListener
    {
        [SerializeField] private PlayerInput _input;
        
        [SerializeField] private Transform _parent;
        [SerializeField] private ItemPickup _item;
        
        private ItemCore _currentItem;

        [ContextMenu("Equip")]
        public void TestGrab()
        {
            GrabItem(_item);
        }

        [ContextMenu("UnEquip")]
        public void TestDrop()
        {
            DropItem(transform.position + Vector3.forward*0.5f);
        }

        public void GrabItem(ItemPickup pickup)
        {
            if (HasItemInHand()) return;

            EquipInstance(pickup.Instance);
            Destroy(pickup.gameObject);
        }

        public void DropItem(Vector3 position)
        {
            if (!HasItemInHand()) return;

            ItemInstance instance = _currentItem.Instance;
            SO_Item data = instance.Data;

            DestroyItemInHand();

            GameObject world = new GameObject(data.itemName);
            world.transform.position = position;
            Instantiate(data.visualPrefab, world.transform);
            world.AddComponent<ItemPickup>().Setup(instance);
        }

        private void EquipInstance(ItemInstance instance)
        {
            GameObject go = new GameObject(instance.Data.itemName);
            go.transform.SetParent(_parent, false);

            ItemCore core = go.AddComponent<ItemCore>();
            Instantiate(instance.Data.visualPrefab, core.transform);
            core.SetInstance(instance);

            _currentItem = core;
            _currentItem.SubscribeToInput(_input);
        }
        
        private bool HasItemInHand()
        {
            return _currentItem != null;
        }
        
        private void DestroyItemInHand()
        {
            if (_currentItem == null)
                return;

            _currentItem.UnsubscribeFromInput(_input);

            Destroy(_currentItem.gameObject);
            _currentItem = null;
        }
    }
}