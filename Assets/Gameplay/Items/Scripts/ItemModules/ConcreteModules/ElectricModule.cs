using System;
using UnityEngine;

namespace Gameplay.Items.Scripts.ItemModules.ConcreteModules
{
    [Serializable]
    public class ElectricModule : ItemModule, ICondition
    {
        public ItemInput InputType => type;
        
        [SerializeField] private ItemInput type;
        [SerializeField] private float _batteryCapacity = 100;
        [SerializeField] private float _useCost = 25;

        private bool _alreadyBind = false;
        
        private float _currentBatteryCapacity;
        
        public bool CheckCondition()
        {
            return _batteryCapacity-_useCost >= 0;
        }

        public void UseItem()
        {
            if (_currentBatteryCapacity <= 0)
                return;
            
            _currentBatteryCapacity -= _useCost;
            
            Context.Inventory.UpdateElectricInfo(_currentBatteryCapacity);
        }

        protected override void OnBind()
        {
            if(!_alreadyBind)
            {
                _currentBatteryCapacity = _batteryCapacity;
                _alreadyBind = true;
            }
            
            Context.Inventory.EnableElectricInfo(true);
            
            Context.Inventory.UpdateElectricInfo(_currentBatteryCapacity);
        }
        
        public void ThrowItem()
        {
            Context.Inventory.EnableElectricInfo(false);
        }
        
        public void AddEnergy(float amount)
        {
            _currentBatteryCapacity += amount;
            
            if(_currentBatteryCapacity > _batteryCapacity)
                _currentBatteryCapacity = _batteryCapacity;
            
            Context.Inventory.UpdateElectricInfo(_currentBatteryCapacity);
        }
        
    }
}