using System;
using System.Collections.Generic;
using Gameplay.Controller;
using Gameplay.Items.Scripts.ItemData;
using Gameplay.Items.Scripts.ItemModules;
using MyPrint;
using UnityEngine;

namespace Gameplay.Items.Scripts.PlayerItemGestion
{
    public class ItemCore : MonoBehaviour
    {
        private List<ILeftClick> _leftClicks;
        private List<IRightClick> _rightClicks;
        private List<ICondition> _conditions;
        
        /*
           private List<IPassif> _passifs = new();
       */

        private SO_Item _itemData;
        
        public ItemInstance Instance { get; private set; }
        
        public void SetInstance(ItemInstance instance)
        {
            Instance = instance;

            foreach (var m in Instance.AllModules())
                m?.Initialize(this);   // juste rebind, PAS de reset
        }
        
        public void SetNewItem(SO_Item newItem)
        {
            _itemData = newItem;
            
            _leftClicks = new List<ILeftClick>(_itemData.leftClicksActions);
            _rightClicks = new List<IRightClick>(_itemData.rightClicksActions);
            _conditions = new List<ICondition>(_itemData.conditions);

            foreach (ILeftClick leftClick in _leftClicks)
                leftClick?.Initialize(this);
            
            foreach (IRightClick rightClick in _rightClicks)
                rightClick?.Initialize(this);
            
            foreach (ICondition condition in _conditions)
                condition?.Initialize(this);
        }

        public void SubscribeToInput(PlayerInput input)
        {
            //Left Click Action
            input.OnStartLeftInput += PerformLeftClick;
            input.OnEndLeftInput += PerformLeftRelease;
            
            //Right Click Action
            input.OnStartRightInput += PerformRightClick;
            input.OnEndRightInput += PerformRightRelease;
        }

        public void UnsubscribeFromInput(PlayerInput input)
        {
            //Left Click Action
            input.OnStartLeftInput -= PerformLeftClick;
            input.OnEndLeftInput -= PerformLeftRelease;
            
            //Right Click Action
            input.OnStartRightInput -= PerformRightClick;
            input.OnEndRightInput -= PerformRightRelease;
        }
        
        #region Perform Input Action
        
        void PerformLeftClick()
        {
            if (!CanInput(ItemInput.Left)) 
                return;
            
            foreach (ILeftClick l in Instance.LeftClicks) 
                l?.StartLeftClick();
        }
                
        void PerformLeftRelease()
        {
            if (!CanInput(ItemInput.Left)) return;
            
            foreach (ILeftClick l in Instance.LeftClicks)
                l?.EndLeftClick();
            
            foreach (ICondition condition in Instance.Conditions)
                condition?.UseItem();
        }
        
        void PerformRightClick()
        {
            if (!CanInput(ItemInput.Right)) 
                return;
            
            foreach (IRightClick r in Instance.RightClicks) 
                r?.StartRightClick();
        }
        

        void PerformRightRelease()
        {
            if (!CanInput(ItemInput.Right)) 
                return;
            
            foreach (IRightClick r in Instance.RightClicks)
                r?.EndRightClick();
        }
        
        #endregion

        bool CanInput(ItemInput type)
        {
            foreach (ICondition c in Instance.Conditions)
                if (!c.CheckCondition(type)) 
                    return false;
            return true;
        }
    }
}