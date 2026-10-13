using System;
using System.Collections;
using Unity.Netcode;
using Gameplay.Controller;
using Gameplay.Controller.Gameplay.Controller;
using Gameplay.IA.Scripts;
using MyPrint;
using UnityEngine;
using UnityEngine.AI;

namespace Gameplay.IA
{
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(GuardMotor))]
    [RequireComponent(typeof(GuardHealth))]
    [RequireComponent(typeof(GuardLineOfSight))]
    public class GuardBehavior : NetworkBehaviour
    {
        [SerializeField] G_PatrolState _patrolState;
        [SerializeField] G_InvestigateState _investigateState;
        [SerializeField] G_PursuitState _pursuitState;
        [SerializeField] G_KoState _koState;
        
        [HideInInspector] public GuardHealth GuardHealth;
        [HideInInspector] public GuardMotor GuardMotor;
        [HideInInspector] public GuardLineOfSight GuardSight;

        [SerializeField] private MeshRenderer _meshRenderer;
        
        private StateMachine _stateMachine;
        
        public override void OnNetworkSpawn()
        {
            GuardHealth = GetComponent<GuardHealth>();
            GuardMotor = GetComponent<GuardMotor>();
            GuardSight = GetComponent<GuardLineOfSight>();

            GuardHealth.OnStun += HandleStun;
            
            SetUpStateMachine();
        }

        private void SetUpStateMachine()
        {
            _stateMachine = new StateMachine();
            
            _patrolState.SetBehaviour(this);
            _pursuitState.SetBehaviour(this);
            _investigateState.SetBehaviour(this);
            _koState.SetBehaviour(this);
            
            //Get new target
            TriggerPredicate onGetNewTargetPatrol = new TriggerPredicate();
            GuardSight.OnGetNewTarget += onGetNewTargetPatrol.Trigger;
            At(_patrolState, _pursuitState, onGetNewTargetPatrol);
            
            
            TriggerPredicate onGetNewTargetInvestigate = new TriggerPredicate();
            _investigateState.OnFindPlayer += onGetNewTargetInvestigate.Trigger;
            At(_investigateState, _pursuitState, onGetNewTargetInvestigate);
            
            
            //Loose target
            TriggerPredicate onLooseTarget = new TriggerPredicate();
            GuardSight.OnTargetLost += onLooseTarget.Trigger;
            At(_pursuitState, _patrolState, onLooseTarget);
            //At(_pursuitState, _patrolState, new FuncPredicate(GuardMotor.TargetKO));
            
            TriggerPredicate onEndInvestigate = new TriggerPredicate();
            _investigateState.OnInvestigationEnded += onEndInvestigate.Trigger;
            At(_investigateState, _patrolState, onEndInvestigate);
            
            
            TriggerPredicate onGetNewSuspiciousTarget = new TriggerPredicate();
            GuardSight.OnGetNewSuspicious += onGetNewSuspiciousTarget.Trigger;
            At(_patrolState, _investigateState, onGetNewSuspiciousTarget);
            
            // From patrol to investigate by suspicious sound
            
            //KO
            Any(_koState, new FuncPredicate(() => GuardHealth.IsKO));

            //Event on take damage
            TriggerPredicate onHit = new TriggerPredicate();
            GuardHealth.OnTakeDamage += onHit.Trigger;
            Any(_pursuitState, onHit);
            
            _stateMachine.SetState(_patrolState);
        }
        
        void At(IState from, IState to, IPredicate condition) => _stateMachine.AddTransition(from, to, condition);
        void Any(IState to, IPredicate condition) => _stateMachine.AddAnyTransition(to, condition);

        private void Update()
        {
            if(IsServer)
            {
                _stateMachine.Update();

                ABPrint.Print(_stateMachine.CurrentStateName, ABColor.Orange);
            }
        }

        private void LateUpdate()
        {
            if (IsServer) _stateMachine.LateUpdate();
        }
        
        public void SetColor(Color color) => _meshRenderer.material.color = color;
        
        
        private void HandleStun(bool isStunned)
        {
            if (isStunned)
            {
                GuardMotor.StopAgent();
            }
            else
            {
                GuardMotor.StartAgent();
            }
        }
    }
}