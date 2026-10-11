using System;
using UnityEngine;

namespace Gameplay.Items
{
    [Serializable]
    public abstract class ActionModule : ItemModule
    {
        [SerializeReference, SerializeReferencePicker] private ICondition _condition;

        public ICondition ConditionParent => _condition;

        protected override void OnCloned() => _condition = _condition?.Clone() as ICondition;

        protected bool CanUse() => _condition == null || _condition.CheckCondition();

        protected void ConsumeCondition(OnModuleDoAction_EVENT data)
        {
            if (_condition is IServerAction)
                Context.SendToServer(_condition, data);
        }
    }
}