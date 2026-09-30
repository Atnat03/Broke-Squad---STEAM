using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

namespace Gameplay.IA.Scripts
{
    public class TestGuard : NetworkBehaviour
    {
        [SerializeField] private float _speedPatrol = 2;
        [SerializeField] private float _speedChase = 3;
        [SerializeField] private GuardFieldOfView _guardFieldOfView;
        
        [Header("Color")]
        [SerializeField] private MeshRenderer _meshRenderer;
        [SerializeField] private Color _colorPatrol;
        [SerializeField] private Color _colorChase;
        
        [Header("Navigation")]
        [SerializeField] private NavMeshAgent _agent;
        [SerializeField] private Transform[] _patrolPoints;
        
        private readonly NetworkVariable<int> _patrolPointIndex = new NetworkVariable<int>();
        private readonly NetworkVariable<bool> _isInChase = new NetworkVariable<bool>();

        public override void OnNetworkSpawn()
        {
            _isInChase.OnValueChanged += GuardStateChange;
        }

        public override void OnNetworkDespawn()
        {
            _isInChase.OnValueChanged -= GuardStateChange;
        }

        private void GuardStateChange(bool previousValue, bool newValue)
        {
            Color currentColor = newValue ? _colorChase : _colorPatrol;
            
            _meshRenderer.material.color = currentColor;
        }

        void Update()
        {
            if (!IsServer) return;
            
            if (_guardFieldOfView.CanSeeTarget)
            {
                _isInChase.Value = true;
                Chase();
            }
            else
            {
                _isInChase.Value = false;
                Patrol();
            }
        }

        private void ReachTarget()
        {
            _patrolPointIndex.Value = (_patrolPointIndex.Value + 1) % _patrolPoints.Length;
        }

        private void Chase()
        {
            _agent.speed = _speedChase;
            _agent.SetDestination(_guardFieldOfView.Target.position);
        }
        
        private void Patrol()
        {
            _agent.speed = _speedPatrol;
            
            if (Vector3.Distance(transform.position, _patrolPoints[_patrolPointIndex.Value].position) >= 2)
            {
                _agent.SetDestination(_patrolPoints[_patrolPointIndex.Value].position);
            }
            else
            {
                ReachTarget();
            }
        }
    }
}