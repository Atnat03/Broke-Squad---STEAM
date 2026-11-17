using System;
using MyPrint;
using UnityEngine;

namespace Gameplay.Items
{
    public enum AnimationType { Trigger, Bool, Float }
    
    [Serializable]
    public class AnimationModule : ActionModule, IFirstAction
    {
        public override string Emoji => "🎞️";

        private string _keyEvent = "ANIMATION_ITEM_EVENT";
        
        
        [Header("Aniation Type")]
        [SerializeField] private AnimationType _animationType = AnimationType.Trigger;

        [Header("Animation Setting")] 
        [SerializeField] private string _animationName = "";
        [SerializeField] private bool _boolAnimation = true;
        [SerializeField] private float _floatAnimation = 0;
        
        public ICondition Condition => ConditionParent;
        public void StartFirstAction()
        {
            if (!CanUse()) return;
            
            OnModuleDoAction_EVENT e = new OnModuleDoAction_EVENT
            {
                KeyEvent = _keyEvent,
                ValueS = _animationName,
                ValueI = -1
            };

            switch (_animationType)
            {
                case AnimationType.Trigger:
                    e.ValueI = (int)AnimationType.Trigger;
                    break;
                case AnimationType.Bool:
                    e.ValueI = (int)AnimationType.Bool;
                    e.ValueB = _boolAnimation;
                    break;
                case AnimationType.Float:
                    e.ValueI = (int)AnimationType.Float;
                    e.ValueF = _floatAnimation;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            
            ABPrint.Print("I : " + e.ValueI);
            
            if(e.ValueI != -1)
                Context.SendEventToEveryone(e);
            
            ConsumeCondition(new OnModuleDoAction_EVENT());
        }

        public void StopFirstAction() { }
        public void OnNotInHand() { }
        public void OnThrow() { }
    }
}