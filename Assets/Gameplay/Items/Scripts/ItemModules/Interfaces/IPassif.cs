namespace Gameplay.Items.Scripts.ItemModules
{
    public interface IPassif : IItemModule
    {
        public void OnCollide();
        public void OnThrow();
        public void OnUpdateState();
    }
}