namespace Gameplay.Items
{
    public interface IServerAction
    {
        public void ServerExecute(OnModuleDoAction_EVENT data);
    }
}