namespace Gameplay.IA.Refacto.States
{
    public abstract class BaseState_GUARD
    {
        public abstract void EnterState(StateMachine_GUARD machine);
        public abstract void UpdateState(StateMachine_GUARD machine);
        public abstract void ExitState(StateMachine_GUARD machine);
    }
}