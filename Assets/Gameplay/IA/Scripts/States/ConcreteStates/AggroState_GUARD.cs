
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

            _playerHealth = GetPlayerHealth(_target);
            
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

            Transform target = Brain.LineSight.PerformFindingPlayer(
                stimulated ? _target : null
            );

            if (!target)
            {
                if (_playerHealth == null || _playerHealth.IsDowned)
                {
                    Brain.ChangeState<PatrolState_GUARD, PatrolParam>(new PatrolParam { IsAlerted = false });
                    return;
                }

                Brain.Motor.SetTarget(Brain.LineSight.LastSeenPosition);
                return;
            }

            _target = target;

            _playerHealth = GetPlayerHealth(_target);

            Vector3 destination = Brain.LineSight.CanSeeTarget ? _target.position : Brain.LineSight.LastSeenPosition;

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
            if (!_target || !_playerHealth) return;

            bool isInRangeToAttack = (_transform.position - _target.position).sqrMagnitude < Brain.CurrentSetting.AttackRange * Brain.CurrentSetting.AttackRange;

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
            if (!_target || !_playerHealth)
                return;
            
            Brain.ReplicateAttackClientRpc();
            _playerHealth.ApplyDamage(Brain.CurrentSetting.Damage);
        }

        private PlayerHealth GetPlayerHealth(Transform target)
        {
            PlayerHealth pH = null;
            
            foreach (Transform t in _target)
            {
                if (t.TryGetComponent(out PlayerHealth health))
                {
                    pH = health;
                }
            }

            return pH;
        }
        
        #endregion
    }
}