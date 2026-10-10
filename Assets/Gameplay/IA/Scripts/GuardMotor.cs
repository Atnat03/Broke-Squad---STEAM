using System;
using Bus;
using UnityEngine;
using UnityEngine.AI;

namespace Gameplay.IA
{
    public class GuardMotor : NetworkBusListener
    {
        [SerializeField] private NavMeshAgent _navMeshAgent;
        private float _speed;        
        private Vector3 _targetPosition;
        
        public void SetTarget(Vector3 position)
        {
            if (!IsServer) return;
            
            _targetPosition = position;
            
            _navMeshAgent.SetDestination(_targetPosition);
        }
        
        public void SetSpeed(float speed)
        {
            _speed = speed;
            _navMeshAgent.speed = speed;
        }
    }
}