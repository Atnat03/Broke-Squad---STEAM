namespace Gameplay.Controller
{
    public interface ITransition
    {
        IState TargetState { get; }
        IPredicate Condition { get; }
        
    }
}