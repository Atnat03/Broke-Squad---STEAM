using System;
using Unity.Netcode;
using Gameplay.Controller;
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
        
        [Header("DebugVisuel")]
        [SerializeField] MeshRenderer _meshRenderer;
        [SerializeField] Color _normalColor;
        [SerializeField] Color _duringDetectionColor;
        [SerializeField] Color _hasTargetColor;
        
        private StateMachine _stateMachine;
        
        public override void OnNetworkSpawn()
        {
            GuardHealth = GetComponent<GuardHealth>();
            GuardMotor = GetComponent<GuardMotor>();
            GuardSight = GetComponent<GuardLineOfSight>();

            GuardSight.OnTargetSeen += GetTarget;
            GuardSight.OnDetectionProgressChanged += Detection;
            GuardSight.OnTargetLost += LostTarget;
            
            SetUpStateMachine();
        }

        private void Detection(float ratio)
        {
            if(ratio is > 0.1f and < 0.9f)
                _meshRenderer.material.color = _duringDetectionColor;
        }

        private void LostTarget()
        {
            _meshRenderer.material.color = _normalColor;
        }

        private void GetTarget(Transform obj)
        {
            _meshRenderer.material.color = _hasTargetColor;
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
                _stateMachine.Update();

                Transform found = GuardSight.Target;
                if(found)
                    ABPrint.Print(found.name, ABColor.Pink);
            }
        }
        
        private void LateUpdate() { if (IsServer) _stateMachine.LateUpdate(); }
    }
}