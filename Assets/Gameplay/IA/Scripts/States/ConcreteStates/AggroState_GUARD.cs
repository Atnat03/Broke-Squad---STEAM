
using System.Collections;
using Gameplay.PlayerData;
using MyPrint;
using Unity.Netcode;
using UnityEngine;

namespace Gameplay.IA
{
    public struct AggroParam
    {
        public Transform Target;
    }
    
    public class AggroState_GUARD : BaseState_GUARD<AggroParam>
    {
        private Transform _target;
        private PlayerHealth _playerHealth;
        
        //Attack
        private Coroutine _attackCoroutine;
        private bool IsAttacking { get; set; }
        
        public override void EnterState(AggroParam data)
        {
            _target = data.Target;
            
            _playerHealth = _target.TryGetComponent(out PlayerHealth playerHealth) ? playerHealth : null;
            
            Brain.Motor.SetSpeed(Brain.CurrentSetting.AggroSpeed);
            Brain.Motor.SetTarget(_target.position);

            Brain.ResetLockDownTimer();

            Brain.OnAggroStateChangeServerRpc(true);
        }
        
        public override void ExitState()
        {
            if (!Brain.IsServer) return;

            Brain.OnAggroStateChangeServerRpc(false);
        }

        public override void UpdateState()
        {
            bool stimulated = IsTargetClose() || WasRecentlyHit();
 
            Transform target = Brain.LineSight.PerformFindingPlayer(stimulated ? _target : null);
 
            if (!target || _playerHealth.IsDowned)
            { 
                Brain.ChangeState<PatrolState_GUARD, PatrolParam>(new PatrolParam { IsAlerted = false });
                return;
            }
 
            _target = target;
 
            Vector3 destination = Brain.LineSight.CanSeeTarget ? target.position : Brain.LineSight.LastSeenPosition;
 
            Brain.Motor.SetTarget(destination - _transform.forward * (Brain.CurrentSetting.AttackRange * 0.5f));

            HandleAttack();
        }

        private bool IsTargetClose()
        {
            if (!_target) return false;
 
            float radius = Brain.CurrentSetting.AggroCloseRadius;
            return (_target.position - _transform.position).sqrMagnitude <= radius * radius;
        }
 
        private bool WasRecentlyHit()
        {
            return Brain.Health.TimeSinceLastDamage <= Brain.CurrentSetting.AggroDamageMemory;
        }

        public void UpdateSetting()
        {
            Brain.Motor.SetSpeed(Brain.CurrentSetting.AggroSpeed);
        }
        
        #region Attack

        private void HandleAttack()
        {
            bool isInRangeToAttack = (_transform.position - _target.position).sqrMagnitude < Brain.CurrentSetting.AttackRange;
            
            if (isInRangeToAttack && _attackCoroutine == null && !IsAttacking && !Brain.Health.IsStun)
            {
                _attackCoroutine = Brain.StartCoroutine(AttackRoutine());
            }
        }

        IEnumerator AttackRoutine()
        {
            IsAttacking = true;
            Brain.Motor.StopAgent(true);

            PerformAttack();

            yield return new WaitForSeconds(Brain.CurrentSetting.AttackCooldown);
            
            IsAttacking = false;
            Brain.Motor.StopAgent(false);

            _attackCoroutine = null;
        }

        private void PerformAttack()
        {
            Brain.ReplicateAttackClientRpc();
            _playerHealth.ApplyDamage(Brain.CurrentSetting.Damage);
        }

        #endregion
    }
}