namespace Gameplay.Items
{
    public interface ICondition : IItemModule
    {
        public bool CheckCondition();
        public void UseItem();
        public void ThrowItem();
    }
}