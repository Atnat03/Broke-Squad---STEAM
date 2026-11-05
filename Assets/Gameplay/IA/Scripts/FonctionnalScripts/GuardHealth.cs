using System;
using System.Collections;
using Gameplay.LD.Scripts;
using MyPrint;
using Unity.Netcode;
using UnityEngine;

namespace Gameplay.IA
{
    public class GuardHealth : NetworkBehaviour, IDamageable, IStunnable
    {
        [Header("Settings")] 
        [SerializeField] private float _baseHP = 100;
        
        private readonly Collider[] _nearBuffer = new Collider[8];
        private float _lastDamageTime = float.NegativeInfinity;
        
        public float TimeSinceLastDamage => Time.time - _lastDamageTime;
        
        public bool IsStun {get; private set;}
        public bool IsDead => _isDead.Value;
        
        //Network Variables
        private readonly NetworkVariable<float> _currentHP = new();
        private readonly NetworkVariable<bool> _isDead = new();
        
        //Actions
        public Action<float> OnHealthUpdate;
        public Action<bool> OnGuardDie;
        public Action<bool> OnGuardStunning;

        //Brain reference        
        public GuardBrain Brain { get; set; }
        public void Initialise(GuardBrain brain) => Brain = brain;
        
        public override void OnNetworkSpawn()
        {
            _currentHP.OnValueChanged += OnHpChange;
            _isDead.OnValueChanged += OnDie;
            
            if(IsServer)
                _currentHP.Value = _baseHP;
        }
        public override void OnNetworkDespawn()
        {
            _currentHP.OnValueChanged -= OnHpChange;
            _isDead.OnValueChanged -= OnDie;
        }

        public void ApplyDamage(float damage)
        {
            if (!IsServer) return;
            
            _currentHP.Value -= damage;
            _lastDamageTime = Time.time;

            Brain.ResetLockDownTimer();
            
            if (_currentHP.Value <= 0)
            {
                Death();
                return;
            }

            if (Brain.CurrentState is AggroState_GUARD) return;
 
            Transform target = GetNearestPlayer();
            
            if (target)
            {
                Brain.ChangeState<AggroState_GUARD, AggroParam>(new AggroParam
                {
                    Target = target
                });
            }
        }

        private void Death()
        {
            _isDead.Value = true;
            Brain.ChangeState<DeathState_GUARD, DeathParam>(new DeathParam());
        }

        private Transform GetNearestPlayer()
        {
            int count = Physics.OverlapSphereNonAlloc(transform.position, Brain.CurrentSetting.AggroCloseRadius, _nearBuffer, Brain.CurrentSetting.TargetPlayerMask, QueryTriggerInteraction.Ignore);
            
            Transform nearest = null;
            float minDistance = float.MaxValue;
            
            for (int i = 0; i < count; i++)
            {
                Transform current = _nearBuffer[i].transform;
                float distance = (current.position - transform.position).sqrMagnitude;
 
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearest = current;
                }
            }
            
            return nearest;
        }
        
        #region NV Value change
        
        private void OnHpChange(float previousValue, float newValue)
        {
            OnHealthUpdate?.Invoke(newValue / _baseHP);
        }
        
        private void OnDie(bool previousValue, bool newValue)
        {
            OnGuardDie?.Invoke(newValue);
        }
        
        #endregion

        #region Stun
        
        public void ApplyStun(float stunDuration)
        {
            if (!IsServer) return;

            PerformStunRpc(stunDuration);
        }

        [Rpc(SendTo.Everyone)]
        private void PerformStunRpc(float duration)
        {
            StartCoroutine(StunRoutine(duration));
        }

        IEnumerator StunRoutine(float duration)
        {
            OnGuardStunning?.Invoke(true);
            Brain.Motor.StopAgent(true);
            IsStun = true;

            yield return new WaitForSeconds(duration);

            OnGuardStunning?.Invoke(false);
            Brain.Motor.StopAgent(false);
            IsStun = false;
        }
        
        #endregion
    }
}