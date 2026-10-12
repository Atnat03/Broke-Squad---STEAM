using System;
using MyPrint;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

namespace Gameplay.IA
{
    [Serializable]
    public class G_PatrolState : G_BaseState
    {
        [SerializeField] private float _patrolSpeed = 1f;
        
        [SerializeField] Transform[] _patrolNodes;
        private int _currentPatrolNode = 0;

        [SerializeField] private float _distanceCloseToChangePoint = 2;
        
        public override void OnEnter()
        {
            _behavior.SetColor(_color);
            
            _motor.SetTarget(NearestPoint());
            _motor.SetSpeed(_patrolSpeed);
        }

        public override void OnUpdate()
        {
            bool isToClose = (_myTransform.position - _patrolNodes[_currentPatrolNode].position).sqrMagnitude < _distanceCloseToChangePoint;

            if (isToClose)
            {
                NextPoint();
            }
        }

        private Vector3 NearestPoint()
        {
            Vector3 pos = _patrolNodes[_currentPatrolNode].position;
            int minNode = _currentPatrolNode;
            float minDistance = Vector3.Distance(pos, _myTransform.position);
            
            for (int i = 0; i < _patrolNodes.Length; i++)
            {
                if(i == _currentPatrolNode)
                    continue;
                
                Vector3 curPos = _patrolNodes[i].position;
                float currentDistance = Vector3.Distance(curPos, _myTransform.position);
                
                if (currentDistance < minDistance)
                {
                    minNode = i;
                    minDistance = currentDistance;
                    pos = _patrolNodes[i].position;
                }
            }
            
            _currentPatrolNode = minNode;
            return pos;
        }

        private void NextPoint()
        {
            _currentPatrolNode = (_currentPatrolNode + 1) % _patrolNodes.Length;
            _motor.SetTarget(_patrolNodes[_currentPatrolNode].position);
        }
    }
}