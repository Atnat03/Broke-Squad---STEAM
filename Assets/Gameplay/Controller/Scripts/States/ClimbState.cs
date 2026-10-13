using UnityEngine;

namespace Gameplay.Controller.States
{
    public class ClimbState : BaseState
    {
        public ClimbState(PlayerController playerController) : base(playerController) { }

        public override void OnEnter()
        {
            Motor.StopMoving();
            Motor.IsClimbing = true;
        }

        public override void OnUpdate()
        {
            Motor.ClimbVelocity = playerController.MoveInput.y * Profile.climbSpeed;
        }

        public override void OnExit()
        {
            Motor.IsClimbing = false;
            Motor.ClimbVelocity = 0f;
            
            if (playerController.LadderChecker.ClearedTop)
                Motor.LaunchOffLadderTop(playerController.transform.forward);

            playerController.StartLadderCooldown();
        }
    }
}