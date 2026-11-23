using Gameplay.LD;
using System;
using UnityEngine;

namespace Gameplay.Items
{
    [Serializable]
    public class BreakThingsModule : ActionModule, IFirstAction
    {
        public override string Emoji => "💥";

        public float Force => _force;
        
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
                    Position = Context.Core.transform.position,
                    Volume = _volume,
                    SoundName = _stringName
                });
                
                ConsumeCondition(new OnModuleDoAction_EVENT());
            }
        }

        public void StopFirstAction()
        { }

        public void OnNotInHand(){ }
        public void OnThrow()
        {
            
        }
    }
}