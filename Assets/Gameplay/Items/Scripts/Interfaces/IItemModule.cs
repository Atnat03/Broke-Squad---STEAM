namespace Gameplay.Items
{
    public interface IItemModule
    {
        void ResetState();
        void Initialize(ItemContext ctx);
        IItemModule Clone();
    }
}