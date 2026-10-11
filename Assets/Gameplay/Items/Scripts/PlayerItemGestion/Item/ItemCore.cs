using System.Collections.Generic;
using Bus;
using Gameplay.Controller;
using UnityEngine;

namespace Gameplay.Items
{
    public class ItemCore : MonoBusListener
    {
        private List<IFirstAction> _firstActionList;
        private List<ISecondAction> _secondActionList;
        private SO_Item _itemData;
        
        public ItemInstance Instance { get; private set; }
        
        public void SetInstance(ItemInstance instance, Camera cam, PlayerInventory inventory)
        {
            Instance = instance;

            ItemContext context = new ItemContext(this, cam, inventory);
            
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
            foreach (IFirstAction a in Instance.FirstActionList) 
                a?.StartFirstAction();
        }
                
        void PerformLeftRelease()
        {
            foreach (IFirstAction a in Instance.FirstActionList)
                a?.StopFirstAction();
        }
        
        void PerformRightClick()
        {
            foreach (ISecondAction r in Instance.SecondActionList) 
                r?.StartSecondAction();
        }
        

        void PerformRightRelease()
        {
            foreach (ISecondAction r in Instance.SecondActionList)
                r?.StopSecondAction();
        }
        
        #endregion
    }
}