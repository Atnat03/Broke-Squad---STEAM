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

        private float _current;

        public override void ResetState()
        {
            _current = _batteryCapacity;
            base.ResetState();
        }

        public bool CheckCondition() => _current >= _useCost;

        public void UseItem()
        {
            _current = Mathf.Max(0f, _current - _useCost);
            PushToUI();
        }

        public void AddEnergy(float amount)
        {
            _current = Mathf.Min(_batteryCapacity, _current + amount);
            PushToUI();
        }

        protected override void OnBind()
        {
            Context.Inventory.EnableElectricInfo(true);
            PushToUI();
        }

        public void ThrowItem() { }

        private void PushToUI()
        {
            if (Context?.Inventory != null && Context.Inventory.IsServer)
                Context.Inventory.SetElectricPercent(_current / _batteryCapacity * 100f);
        }
    }
}