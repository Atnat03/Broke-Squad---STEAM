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
            playerController.ClearLadderJump();
            playerController.JumpPressed += playerController.RequestLadderJump;
        }

        public override void OnUpdate()
        {
            Motor.ClimbVelocity = playerController.ClimbInput * playerController.Profile.climbSpeed;
        }

        public override void OnExit()
        {
            playerController.JumpPressed -= playerController.RequestLadderJump;

            bool jumped = playerController.LadderJumpRequested;
            bool wasClimbingUp = Motor.ClimbVelocity > 0f;
            var ladder = playerController.LadderChecker;

            Motor.IsClimbing = false;
            Motor.ClimbVelocity = 0f;

            if (jumped) Motor.LaunchOffLadder(ladder.LadderNormal);              
            else if (wasClimbingUp && ladder.ClearedTop) Motor.LaunchOffLadderTop(-ladder.LadderNormal);          

            playerController.ClearLadderJump();
            playerController.StartLadderCooldown();
        }
    }
}