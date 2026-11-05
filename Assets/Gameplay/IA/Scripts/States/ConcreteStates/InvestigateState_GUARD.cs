using System.Collections;
using MyPrint;
using UnityEngine;

namespace Gameplay.IA
{
    public struct InvestigateParam
    {
        public Vector3 TargetPosition;
    }

    public class InvestigateState_GUARD : BaseState_GUARD<InvestigateParam>
    {
        Vector3 _targetPosition;
        Coroutine _investigateCoroutine;

        readonly Collider[] _buffer = new Collider[8];

        public override void EnterState(InvestigateParam p)
        {
            _targetPosition = p.TargetPosition;
            _investigateCoroutine = null;

            Brain.Motor.StopAgent(false);
            Brain.Motor.SetSpeed(Brain.CurrentSetting.InvestigateSpeed);
            Brain.Motor.SetTarget(_targetPosition);

            Brain.OnInvestigateStateChangeRpc(true);
        }

        public override void UpdateState()
        {
            Transform target = Brain.LineSight.PerformFindingPlayer();

            if (target)
            {
                Brain.ChangeState<AggroState_GUARD, AggroParam>(new AggroParam
                {
                    Target = target,
                });
                return;
            }

            if (_investigateCoroutine != null) return;

            Vector3 toTarget = _targetPosition - _transform.position;
            toTarget.y = 0f;

            float minDistance = Brain.CurrentSetting.MiniDistanceToStartInvestigate;
            bool hasArrived = toTarget.sqrMagnitude <= minDistance * minDistance;

            if (hasArrived)
            {
                _investigateCoroutine = Brain.StartCoroutine(StartInvestigation());
            }
        }

        IEnumerator StartInvestigation()
        {
            Brain.Motor.StopAgent(true);

            ABPrint.Print("Start Investigation");

            yield return new WaitForSeconds(Brain.CurrentSetting.TimeBeforeInvestigateScan);

            Brain.Motor.StopAgent(false);

            ABPrint.Print("Finish Investigation");

            Transform findSomeSuspectObject = ScanInvestigation();
            
            _investigateCoroutine = null;

            Brain.ChangeState<PatrolState_GUARD, PatrolParam>(new PatrolParam
            {
                IsAlerted = findSomeSuspectObject,
                OldTarget = findSomeSuspectObject
            });
        }

        private Transform ScanInvestigation()
        {
            int count = Physics.OverlapSphereNonAlloc(_transform.position, Brain.CurrentSetting.RadiusInvestigate, _buffer, Brain.CurrentSetting.TargetSuspectMask, QueryTriggerInteraction.Ignore);

            Vector3 eye = Brain.LineSight.EyePosition;

            for (int i = 0; i < count; i++)
            {
                Transform target = _buffer[i].transform;
                Vector3 targetPoint = _buffer[i].bounds.center;
                Vector3 dirToTarget = targetPoint - eye;

                Vector3 flatDir = new Vector3(dirToTarget.x, 0f, dirToTarget.z);

                if (flatDir.sqrMagnitude < 0.0001f) return target;

                if (Vector3.Angle(_transform.forward, flatDir) > Brain.CurrentSetting.AngleInvestigate * 0.5f) continue;

                if (Physics.Linecast(eye, targetPoint, Brain.CurrentSetting.ObstructionMask, QueryTriggerInteraction.Ignore)) continue;

                return target;
            }

            return null;
        }

        public override void ExitState()
        {
            if (_investigateCoroutine != null)
            {
                Brain.StopCoroutine(_investigateCoroutine);
                _investigateCoroutine = null;
            }

            Brain.Motor.StopAgent(false);

            Brain.OnInvestigateStateChangeRpc(false);
        }

        public void UpdateSetting()
        {
            Brain.Motor.SetSpeed(Brain.CurrentSetting.InvestigateSpeed);
        }
    }
}