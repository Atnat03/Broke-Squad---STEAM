namespace Gameplay.IA
{
    public struct DeathParam
    { }
    
    public class DeathState_GUARD : BaseState_GUARD<DeathParam>
    {
        public override void EnterState(DeathParam p)
        {
            Brain.Motor.StopAgent(true);
            Brain.DesactivateWhenDie();
        }

        public override void UpdateState()
        {}

        public override void ExitState()
        {}
    }
}