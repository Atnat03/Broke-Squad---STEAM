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
        
        void PerformRightClick()
        {
            if (!CanInput(ItemInput.Right)) return;
            
            foreach (IRightClick rightClick in _rightClicks)
            {
                rightClick?.StartRightClick();
            }
        }

        void PerformRightRelease()
        {
            if (!CanInput(ItemInput.Right)) return;
            
            foreach (IRightClick rightClick in _rightClicks)
            {
                rightClick?.EndRightClick();
            }
        }
        
        void PerformLeftClick()
        {
            if (!CanInput(ItemInput.Left)) return;
            
            foreach (ILeftClick leftClick in _leftClicks)
            {
                leftClick?.StartLeftClick();
            }
            
            foreach (ICondition condition in _conditions)
                condition?.UseItem();
        }

        void PerformLeftRelease()
        {
            if (!CanInput(ItemInput.Left)) return;
            
            foreach (ILeftClick leftClick in _leftClicks)
            {
                leftClick?.EndLeftClick();
            }
        }
        
        #endregion

        bool CanInput(ItemInput type)
        {
            foreach (ICondition condition in _conditions)
            {
                if (!condition.CheckCondition(type))
                {
                    return false;
                }
            }
            
            return true;
        }
    }
}