using System;
using Unity.Netcode;
using Gameplay.Controller;
using MyPrint;
using UnityEngine;
using UnityEngine.AI;

namespace Gameplay.IA
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class GuardBehavior : NetworkBehaviour
    {
        [SerializeField] G_PatrolState _patrolState;
        
        public NavMeshAgent Agent => _agent;
        
        private StateMachine _stateMachine;
        
        private NavMeshAgent _agent;
        
        public override void OnNetworkSpawn()
        {
            _agent = GetComponent<NavMeshAgent>();
            
            SetUpStateMachine();
        }

        private void SetUpStateMachine()
        {
            _stateMachine = new StateMachine();
            
            _patrolState.SetBehaviour(this);
            
            //At(idle, patrol, new FuncPredicate(() => true));
            Any(_patrolState, new FuncPredicate(() => true));
            
            _stateMachine.SetState(_patrolState);
        }
        
        void At(IState from, IState to, IPredicate condition) => _stateMachine.AddTransition(from, to, condition);
        void Any(IState to, IPredicate condition) => _stateMachine.AddAnyTransition(to, condition);

        private void Update()
        {
            if(IsServer)
            {
                ABPrint.Print("State : " + _stateMachine.CurrentStateName);
                _stateMachine.Update();
            }
        }
        
        private void LateUpdate() { if (IsServer) _stateMachine.LateUpdate(); } 
    }
}