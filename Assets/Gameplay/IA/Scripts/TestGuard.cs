using System;
using UnityEngine;
using UnityEngine.AI;

namespace Gameplay.IA.Scripts
{
    public class TestGuard : MonoBehaviour
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
        private int _patrolPointIndex = 0;

        void Update()
        {
            if (_guardFieldOfView.CanSeeTarget)
            { 
                Chase();
            }
            else
            {
                Patrol();
            }
        }

        private void ReachTarget()
        {
            _patrolPointIndex = (_patrolPointIndex + 1) % _patrolPoints.Length;
        }

        private void Chase()
        {
            _agent.speed = _speedChase;
            _agent.SetDestination(_guardFieldOfView.Target.position);
            
            _meshRenderer.material.color = _colorChase;
        }
        
        private void Patrol()
        {
            _agent.speed = _speedPatrol;
            _meshRenderer.material.color = _colorPatrol;
            
            if (Vector3.Distance(transform.position, _patrolPoints[_patrolPointIndex].position) >= 2)
            {
                _agent.SetDestination(_patrolPoints[_patrolPointIndex].position);
            }
            else
            {
                ReachTarget();
            }
        }
    }
}