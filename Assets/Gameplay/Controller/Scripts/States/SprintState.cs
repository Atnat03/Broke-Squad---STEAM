using UnityEngine;

namespace Gameplay.Controller.States
{
    public class SprintState : BaseState
    {
        public SprintState(PlayerController playerController) : base(playerController)
        {
            
        }
        
        public override void OnEnter()
        {
            Motor.JumpAllowed = true;
        }

        public override void OnFixedUpdate()
        {
            Motor.TargetSpeed = Profile.sprintSpeed;
            Body.SetTargetHeight(Body.StandHeight);
            Stamina.Drain(Time.fixedDeltaTime);
        }
    }
}