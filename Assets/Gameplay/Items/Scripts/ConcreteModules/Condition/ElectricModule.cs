namespace Gameplay.Items
{
    public class ElectricModule : ItemModule, ICondition, IServerAction
    {
        public bool CheckCondition()
        {
            throw new System.NotImplementedException();
        }

        public void UseItem()
        {
            throw new System.NotImplementedException();
        }

        public void DisableItemInHand()
        {
            throw new System.NotImplementedException();
        }

        public void ServerExecute(OnModuleDoAction_EVENT data)
        {
            throw new System.NotImplementedException();
        }
    }
}