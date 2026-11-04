using System;
using System.Collections;
using System.Collections.Generic;
using Bus;
using Gameplay.IA;
using MyPrint;
using Unity.Netcode;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Gameplay.IA
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
        [SerializeField] private InvestigateState_GUARD _investigateState = new InvestigateState_GUARD();
        [SerializeField] private DeathState_GUARD _deathState = new DeathState_GUARD();

        [Header("Desactivate Things")]
        [SerializeField] private Collider[] _componentToDisableWhenDie;
        [SerializeField] private GameObject[] _gameObjectToDisableWhenDie;

        [Header("Other")]
        [SerializeField] private float _strengthToBeingAwake = 5;
        [SerializeField] private float _alertedStateDuration = 10f;
        private Coroutine _alertedStateCoroutine;

        private bool _isLockDownActive;

        //Network Variables
        private readonly NetworkVariable<bool> _isAlerted = new NetworkVariable<bool>();
        private readonly NetworkVariable<float> _currentTimerLockDown = new NetworkVariable<float>();

        public GuardSetting_SO CurrentSetting { get; private set; }

        //Scripts Annexes
        public GuardMotor Motor { get; set; }
        public GuardHealth Health { get; set; }
        public GuardLineSight LineSight { get; set; }

        public IState_GUARD CurrentState => _stateMachine.Current;

        //Actions
        public Action<bool> OnAlertedChange;
        public Action<float> OnUpdateLockDownTimer;
        public Action OnAttacking;
        public Action<bool> OnGuardIsAggro;
        public Action<bool> OnGuardIsInvestigate;

        //Others
        LockDownManager _lockDownManager;

        public override void OnNetworkSpawn()
        {
            _isAlerted.OnValueChanged += OnAlertedStateChange;
            _currentTimerLockDown.OnValueChanged += OnLockDownTimerUpdate;

            _lockDownManager = LockDownManager.instance;

            if (IsServer)
            {
                _isLockDownActive = _lockDownManager && _lockDownManager.IsLockDown;

                ListenToEvent<OnLockDownStart_EVENT>(EnterAlertedState);
                ListenToEvent<OnLockDownEnd_EVENT>(ExitAlertedState);

                ChangeState<PatrolState_GUARD, PatrolParam>(new PatrolParam { IsAlerted = false });

                OnAggroStateChangeServerRpc(false);
            }
        }

        public override void OnNetworkDespawn()
        {
            _isAlerted.OnValueChanged -= OnAlertedStateChange;
            _currentTimerLockDown.OnValueChanged -= OnLockDownTimerUpdate;
        }

        private void Awake()
        {
            Motor = GetComponent<GuardMotor>();

            Health = GetComponent<GuardHealth>();
            Health.Initialise(this);

            LineSight = GetComponent<GuardLineSight>();
            LineSight.Initialise(this);

            _stateMachine = new();

            //Patrol State
            _patrolState.Initialise(this);
            _stateMachine.Register(_patrolState);

            //Aggro State
            _aggroState.Initialise(this);
            _stateMachine.Register(_aggroState);

            //Investigate State
            _investigateState.Initialise(this);
            _stateMachine.Register(_investigateState);

            //Death State
            _deathState.Initialise(this);
            _stateMachine.Register(_deathState);
            
            CurrentSetting = _normalSettings;
        }

        private void Update()
        {
            if (!IsServer) return;
            if (Health.IsDead) return;

            _stateMachine.Current.UpdateState();

            UpdateTimerLockDown();
        }

        public void ChangeState<TState, TParams>(TParams p) where TState : BaseState_GUARD<TParams>
        {
            if (!IsServer) return;

            _stateMachine.Change<TState, TParams>(p);
        }

        #region Alerted State

        public void ChangeSettingToNormal()
        {
            StopAlertedTimer();

            _isAlerted.Value = false;
            CurrentSetting = _normalSettings;
            UpdateSettingsInStates();
        }

        public void ChangeSettingToAlerted()
        {
            StopAlertedTimer();

            _isAlerted.Value = true;
            CurrentSetting = _alertedSettings;
            UpdateSettingsInStates();

            if (!_isLockDownActive)
                _alertedStateCoroutine = StartCoroutine(AlertedStateRoutine());
        }

        private void StopAlertedTimer()
        {
            if (_alertedStateCoroutine == null) return;

            StopCoroutine(_alertedStateCoroutine);
            _alertedStateCoroutine = null;
        }

        IEnumerator AlertedStateRoutine()
        {
            yield return new WaitForSeconds(_alertedStateDuration);

            _alertedStateCoroutine = null;

            if (_isLockDownActive) yield break;

            ChangeSettingToNormal();
        }

        private void EnterAlertedState(OnLockDownStart_EVENT data)
        {
            _isLockDownActive = true;
            ChangeSettingToAlerted();
        }

        private void ExitAlertedState(OnLockDownEnd_EVENT data)
        {
            _isLockDownActive = false;
            ChangeSettingToNormal();
        }

        private void UpdateSettingsInStates()
        {
            _aggroState.UpdateSetting();
            _patrolState.UpdateSetting();
            _investigateState.UpdateSetting();
            //Line sight update settings
        }

        private void OnAlertedStateChange(bool previousValue, bool newValue)
        {
            OnAlertedChange?.Invoke(newValue);
        }

        #endregion


        #region LockDown Trigger

        public void ResetLockDownTimer()
        {
            if (!IsServer) return;

            if (_lockDownManager && !_lockDownManager.IsLockDown)
            {
                if (CurrentState is AggroState_GUARD)
                {
                    _currentTimerLockDown.Value = CurrentSetting.TimeBeforeLockDownStart;
                }
            }
        }

        private void UpdateTimerLockDown()
        {
            if (_currentTimerLockDown.Value > 0)
            {
                _currentTimerLockDown.Value -= Time.deltaTime;

                if (_currentTimerLockDown.Value <= 0)
                {
                    InvokeEvent(new OnLockDownStart_EVENT());
                }
            }
        }

        private void OnLockDownTimerUpdate(float previousValue, float newValue)
        {
            OnUpdateLockDownTimer?.Invoke(newValue / CurrentSetting.TimeBeforeLockDownStart);
        }

        #endregion

        #region Attacking

        [Rpc(SendTo.Everyone)]
        public void ReplicateAttackClientRpc()
        {
            OnAttacking?.Invoke();
        }

        #endregion

        #region Other

        [Rpc(SendTo.Everyone)]
        public void OnAggroStateChangeServerRpc(bool state)
        {
            OnGuardIsAggro?.Invoke(state);
        }

        [Rpc(SendTo.Everyone)]
        public void OnInvestigateStateChangeRpc(bool state)
        {
            OnGuardIsInvestigate?.Invoke(state);
        }

        #endregion

        public void DesactivateWhenDie()
        {
            foreach (Collider c in _componentToDisableWhenDie)
                c.enabled = false;

            foreach (GameObject g in _gameObjectToDisableWhenDie)
                g.SetActive(false);
        }

        //Suspicious Sound
        public void TriggerBySound(float f, Transform newTarget)
        {
            if (f >= _strengthToBeingAwake)
            {
                ABPrint.Print("Change to investigate state", ABColor.Yellow);

                ChangeState<InvestigateState_GUARD, InvestigateParam>(new InvestigateParam
                {
                    TargetPosition = newTarget.position,
                });
            }
        }
    }
}