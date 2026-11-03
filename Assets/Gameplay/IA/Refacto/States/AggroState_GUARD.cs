
using MyPrint;
using UnityEngine;

namespace Gameplay.IA.Refacto.States
{
    public struct AggroParam
    {
        public Transform Target;
    }
    
    public class AggroState_GUARD : BaseState_GUARD<AggroParam>
    {
        public override void EnterState(AggroParam data)
        {
            Brain.Motor.SetSpeed(Brain.CurrentSetting.AggroSpeed);
            Brain.Motor.SetTarget(data.Target.position);
        }

        public override void UpdateState()
        {
            ABPrint.Print("Aggro State Update", ABColor.Red);

            Transform target = Brain.LineSight.PerformFindingPlayer();

            if (!target)
            { 
                Brain.ChangeState<PatrolState_GUARD, PatrolParam>(new PatrolParam { IsAlerted = false });
                return;
            }

            Vector3 destination = Brain.LineSight.CanSeeTarget ? target.position : Brain.LineSight.LastSeenPosition;

            Brain.Motor.SetTarget(destination);
        }

        public override void ExitState() { }
    }
}