namespace Gameplay.Items.Scripts.ItemModules
{
    public interface IRightClick : IItemModule
    {
        public void StartRightClick();
        public void EndRightClick();
    }
}