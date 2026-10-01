using System;
using MyPrint;
using UnityEngine;

namespace Gameplay.Items.Scripts.ItemModules.ConcreteModules
{
    [Serializable]
    public class NumberOfUse : ItemModule, ICondition
    {
        [SerializeField] private int _maxUse = 3;
        [SerializeField] private ItemInput _inputType;
        [SerializeField] private bool _destroyWhenUsed = false;
        private int _currentUse = 0;

        public ItemInput InputType => _inputType;
        
        protected override void SetModule()
        {
            _currentUse = _maxUse;
        }

        public bool CheckCondition()
        {
            return _currentUse > 0;
        }

        public void UseItem()
        {
            _currentUse--;

            if (_destroyWhenUsed && _currentUse == 0)
            {
                Context.Inventory.DestroyItemInHand();
                Context.Inventory.EnableUseText(false);
            }
            
            PushToUI();
        }
        
        protected override void OnBind()
        {
            Context.Inventory.EnableUseText(true);
            PushToUI();
        }
        
        private void PushToUI()
        {
            if (Context?.Inventory != null && Context.Inventory.IsServer)
                Context.Inventory.UpdateTextUse(_currentUse + "/" + _maxUse);
        }

        public void ThrowItem()
        { }
    }
}