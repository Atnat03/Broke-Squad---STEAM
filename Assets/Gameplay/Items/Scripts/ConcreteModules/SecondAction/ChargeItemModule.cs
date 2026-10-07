using System;

namespace Gameplay.Items
{
    [Serializable]
    public class ChargeItemModule : ItemModule, IFirstAction, IServerAction
    {
        public ICondition Condition { get; }
        public void StartFirstAction()
        {
            throw new System.NotImplementedException();
        }

        public void StopFirstAction()
        {
            throw new System.NotImplementedException();
        }

        public void ServerExecute(OnModuleDoAction_EVENT data)
        {
            throw new System.NotImplementedException();
        }
    }
}