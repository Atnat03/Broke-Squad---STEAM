using System;
using Bus;
using Gameplay.IA.Refacto.States;

namespace Gameplay.IA.Refacto
{
    public class StateMachine_GUARD : NetworkBusListener
    {
        private BaseState_GUARD _currentState;
        
        private PatrolState_GUARD _patrolState;
        private PursuitState_GUARD _pursuitState;

        private void Start()
        {
            _currentState = _patrolState;
            
            _currentState.EnterState(this);
        }

        private void Update()
        {
            _currentState.UpdateState(this);
        }
    }
}