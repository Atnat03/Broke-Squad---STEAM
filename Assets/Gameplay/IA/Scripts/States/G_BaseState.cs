using System;
using Gameplay.Controller;

namespace Gameplay.IA
{
    [Serializable]
    public abstract class G_BaseState : IState
    {
        protected GuardBehavior _behavior;
        
        public void SetBehaviour(GuardBehavior behavior) => _behavior = behavior;
        
        public virtual void OnEnter() {}
        public virtual void OnExit() {}
        public virtual void OnUpdate() {}
        public virtual void OnFixedUpdate() {}
        public virtual void OnLateUpdate() {}
    }
}