using MyPrint;

namespace Gameplay.IA.Refacto.States
{
    public class PatrolState_GUARD : BaseState_GUARD
    {
        public override void EnterState(StateMachine_GUARD machine)
        {
            ABPrint.Print("Enter state Patrol", ABColor.Green);
        }

        public override void UpdateState(StateMachine_GUARD machine)
        {

        }

        public override void ExitState(StateMachine_GUARD machine)
        {

        }
    }
}