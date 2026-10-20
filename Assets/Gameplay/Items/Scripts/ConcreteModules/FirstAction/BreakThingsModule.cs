using System;
using UnityEngine;

namespace Gameplay.Items
{
    [Serializable]
    public class BreakThingsModule : ActionModule, IFirstAction
    {
        [SerializeField] private float _force = 5;

        [Header("SFX")] 
        [SerializeField, SoundName] private string _stringName;
        [SerializeField, Range(0, 1)] private float _volume = 0.5f;
        
        public ICondition Condition => ConditionParent;

        public void StartFirstAction()
        {
            if (!CanUse()) return;
            
            Transform cam = Context.Camera.transform;

            if (Physics.Raycast(cam.position, cam.forward, out RaycastHit hit, 2f) && hit.transform.TryGetComponent(out IBreakable breakable))
            {
                breakable.Break(_force);
                
                //SFX
                Context.Inventory.InvokeEvent(new PlaySoundEvent
                {
                    position = Context.Core.transform.position,
                    volume = _volume,
                    soundName = _stringName
                });
                
                ConsumeCondition(new OnModuleDoAction_EVENT());
            }
        }

        public void StopFirstAction()
        { }
    }
}