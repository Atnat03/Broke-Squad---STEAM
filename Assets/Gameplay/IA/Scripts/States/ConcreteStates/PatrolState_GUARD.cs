using System;
using System.Collections;
using MyPrint;
using UnityEngine;

namespace Gameplay.IA
{
    public struct PatrolParam
    {
        public bool IsAlerted;
        public Transform OldTarget;
    }
    
    [Serializable]
    public class PatrolState_GUARD : BaseState_GUARD<PatrolParam>
    {
        [Header("Patrol Points")]
        [SerializeField] private Transform[] _patrolPointsNormal;
        [SerializeField] private Transform[] _patrolPointsAlerted;
        private Transform[] _currentPatrolPoints;
        private int _currentPatrolNode = 0;

        private Coroutine _cantInvestigateCoroutine;
        private bool _canInvestigate = true;
        
        public override void EnterState(PatrolParam data)
        {
            ABPrint.PrintBool(data.IsAlerted, "Enter Patrol state : ");

            if (data.IsAlerted)
            {
                Brain.ChangeSettingToAlerted();

                _cantInvestigateCoroutine = Brain.StartCoroutine(CantInvestigateCoroutine());
            }
            
            _currentPatrolPoints = data.IsAlerted ? _patrolPointsAlerted : _patrolPointsNormal;
            _currentPatrolNode = 0;
            
            Brain.Motor.SetTarget(NearestPoint());
            Brain.Motor.SetSpeed(Brain.CurrentSetting.PatrolSpeed);
        }

        public override void UpdateState()
        {
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
                
                Brain.StopCoroutine(_cantInvestigateCoroutine);
                _cantInvestigateCoroutine = null;
                
                return;
            }
            
            //Perform Searching suspect objects
            Transform Suspect = Brain.LineSight.PerformFindingSuspect();

            if (Suspect && _canInvestigate)
            {
                Brain.ChangeState<InvestigateState_GUARD, InvestigateParam>(new InvestigateParam
                {
                    TargetPosition = Suspect.position,
                });
            }
        }

        IEnumerator CantInvestigateCoroutine()
        {
            _canInvestigate = false;
            
            yield return new WaitForSeconds(Brain.CurrentSetting.TimeBeforeCanInvestigateAgain);

            _canInvestigate = true;

            _cantInvestigateCoroutine = null;
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
        
        public void UpdateSetting()
        {
            Brain.Motor.SetSpeed(Brain.CurrentSetting.PatrolSpeed);
        }
    }
}