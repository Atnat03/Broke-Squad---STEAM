using MyPrint;
using UnityEngine;

namespace Gameplay.Items.Scripts.ItemModules.ConcreteModules
{
    public class NumberOfUse : ItemModule, ICondition
    {
        private readonly int _maxUse = 3;
        private int _currentUse = 0;

        public ItemInput InputType => ItemInput.Left;
        
        protected override void SetModule()
        {
            _currentUse = _maxUse;
        }

        public bool CheckCondition(ItemInput type)
        {
            return _currentUse > 0 && type == InputType;
        }

        public void UseItem()
        {
            _currentUse--;
        }
    }
}