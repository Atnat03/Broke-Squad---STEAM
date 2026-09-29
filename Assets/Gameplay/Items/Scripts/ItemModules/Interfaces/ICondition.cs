namespace Gameplay.Items.Scripts.ItemModules
{
    public interface ICondition : IItemModule
    {
        public ItemInput InputType { get; }
        public bool CheckCondition(ItemInput type);
        public void UseItem();
    }
}