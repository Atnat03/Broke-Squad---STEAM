namespace Gameplay.Controller.States
{
    public class TiedUpState : BaseState
    {
        public TiedUpState(PlayerController playerController) : base(playerController)
        {
            
        }
        
        public override void OnEnter()
        {
            Motor.JumpAllowed = false;
            Motor.StopMoving();
            Body.SetTied(true);
        }

        public override void OnFixedUpdate()
        {
            Motor.TargetSpeed = playerController.TiedUpSpeed;
            Body.SetTargetHeight(Body.TiedUpHeight);
        }

        public override void OnExit() => Body.SetTied(false);
    }
}