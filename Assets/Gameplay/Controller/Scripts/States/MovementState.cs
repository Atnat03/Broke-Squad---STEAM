namespace Gameplay.Controller.States
{
    public class MovementState : BaseState
    {
        public MovementState(PlayerController playerController) : base(playerController)
        {
        }
        
        public override void OnEnter()
        {
            Motor.JumpAllowed = true;
            Body.SetTied(false);
        }

        public override void OnFixedUpdate()
        {
            Motor.TargetSpeed = Profile.walkSpeed;
            Body.SetTargetHeight(Body.StandHeight);
        }
    }
}