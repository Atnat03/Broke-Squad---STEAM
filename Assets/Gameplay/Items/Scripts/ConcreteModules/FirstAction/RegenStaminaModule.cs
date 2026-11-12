using System;
using UnityEngine;

namespace Gameplay.Items
{
    [Serializable]
    public class RegenStaminaModule : ActionModule, IFirstAction
    {
        [SerializeField] public float staminaGainAmount;
        public ICondition Condition => ConditionParent;
        public void StartFirstAction()
        {
            Context.Inventory.Controller.Stamina.GainStamina(staminaGainAmount);
            
            ConsumeCondition(new OnModuleDoAction_EVENT());
        }

        public void StopFirstAction()
        {
            
        }

        public void OnNotInHand() { }
        public void OnThrow()
        {
            
        }
    }
}