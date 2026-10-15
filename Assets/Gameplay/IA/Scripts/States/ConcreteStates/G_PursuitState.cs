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
        [SerializeField] private float _aggroRadius = 15f;
        [SerializeField] private float _maxPursuitDistance = 25f;

        private Transform _target;
        private PlayerHealth _playerHealth;
        
        [Header("Attack")] 
        [SerializeField] private float _damage = 10;
        [SerializeField] private float _attackRange = 2;
        [SerializeField] private float _attackCooldown = 1.5f;
        private bool _isAttacking = false;
        private Coroutine _attackCoroutine;
        
        public Action OnAttacking;
        public Action OnPursuitEnded;

        private readonly Collider[] _overlapBuffer = new Collider[16];
        
        public override void OnEnter()
        {
            _behavior.SetColor(_color);
            
            Transform newTarget = _behavior.GuardSight.Target;
            
            if (newTarget == null)
            {
                newTarget = FindClosestPlayerInRadius();
            }

            if (newTarget == null) return;
            
            _target = newTarget;
            
            _behavior.GuardSight.ForceTarget(newTarget);

            if (newTarget.TryGetComponent(out PlayerHealth player)) 
                _playerHealth = player;
            
            _behavior.InvokeEvent(new OnLockDownStart_EVENT());
            
            _motor.SetSpeed(_pursuitSpeed);
        }

        public override void OnUpdate()
        {
            _target = _behavior.GuardSight.Target;

            if (_target == null)
            {
                _playerHealth = null;
                OnPursuitEnded?.Invoke();
                return;
            }

            if (_playerHealth == null)
                _target.TryGetComponent(out _playerHealth);

            float distSqr = (_myTransform.position - _target.position).sqrMagnitude;

            if (distSqr > _maxPursuitDistance * _maxPursuitDistance)
            {
                _behavior.GuardSight.ForceTarget(null); 
                OnPursuitEnded?.Invoke();
                return;
            }
    
            bool isToClose = distSqr < _attackRange * _attackRange;
            if (isToClose && !_isAttacking)
            {
                if (_attackCoroutine == null)
                    _attackCoroutine = _behavior.StartCoroutine(AttackRoutine());
            }
            else
            {
                _motor.SetTarget(_target.position);
            }
        }

        IEnumerator AttackRoutine()
        {
            if (_playerHealth != null && !_playerHealth.IsDowned)
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
        
        private Transform FindClosestPlayerInRadius()
        {
            int hitCount = Physics.OverlapSphereNonAlloc(
                _myTransform.position, 
                _aggroRadius, 
                _overlapBuffer
            );

            Transform closest = null;
            float closestDistSqr = float.MaxValue;

            for (int i = 0; i < hitCount; i++)
            {
                Collider hit = _overlapBuffer[i];
                if (hit == null) continue;

                if (hit.TryGetComponent(out PlayerHealth playerHealth) && !playerHealth.IsDowned)
                {
                    float distSqr = (hit.transform.position - _myTransform.position).sqrMagnitude;
                    if (distSqr < closestDistSqr)
                    {
                        closestDistSqr = distSqr;
                        closest = hit.transform;
                    }
                }
            }

            return closest;
        }
    }
}