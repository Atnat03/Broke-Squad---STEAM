using System;
using System.Collections.Generic;
using Bus;
using Gameplay.IA.Refacto.States;
using UnityEngine;

namespace Gameplay.IA.Refacto
{
    [RequireComponent(typeof(GuardMotor))]
    [RequireComponent(typeof(GuardHealth))]
    [RequireComponent(typeof(GuardLineSight))]
    public class GuardBrain : NetworkBusListener
    {
        private StateMachine_GUARD _stateMachine;
        
        [Header("Settings")]
        [SerializeField] private GuardSetting_SO _normalSettings;
        [SerializeField] private GuardSetting_SO _alertedSettings;

        [Header("States")] 
        [SerializeField] private PatrolState_GUARD _patrolState;
        [SerializeField] private AggroState_GUARD _aggroState = new AggroState_GUARD();
        
        public GuardSetting_SO CurrentSetting { get; private set; }
        
        //Scripts Annexes
        public GuardMotor Motor { get; set; }
        public GuardHealth Health { get; set; }
        public GuardLineSight LineSight { get; set; }

        private void Awake()
        {
            _stateMachine = new();

            //Patrol State
            _patrolState.Initialise(this);
            _stateMachine.Register(_patrolState);

            //Aggro State
            _aggroState.Initialise(this);
            _stateMachine.Register(_aggroState);
            
            ChangeSettingToNormal();
            
            Motor = GetComponent<GuardMotor>();
            Health = GetComponent<GuardHealth>();
            LineSight = GetComponent<GuardLineSight>();
            LineSight.Initialise(this);
            
            ChangeState<PatrolState_GUARD, PatrolParam>(new PatrolParam{IsAlerted = false});
        }

        private void Update()
        {
            if(!IsServer) return;
            
            _stateMachine.Current.UpdateState();
        }
        
        public void ChangeState<TState, TParams>(TParams p) where TState : BaseState_GUARD<TParams>
        {
            _stateMachine.Change<TState, TParams>(p);
        }
        
        public void ChangeSettingToNormal() => CurrentSetting = _normalSettings;
        public void ChangeSettingToAlerted() => CurrentSetting = _alertedSettings;
    }
}