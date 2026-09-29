namespace Gameplay.Controller
{
    public class Transition : ITransition
    {
        public IState TargetState { get; }
        public IPredicate Condition { get; }

        public Transition(IState targetState, IPredicate condition)
        {
            this.TargetState = targetState;
            this.Condition = condition;
        }
    }
}