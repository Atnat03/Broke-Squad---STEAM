using System;
using System.Collections;
using System.Collections.Generic;
using Bus;
using Gameplay.Controller;
using Gameplay.Items.Scripts.ItemData;
using Gameplay.Items.Scripts.ItemModules;
using MyPrint;
using UnityEngine;

namespace Gameplay.Items.Scripts.PlayerItemGestion
{
    public class ItemCore : MonoBusListener
    {
        private List<ILeftClick> _leftClicks;
        private List<IRightClick> _rightClicks;
        private List<ICondition> _conditions;
        
        /*
           private List<IPassif> _passifs = new();
       */

        private SO_Item _itemData;
        
        public ItemInstance Instance { get; private set; }
        
        public void SetInstance(ItemInstance instance, PlayerInventory inventory)
        {
            Instance = instance;

            ItemContext context = new ItemContext(this, inventory, inventory.PlayerCamera);
            
            foreach (var module in Instance.AllModules())
                module.Initialize(context);
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
            if (!CanInput()) 
                return;
            
            foreach (ILeftClick l in Instance.LeftClicks) 
                l?.StartLeftClick();
        }
                
        void PerformLeftRelease()
        {
            if (!CanInput()) return;
            
            foreach (ILeftClick l in Instance.LeftClicks)
                l?.EndLeftClick();
        }
        
        void PerformRightClick()
        {
            foreach (IRightClick r in Instance.RightClicks) 
                r?.StartRightClick();
        }
        

        void PerformRightRelease()
        {
            foreach (IRightClick r in Instance.RightClicks)
                r?.EndRightClick();
        }

        public void UseCondition()
        {
            foreach (ICondition cond in Instance.Conditions)
                cond?.UseItem();
        }
        
        #endregion

        bool CanInput()
        {
            foreach (ICondition c in Instance.Conditions)
                if (!c.CheckCondition())
                    return false;
            return true;
        }
        
        public Coroutine StartModuleCoroutine(IEnumerator routine)
        {
            return StartCoroutine(routine);
        }

        public void ThrowItem()
        {
            foreach (ICondition c in Instance.Conditions)
            {
                c.ThrowItem();
            }
        }
    }
}