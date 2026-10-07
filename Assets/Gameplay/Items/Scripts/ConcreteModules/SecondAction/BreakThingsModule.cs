using System;
using UnityEngine;

namespace Gameplay.Items
{
    [Serializable]
    public class BreakThingsModule : ActionModule, IFirstAction
    {
        [SerializeField] private float _force = 5;

        public ICondition Condition => ConditionParent;

        public void StartFirstAction()
        {
            if (!CanUse()) return;
            
            Transform cam = Context.Camera.transform;

            if (Physics.Raycast(cam.position, cam.forward, out RaycastHit hit, 2f) && hit.transform.TryGetComponent(out IBreakable breakable))
            {
                breakable.Break(_force);
                ConsumeCondition(new OnModuleDoAction_EVENT());
            }
        }

        public void StopFirstAction()
        { }
    }
}