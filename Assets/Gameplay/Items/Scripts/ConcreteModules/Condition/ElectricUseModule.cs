using System;
using MyPrint;
using Unity.Collections;
using UnityEngine;

namespace Gameplay.Items
{
    [Serializable]
    public class ElectricUseModule : ItemModule, ICondition, IServerAction, IReplicatedModule
    {
        private float _maxPercentValue = 100;
        [SerializeField] private float _startPercentValue = 100;
        [SerializeField, Tooltip("En %")] private float _electricNeededToUse = 10;
        private float _currentPercentValue;
        private FixedString32Bytes _keyEvent = "ELECTRIC_USE_MODULE";
        
        private bool _isInHand;
        
        protected override void SetModule() => _currentPercentValue = _startPercentValue;
        public bool CheckCondition() => _currentPercentValue-_electricNeededToUse >= 0;

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
            _isInHand = true;
            if (!Context.Inventory.IsServer) return;

            SendUI(true, _currentPercentValue, -1);
        }

        public void DisableItemInHand()
        {
            _isInHand = false;
            if (!Context.Inventory.IsServer) return;

            SendUI(false, 0f, 0);
        }
        
        public bool AddEnergy(float amount)
        {
            if (amount <= 0f || _currentPercentValue >= _maxPercentValue)
                return false;

            _currentPercentValue = Mathf.Min(_currentPercentValue + amount, _maxPercentValue);

            if (_isInHand)
                SendUI(true, _currentPercentValue, 0);

            return true;
        }
        
        private void SendUI(bool visible, float value, int snapFlag)
        {
            Context.SendEventTo(Context.Inventory.OwnerClientId, new OnModuleDoAction_EVENT
            {
                ClientId = Context.Inventory.OwnerClientId,
                KeyEvent = _keyEvent,
                ValueB = visible,
                ValueF = value,
                ValueI = snapFlag
            });
        }

        public void ServerExecute(OnModuleDoAction_EVENT data) => UseItem();
        
        public int GetState() => (int)_currentPercentValue;
        public void SetState(int value) => _currentPercentValue = Mathf.Clamp(value, 0, _maxPercentValue);
    }
}