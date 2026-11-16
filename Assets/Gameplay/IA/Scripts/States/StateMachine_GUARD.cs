using System;
using System.Collections.Generic;
using Bus;
using Gameplay.IA;

namespace Gameplay.IA
{
    public class StateMachine_GUARD : NetworkBusListener
    {
        private readonly Dictionary<Type, IState_GUARD> _states = new();
        private IState_GUARD _current;

        public IState_GUARD Current => _current;
        
        public void Register(IState_GUARD state) => _states[state.GetType()] = state;
        
        public TState Change<TState, TParams>(TParams p) where TState : BaseState_GUARD<TParams>
        {
            _current?.ExitState();

            var next = (TState)_states[typeof(TState)];

            _current = next;

            next.EnterState(p);

            return next;
        }
    }
}