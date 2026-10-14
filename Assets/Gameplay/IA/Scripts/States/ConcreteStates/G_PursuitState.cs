using System;
using System.Collections;
using Gameplay.PlayerData;
using MyPrint;
using UnityEngine;

namespace Gameplay.IA
{
    [Serializable]
    public class G_PursuitState : G_BaseState
    {
        [SerializeField] private float _pursuitSpeed = 1f;
        private Transform _target;
        private PlayerHealth _playerHealth;

        [Header("Attack")] 
        [SerializeField] private float _damage = 10;
        [SerializeField] private float _attackRange = 2;
        [SerializeField] private float _attackCooldown = 1.5f;
        private bool _isAttacking = false;
        private Coroutine _attackCoroutine;
        
        public Action OnAttacking;
        
        public override void OnEnter()
        {
            _behavior.SetColor(_color);
            
            Transform newTarget = _behavior.PlayerTarget;

            if (!newTarget) return;
            
            _target = newTarget;
            if (newTarget.TryGetComponent(out PlayerHealth player)) _playerHealth = player;
            
            _behavior.InvokeEvent(new OnLockDownStart_EVENT());
            
            _motor.SetSpeed(_pursuitSpeed);
        }

        public override void OnUpdate()
        {
            if(_target == null)
                return;
            
            bool isToClose = (_myTransform.position - _target.position).sqrMagnitude < _attackRange;

            if (isToClose && !_isAttacking)
            {
                if(_attackCoroutine == null)
                    _attackCoroutine = _behavior.StartCoroutine(AttackRoutine());
            }
            else
            {
                _motor.SetTarget(_target.position);
            }
        }

        IEnumerator AttackRoutine()
        {
            if(!_playerHealth.IsDowned)
            {
                _playerHealth.ApplyDamage(_damage);
                
                _isAttacking = true;
                _motor.StopAgent();
                
                OnAttacking?.Invoke();
                
                yield return new WaitForSeconds(_attackCooldown);
                
                _motor.StartAgent();
                _isAttacking = false;

                _attackCoroutine = null;
            }
        }
    }
}