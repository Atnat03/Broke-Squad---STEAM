using System;
using System.Collections.Generic;
using Bus;
using Gameplay.Items;
using MyPrint;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace Gameplay.IA
{
    public class LockDownManager : NetworkBusListener
    {
        public static LockDownManager instance;

        [Header("Settings")] 
        [SerializeField] private float _lockDownDuration = 10f;
        
        public bool IsLockDown => _isLockDown.Value;
        public float RatioDuration => _currentDuration.Value / _lockDownDuration;
        
        private readonly NetworkVariable<FixedString32Bytes> _triggerPlayerName = new NetworkVariable<FixedString32Bytes>();
        private readonly NetworkVariable<bool> _isGoal = new NetworkVariable<bool>();
        private readonly NetworkVariable<float> _currentDuration = new NetworkVariable<float>();
        private readonly NetworkVariable<bool> _isLockDown = new NetworkVariable<bool>();

        private List<GuardBrain> _guardList = new List<GuardBrain>();

        public Action<string> OnLockDownStart;
        
        private void Awake()
        {
            instance = this;
        }

        public override void OnNetworkSpawn()
        { 
            _isLockDown.OnValueChanged += OnLockDownChange;
            _currentDuration.OnValueChanged += OnDurationChange;
            
            if(IsServer)
                _isLockDown.Value = false;
            
            ListenToEvent<OnLockDownStart_EVENT>(TryStartLockDown);
            ListenToEvent<OnLockDownEnd_EVENT>(TryEndLockDown);
        }

        #region Start Lock Down

        private void TryStartLockDown(OnLockDownStart_EVENT ev) => StartLockDown(ev.PlayerTriggerName, ev.isGoal);

        public void StartLockDown(FixedString32Bytes triggerPlayerName, bool isGoal = false)
        {
            if (_isLockDown.Value)
                return;

            if (!IsServer)
            {
                StartLockDownServerRpc(triggerPlayerName, isGoal);
                return;
            }

            _triggerPlayerName.Value = triggerPlayerName;
            _isGoal.Value = isGoal;
            _isLockDown.Value = true;
            _currentDuration.Value = _lockDownDuration;
        }

        [Rpc(SendTo.Server)]
        private void StartLockDownServerRpc(FixedString32Bytes triggerPlayerName, bool isGoal) => StartLockDown(triggerPlayerName, isGoal);

        void StartLockDown()
        {
            if (!IsServer)
            {
                StartLockDownServerRpc();
                return;
            }

            _isGoal.Value = false;
            _isLockDown.Value = true;
            _currentDuration.Value = _lockDownDuration;
        }

        [Rpc(SendTo.Server)]
        private void StartLockDownServerRpc() => StartLockDown();

        #endregion
        
        #region End Lock Down
        
        private void TryEndLockDown(OnLockDownEnd_EVENT obj)
        {
            if (!_isLockDown.Value)
                return;
            
            EndLockDown();
        }
        
        [ContextMenu("End Lock Down")]
        public void EndLockDown()
        {
            if (!IsServer)
            {
                EndLockDownServerRpc();
                return;
            }

            _isLockDown.Value = false;
        }

        [Rpc(SendTo.Server)]
        private void EndLockDownServerRpc() => EndLockDown();

        #endregion
        
        private void OnLockDownChange(bool previousValue, bool newValue)
        {
            if (newValue)
                InvokeEvent(new OnLockDownStart_EVENT
                {
                    PlayerTriggerName = _triggerPlayerName.Value,
                    isGoal = _isGoal.Value
                });
            else
                InvokeEvent(new OnLockDownEnd_EVENT());
        }
        
        private void OnDurationChange(float previousValue, float newValue)
        {
            
        }

        private void Update()
        {
            if (!IsServer) return;

            if (IsLockDown)
            {
                if (!CanStartCooldownToStop())
                {
                    if(!Mathf.Approximately(_currentDuration.Value, _lockDownDuration))
                        _currentDuration.Value = _lockDownDuration;
                    return;
                }
                
                _currentDuration.Value -= Time.deltaTime;
                if (_currentDuration.Value <= 0)
                {
                    EndLockDown();
                }
            }
        }
        
        #region Guard List
        
        public void SubscribeGuard(GuardBrain guard) => _guardList.Add(guard);
        public void UnSubscribeGuard(GuardBrain guard) => _guardList.Remove(guard);

        bool CanStartCooldownToStop()
        {
            foreach (GuardBrain g in _guardList)
            {
                if (g.CurrentState is AggroState_GUARD)
                    return false;
            }
            
            return true;
        }
        
        
        #endregion
    }
}
