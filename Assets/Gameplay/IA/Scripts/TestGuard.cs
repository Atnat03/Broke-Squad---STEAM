using System;
using System.Collections;
using Gameplay.LD.Scripts;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using Gameplay.PlayerData;

namespace Gameplay.IA.Scripts
{
    public class TestGuard : NetworkBehaviour, IDamageable, IStunnable
    {
        [SerializeField] private float _speedPatrol = 2;
        [SerializeField] private float _speedChase = 3;
        [SerializeField] private float _distanceToStopChasing = 5;
        [SerializeField] private GuardFieldOfView _guardFieldOfView;

        [Header("Color")]
        [SerializeField] private MeshRenderer _meshRenderer;
        [SerializeField] private Color _colorPatrol;
        [SerializeField] private Color _colorChase;

        [Header("Navigation")]
        [SerializeField] private NavMeshAgent _agent;
        [SerializeField] private Transform[] _patrolPoints;
        [SerializeField] private float _patrolPointReachDistance = 2f;

        [Header("Attack")]
        [SerializeField] private int _damage = 10;
        [SerializeField] private float _attackRange = 2;
        [SerializeField] private float _attackCooldown = 1;
        [SerializeField] private float _timeToStayInRangeToApplyDamage = 0.5f;

        [Header("HP")]
        [SerializeField] private float _maxHealth = 100;
        [SerializeField] private Color _colorHit = Color.white;
        [SerializeField] private Image _currentHealthUI;

        [Header("Stun")] 
        [SerializeField] private Color _stunColor = Color.deepSkyBlue;
        private Coroutine _stunCoroutine;
        
        private readonly NetworkVariable<float> _currentHealth = new NetworkVariable<float>();
        private readonly NetworkVariable<bool> _isInChase = new NetworkVariable<bool>();
        private readonly NetworkVariable<bool> _isStun = new NetworkVariable<bool>();

        private int _patrolPointIndex;
        private bool _isAttacking;
        private Coroutine _hitColorCoroutine;

        private Transform _target;
        
        public override void OnNetworkSpawn()
        {
            _isInChase.OnValueChanged += GuardStateChange;
            _currentHealth.OnValueChanged += UpdateHP;
            _isStun.OnValueChanged += StunStateChange;
            
            _currentHealth.Value = _maxHealth;

            if (IsServer)
            {
                _currentHealth.Value = _maxHealth;
            }
            else
            {
                _agent.enabled = false;
            }

            ApplyStateColor(_isInChase.Value);
            RefreshHealthUI(_currentHealth.Value);
            
            Patrol();
        }
        

        public override void OnNetworkDespawn()
        {
            _isInChase.OnValueChanged -= GuardStateChange;
            _currentHealth.OnValueChanged -= UpdateHP;
            _isStun.OnValueChanged -= StunStateChange;
        }

        private void UpdateHP(float previousValue, float newValue)
        {
            RefreshHealthUI(newValue);

            if (newValue < previousValue)
            {
                if (_hitColorCoroutine != null)
                    StopCoroutine(_hitColorCoroutine);

                _hitColorCoroutine = StartCoroutine(HitColor());
            }
        }

        private void RefreshHealthUI(float value)
        {
            if (_currentHealthUI != null)
                _currentHealthUI.fillAmount = Mathf.Clamp01(value / _maxHealth);
        }

        private void StunStateChange(bool previousValue, bool newValue)
        {
            if (newValue)
            {
                _meshRenderer.material.color = _stunColor;
            }
            else
            {
                _meshRenderer.material.color = _colorPatrol;
            }
        }
        
        private IEnumerator HitColor()
        {
            _meshRenderer.material.color = _colorHit;

            yield return new WaitForSeconds(0.25f);

            ApplyStateColor(_isInChase.Value);
            _hitColorCoroutine = null;
        }

        private void GuardStateChange(bool previousValue, bool newValue)
        {
            if (_hitColorCoroutine == null)
                ApplyStateColor(newValue);
        }

        private void ApplyStateColor(bool chasing)
        {
            _meshRenderer.material.color = chasing ? _colorChase : _colorPatrol;
        }

        private void Update()
        {
            if (!IsServer) return;
            if (_isStun.Value) return;

            // Si je la vois, je (re)prends la cible
            if (_guardFieldOfView.CanSeeTarget && _guardFieldOfView.Target != null)
            {
                _target = _guardFieldOfView.Target;
            }
            // Sinon je la garde tant qu'elle est assez proche, je la perds si elle est trop loin
            else if (_target != null &&
                     Vector3.Distance(_target.position, transform.position) > _distanceToStopChasing)
            {
                _target = null;
            }

            bool isChasing = _target != null;

            if (_isInChase.Value != isChasing)
                _isInChase.Value = isChasing;

            if (isChasing)
            {
                Chase();

                if (!_isAttacking && IsTargetInAttackRange())
                    StartCoroutine(AttackRoutine());
            }
            else
            {
                Patrol();
            }
        }

        private IEnumerator AttackRoutine()
        {
            _isAttacking = true;

            float elapsed = 0f;
            while (elapsed < _timeToStayInRangeToApplyDamage)
            {
                if (!IsTargetInAttackRange())
                {
                    _isAttacking = false;
                    yield break;
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            if (_target != null && _target.TryGetComponent(out PlayerData.PlayerData player))
                player.TakeDamage(_damage);

            yield return new WaitForSeconds(_attackCooldown);

            _isAttacking = false;
        }

        private bool IsTargetInAttackRange()
        {
            if (_target == null) 
                return false;

            return Vector3.Distance(_target.position, transform.position) < _attackRange;
        }

        private void Chase()
        {
            if (_target == null ||IsTargetInAttackRange())
                return;
            
            _agent.speed = _speedChase;
            _agent.SetDestination(_target.position);
        }

        private void Patrol()
        {
            if (_patrolPoints == null || _patrolPoints.Length == 0) return;

            _agent.speed = _speedPatrol;

            Transform point = _patrolPoints[_patrolPointIndex];

            if (Vector3.Distance(transform.position, point.position) >= _patrolPointReachDistance)
                _agent.SetDestination(point.position);
            else
                _patrolPointIndex = (_patrolPointIndex + 1) % _patrolPoints.Length;
        }

        public void ApplyDamage(float damage)
        {
            if (!IsServer) return;

            _currentHealth.Value -= damage;

            if (_currentHealth.Value <= 0)
                NetworkObject.Despawn();
        }

        public void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, transform.position + Vector3.forward * _attackRange);
        }

        public void ApplyStun(float stunDuration)
        {
            if (!IsServer) return;

            if (_stunCoroutine == null)
            {
                _stunCoroutine = StartCoroutine(Stun(stunDuration));
            }
        }

        private IEnumerator Stun(float stunDuration)
        {
            _isStun.Value = true;
            _agent.isStopped = true;
            
            yield return new WaitForSeconds(stunDuration);
            
            _agent.isStopped = false;
            _isStun.Value = false;
            
            _stunCoroutine = null;
        }
    }
}