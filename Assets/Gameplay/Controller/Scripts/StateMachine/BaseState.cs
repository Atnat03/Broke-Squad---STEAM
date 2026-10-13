namespace Gameplay.Controller.States
{
    public abstract class BaseState : IState
    {
        protected readonly PlayerController playerController;

        protected BaseState(PlayerController playerController) => this.playerController = playerController;

        protected ControllerProfileSO Profile => playerController.Profile;
        protected PlayerMotor Motor => playerController.Motor;
        protected PlayerBody Body => playerController.Body;
        protected PlayerStamina Stamina => playerController.Stamina;

        public virtual void OnEnter() {}
        public virtual void OnExit() {}
        public virtual void OnUpdate() {}
        public virtual void OnFixedUpdate() {}
        public virtual void OnLateUpdate() {}
    }
}