using UnityEngine;

namespace Gameplay.IA.Refacto.States
{
    public abstract class BaseState_GUARD<TParams> : IState_GUARD
    {
        protected GuardBrain Brain;
        protected Transform _transform;
        
        public void Initialise(GuardBrain brain)
        {
            Brain = brain;
            _transform = brain.transform;
        }

        public abstract void EnterState(TParams p);
        public abstract void UpdateState();
        public abstract void ExitState();
    }
}