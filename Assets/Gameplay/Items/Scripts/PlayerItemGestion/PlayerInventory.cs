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
        
        [SerializeField] private SO_ItemList _possibleItems;
        [SerializeField] private Transform _parent;
        [SerializeField] private GameObject _visualPrefab;
        
        private ItemCore _currentItem;

        private void Start()
        {
            SO_Item item = ScriptableObject.CreateInstance<SO_Item>();
            item.id = 0;
            item.itemName = "test";
            item.visualPrefab = _visualPrefab;
            item.rightClicksActions = new List<IRightClick> { new ThrowModule() };
            item.leftClicksActions = new List<ILeftClick> { new BreakThings() };
            item.conditions = new List<ICondition> { new NumberOfUse() };
            
            CreateNewItem(item);
        }

        public void GrabItem()
        {
           
        }

        public void DropItem()
        {
            
        }

        public bool HasItemInHand()
        {
            return _currentItem != null;   
        }

        private void CreateNewItem(SO_Item item)
        {
            GameObject newItem = new GameObject(item.itemName);
            newItem.transform.SetParent(_parent);
            
            ItemCore itemCore = newItem.AddComponent<ItemCore>();
            Instantiate(item.visualPrefab, itemCore.transform);
            
            itemCore.SetNewItem(item);
            
            _currentItem = itemCore;
            
            SetInputActionToItem();
        }
        
        public void DestroyItemInHand()
        {
            if (_currentItem == null)
                return;

            _currentItem.UnsubscribeFromInput(_input);

            Destroy(_currentItem.gameObject);
            _currentItem = null;
        }

        private void SetInputActionToItem()
        {
            _currentItem.SubscribeToInput(_input);
        }
    }
}