namespace Gameplay.Controller.States
{
    public class CrouchState : BaseState
    {
        public CrouchState(PlayerController playerController) : base(playerController)
        {
        }

        public override void OnEnter()
        {
            Motor.JumpAllowed = false;
            playerController.PlayCrouchSound();
        }

        public override void OnFixedUpdate()
        {
            Motor.TargetSpeed = Profile.crouchSpeed;
            Body.SetTargetHeight(Body.CrouchHeight);
        }
    }
}