using System;
using MyPrint;
using UnityEngine;

namespace Gameplay.Items
{
    public class AnimationModule : ActionModule, IFirstAction
    {
        enum AnimationType { Trigger, Bool, Float }
        
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
            
            Animator animator = Context.Core.GetComponentInChildren<Animator>();
            
            if (animator)
            {
                switch (_animationType)
                {
                    case AnimationType.Trigger:
                        animator.SetTrigger(_animationName);
                        break;
                    case AnimationType.Bool:
                        animator.SetBool(_animationName, _boolAnimation);
                        break;
                    case AnimationType.Float:
                        animator.SetFloat(_animationName, _floatAnimation);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            
            ConsumeCondition(new OnModuleDoAction_EVENT());
        }

        public void StopFirstAction() { }
        public void OnNotInHand() { }
        public void OnThrow() { }
    }
}