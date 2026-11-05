namespace Gameplay.Items
{
    public class HardToOpenModule : ActionModule, IFirstAction, IServerAction
    {
        public ICondition Condition => ConditionParent;
        public void StartFirstAction()
        {
            
        }

        public void StopFirstAction()
        {
            
        }

        public void ServerExecute(OnModuleDoAction_EVENT data)
        {
            
        }
    }
}