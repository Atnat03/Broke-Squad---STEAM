using System;
using MyPrint;
using UnityEngine;

namespace Gameplay.IA.Refacto.States
{
    public struct PatrolParam
    {
        public bool IsAlerted;
    }
    
    [Serializable]
    public class PatrolState_GUARD : BaseState_GUARD<PatrolParam>
    {
        [Header("Patrol Points")]
        [SerializeField] private Transform[] _patrolPointsNormal;
        [SerializeField] private Transform[] _patrolPointsAlerted;
        private Transform[] _currentPatrolPoints;
        private int _currentPatrolNode = 0;
        
        public override void EnterState(PatrolParam data)
        {
            ABPrint.PrintBool(data.IsAlerted, "Enter Patrol state : ");

            _currentPatrolPoints = data.IsAlerted ? _patrolPointsAlerted : _patrolPointsNormal;
            _currentPatrolNode = 0;
            
            Brain.Motor.SetTarget(NearestPoint());
            Brain.Motor.SetSpeed(Brain.CurrentSetting.PatrolSpeed);
        }

        public override void UpdateState()
        {
            ABPrint.Print("Patrol State");

            if (_currentPatrolPoints == null || _currentPatrolPoints.Length == 0)
                return;
            
            bool isToClose = (_transform.position - _currentPatrolPoints[_currentPatrolNode].position).sqrMagnitude < Brain.CurrentSetting.DistanceCloseToChangePoint;
            
            if (isToClose)
            {
                NextPoint();
            }
            
            //Perform Searching Target
            Transform Target = Brain.LineSight.PerformFindingPlayer();

            if (Target)
            {
                Brain.ChangeState<AggroState_GUARD, AggroParam>(new AggroParam
                {
                    Target = Target,
                });
            }
        }

        public override void ExitState()
        { }
        
        private Vector3 NearestPoint()
        {
            Vector3 pos = _currentPatrolPoints[_currentPatrolNode].position;
            int minNode = _currentPatrolNode;
            float minDistance = Vector3.Distance(pos, _transform.position);
            
            for (int i = 0; i < _currentPatrolPoints.Length; i++)
            {
                if(i == _currentPatrolNode)
                    continue;
                
                Vector3 curPos = _currentPatrolPoints[i].position;
                float currentDistance = Vector3.Distance(curPos, _transform.position);
                
                if (currentDistance < minDistance)
                {
                    minNode = i;
                    minDistance = currentDistance;
                    pos = _currentPatrolPoints[i].position;
                }
            }
            
            _currentPatrolNode = minNode;
            return pos;
        }
        
        private void NextPoint()
        {
            _currentPatrolNode = (_currentPatrolNode + 1) % _currentPatrolPoints.Length;
            Brain.Motor.SetTarget(_currentPatrolPoints[_currentPatrolNode].position);
        }
    }
}