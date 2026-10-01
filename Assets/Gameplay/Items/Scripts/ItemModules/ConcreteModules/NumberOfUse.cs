using System;
using UnityEngine;

namespace Gameplay.Items.Scripts.ItemModules.ConcreteModules
{
    [Serializable]
    public class NumberOfUse : ItemModule, ICondition
    {
        [SerializeField] private int _maxUse = 3;
        [SerializeField] private ItemInput _inputType;
        [SerializeField] private bool _destroyWhenUsed = false;
        private int _currentUse;

        public ItemInput InputType => _inputType;
        public int MaxUse => _maxUse;
        public int CurrentUse => _currentUse;
        public bool ShouldDestroy => _destroyWhenUsed && _currentUse <= 0;

        protected override void SetModule() => _currentUse = _maxUse;

        public bool CheckCondition() => Context.Inventory.Uses.x > 0;

        public void UseItem() => Context.Inventory.RequestUseItem();

        public bool Consume()
        {
            if (_currentUse <= 0) return false;
            _currentUse--;
            return true;
        }

        protected override void OnBind()
        {
            Context.Inventory.EnableUseText(true);
            Vector2Int u = Context.Inventory.Uses;
            if (u.y > 0) Context.Inventory.UpdateTextUse(u.x + "/" + u.y);
        }

        public void ThrowItem() { }
    }
}