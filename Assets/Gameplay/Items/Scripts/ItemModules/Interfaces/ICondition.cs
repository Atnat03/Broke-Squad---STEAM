namespace Gameplay.Items.Scripts.ItemModules
{
    public interface ICondition : IItemModule
    {
        public ItemInput InputType { get; }
        public bool CheckCondition();
        public void UseItem();
        public void ThrowItem();
    }
}