using System;
using System.Collections;
using Bus;
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
    public class GuardBehavior : NetworkBusListener
    {
        [SerializeField] G_PatrolState _patrolState;
        [SerializeField] G_PatrolState _patrolState_LOCKDOWN;
        [SerializeField] G_InvestigateState _investigateState;
        [SerializeField] G_InvestigateState _investigateState_LOCKDOWN;
        [SerializeField] G_PursuitState _pursuitState;
        [SerializeField] G_PursuitState _pursuitState_LOCKDOWN;
        [SerializeField] G_KoState _koState;
        
        [HideInInspector] public GuardHealth GuardHealth;
        [HideInInspector] public GuardMotor GuardMotor;
        [HideInInspector] public GuardLineOfSight GuardSight;

        [SerializeField] private MeshRenderer _meshRenderer;

        [Header("Sound")] 
        [SerializeField] private float _strengthToBeingAwake = 10;
        
        [HideInInspector] public Vector3 LastSoundPosition;
        [HideInInspector] public Transform LastSoundSource;
        
        private StateMachine _stateMachine;

        public IState CurrentState => _stateMachine.CurrentState;
        
        private LockDownManager _lockDownManager;
        
        private bool IsLockDown => _lockDownManager != null && _lockDownManager.IsLockDown;

        /*
        [HideInInspector] public Transform PlayerTarget = null;
        [HideInInspector] public Transform SuspectTarget = null;
        */

        public Action OnDetectSound;

        private void Awake()
        {
            GuardHealth = GetComponent<GuardHealth>();
            GuardMotor = GetComponent<GuardMotor>();
            GuardSight = GetComponent<GuardLineOfSight>();
            GuardSight.GuardBrain = this;
            GuardHealth.GuardBrain = this;
        }

        public override void OnNetworkSpawn()
        {
            GuardHealth.OnStun += HandleStun;
            
            _lockDownManager = LockDownManager.instance;
            _lockDownManager.SubscribeGuard(this);
            
            SetUpStateMachine();
        }
        
        public override void OnNetworkDespawn()
        {
            GuardHealth.OnStun -= HandleStun;
            
            _lockDownManager.UnSubscribeGuard(this);
        }

        private void SetUpStateMachine()
        {
            _stateMachine = new StateMachine();
            
            _patrolState.SetBehaviour(this);
            _pursuitState.SetBehaviour(this);
            _investigateState.SetBehaviour(this);
            
            /*
             _patrolState_LOCKDOWN.SetBehaviour(this);
            _pursuitState_LOCKDOWN.SetBehaviour(this);
            _investigateState_LOCKDOWN.SetBehaviour(this);
            */
            
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
            
            TriggerPredicate onPursuitEnded = new TriggerPredicate();
            _pursuitState.OnPursuitEnded += onPursuitEnded.Trigger;
            At(_pursuitState, _patrolState, onPursuitEnded);
            
            TriggerPredicate onEndInvestigate = new TriggerPredicate();
            _investigateState.OnInvestigationEnded += onEndInvestigate.Trigger;
            At(_investigateState, _patrolState, onEndInvestigate);
            
            TriggerPredicate onGetNewSuspiciousTarget = new TriggerPredicate();
            GuardSight.OnGetNewSuspicious += onGetNewSuspiciousTarget.Trigger;
            At(_patrolState, _investigateState, onGetNewSuspiciousTarget);
            
            // From patrol to investigate by suspicious sound
            TriggerPredicate onDetectSound = new TriggerPredicate();
            OnDetectSound += onDetectSound.Trigger;
            At(_patrolState, _investigateState, onDetectSound);
            
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
            if (!IsServer && _stateMachine == null) return;
            
            _stateMachine.Update();

            ABPrint.Print(_stateMachine.CurrentStateName, ABColor.Orange);
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

        public void TriggerBySound(float f, Transform newTarget)
        {
            if (f >= _strengthToBeingAwake)
            {
                ABPrint.Print("suspicious sound detected !! ", ABColor.Orange);

                LastSoundSource = newTarget;
                LastSoundPosition = newTarget != null ? newTarget.position : transform.position;
                
                OnDetectSound?.Invoke();
            }
        }
    }
}