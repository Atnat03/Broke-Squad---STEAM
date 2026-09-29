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
        }

        public void ThrowItem()
        { }
    }
}