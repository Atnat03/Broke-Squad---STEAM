namespace Gameplay.Controller
{
    using UnityEngine;

    public class BaseState : IState
    {
        protected readonly PlayerController playerController;

        protected BaseState(PlayerController playerController)
        {
            this.playerController = playerController;
        }

        public virtual void OnEnter() {}
        public virtual void OnExit() {}
        public virtual void OnUpdate() {}
        public virtual void OnFixedUpdate() {}
        public virtual void OnLateUpdate() {}
    }
}

