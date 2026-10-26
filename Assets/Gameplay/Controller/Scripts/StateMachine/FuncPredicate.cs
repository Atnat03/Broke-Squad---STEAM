namespace Gameplay.Controller
{
    using System;
    
    public class FuncPredicate : IPredicate
    {
        readonly Func< bool> func;

        public FuncPredicate(Func<bool> func)
        {
            this.func = func;
        }
        
        
        public bool Evaluate()
        {
            return func.Invoke();
        }
    }
    
    namespace Gameplay.Controller
    {
        public class TriggerPredicate : IPredicate
        {
            bool _triggered;

            public void Trigger() => _triggered = true;

            public bool Evaluate()
            {
                if (!_triggered) return false;
                _triggered = false;
                return true;
            }
        }
    }
}