using System;
using MyPrint;
using Unity.Collections;
using UnityEngine;

namespace Gameplay.Items
{
    [Serializable]
    public class ElectricUseModule : ItemModule, ICondition, IServerAction, IReplicatedModule
    {
        [SerializeField, Tooltip("En %")] private float _electricNeededToUse = 10;
        private float _currentPercentValue;
        private FixedString32Bytes _keyEvent = "ELECTRIC_USE_MODULE";
        
        protected override void SetModule() => _currentPercentValue = 100;
        public bool CheckCondition() => _currentPercentValue-_electricNeededToUse > 0;

        public void UseItem()
        {
            if (!CheckCondition()) return;
            
            _currentPercentValue -= _electricNeededToUse;
            
            Context.SendEventTo(Context.Inventory.OwnerClientId, new OnModuleDoAction_EVENT
            {
                ClientId = Context.Inventory.OwnerClientId,
                KeyEvent = _keyEvent,
                ValueB = true,
                ValueF = _currentPercentValue,
                ValueI = 0
            });
            
            Context.NotifyStateChanged(this);
        }

        protected override void OnComeInHand()
        {
            if (!Context.Inventory.IsServer) return;
            
            Context.SendEventTo(Context.Inventory.OwnerClientId, new OnModuleDoAction_EVENT
            {
                ClientId = Context.Inventory.OwnerClientId,
                KeyEvent = _keyEvent,
                ValueB = true,
                ValueF = _currentPercentValue,
                ValueI = -1
            });
        }

        public void DisableItemInHand()
        {
            Context.SendEventTo(Context.Inventory.OwnerClientId, new OnModuleDoAction_EVENT
            {
                ClientId = Context.Inventory.OwnerClientId,
                KeyEvent = _keyEvent,
                ValueB = false,
            });
        }
        
        public bool AddEnergy(int amount)
        {
            if (_currentPercentValue >= 100)
                return false;
            
            if (amount >= 0)
            {
                _currentPercentValue += amount;
                
                if (_currentPercentValue > 100) 
                    _currentPercentValue = 100;
                
                return true;
            }

            return false;
        }

        public void ServerExecute(OnModuleDoAction_EVENT data) => UseItem();
        
        public int GetState() => (int)_currentPercentValue;
        public void SetState(int value) => _currentPercentValue = Mathf.Clamp(value, 0, 100);
    }
}