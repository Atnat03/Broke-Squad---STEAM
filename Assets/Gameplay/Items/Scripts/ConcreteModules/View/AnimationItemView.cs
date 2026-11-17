using System;
using Bus;
using MyPrint;
using UnityEngine;

namespace Gameplay.Items
{
    public class AnimationItemView : MonoBusListener
    {
        private string _keyEvent = "ANIMATION_ITEM_EVENT";
        
        [SerializeField] Animator animator;
        
        private void OnEnable()
        {
            ListenToEvent<OnModuleDoAction_EVENT>(Animated);
        }

        private void Animated(OnModuleDoAction_EVENT data)
        {
            if(!animator)
            {
                ABPrint.Print("No animator on " + transform.name, ABColor.Red);
                return;
            }
            
            if (data.KeyEvent != _keyEvent) return;
            
            switch ((AnimationType)data.ValueI)
            {
                case AnimationType.Trigger:
                    animator.SetTrigger(data.ValueS.Value);
                    break;
                case AnimationType.Bool:
                    animator.SetBool(data.ValueS.Value, data.ValueB);
                    break;
                case AnimationType.Float:
                    animator.SetFloat(data.ValueS.Value, data.ValueF);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}