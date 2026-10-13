using System;
using Bus;
using Gameplay.PlayerData;
using TMPro;
using UnityEngine;
using UnityEngine.AI;

namespace Gameplay.IA
{
    public class GuardMotor : NetworkBusListener
    {
        public Transform Target { get; set; }
        public Vector3 SuspiciousTarget { get; set; }
        
        [SerializeField] private NavMeshAgent _navMeshAgent;
        private Vector3 _targetPosition;

        public Action OnInvestigateGoToNull;
        
        public void SetTarget(Vector3 position)
        {
            if (!IsServer) return;
            
            _targetPosition = position;
            
            _navMeshAgent.SetDestination(_targetPosition);
        }
        
        public void SetSpeed(float speed)
        {
            _navMeshAgent.speed = speed;
        }

        public void StopAgent() => _navMeshAgent.isStopped = true;
        
        public void StartAgent() => _navMeshAgent.isStopped = false;
        
        public bool HasTarget() => Target != null;
        public bool HasSuspiciousTarget() => SuspiciousTarget != Vector3.zero;

        public bool TargetKO()
        {
            if (Target)
            {
                if (Target.TryGetComponent(out PlayerHealth ph))
                {
                    return ph.IsDowned;
                }
            }

            return false;
        }
    }
}