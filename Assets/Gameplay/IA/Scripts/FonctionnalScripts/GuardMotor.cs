using MyPrint;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

namespace Gameplay.IA
{
    public class GuardMotor : NetworkBehaviour
    {
        [SerializeField] NavMeshAgent _agent;
        Vector3 _targetPosition;

        public void SetTarget(Vector3 target)
        {
            _targetPosition = target;
            _agent.SetDestination(_targetPosition);
        }

        public void SetSpeed(float speed)
        {
            _agent.speed = speed;
        }

        public void StopAgent(bool stop)
        {
            if (!IsServer) return;

            _agent.isStopped = stop;
        }

        public float GetDistanceFromTarget(Vector3 target)
        {
            return (target - transform.position).sqrMagnitude;
        }
    }
}