using System;
using System.Collections;
using UnityEngine;

namespace Gameplay.Items
{
    [Serializable]
    public class NewStaminaModule : ActionModule, IFirstAction
    {
        [SerializeField] float newRegenParSecond;
        [SerializeField] float effectDuration;
        [SerializeField] float regenDelay;
        
        private Coroutine _regenCoroutine;
        public ICondition Condition => ConditionParent;
        
        public void StartFirstAction()
        {
            if(_regenCoroutine != null) Context.StopCoroutine(_regenCoroutine);
            _regenCoroutine = Context.StartCoroutine(OverrideStaminaRegenTimer(newRegenParSecond, effectDuration));
            ConsumeCondition(new OnModuleDoAction_EVENT());
        }

        public void StopFirstAction()
        {
            
        }

        public void OnNotInHand() { }
        public void OnThrow()
        {
            
        }

        IEnumerator OverrideStaminaRegenTimer(float newRegen, float duration)
        {
            Context.Inventory.Controller.Stamina.OverrideStaminaRegenAndDelay(newRegen, regenDelay);
            yield return new WaitForSeconds(duration);
            Context.Inventory.Controller.Stamina.OverrideStaminaRegenAndDelay(-1, -1);
        }
    }
}