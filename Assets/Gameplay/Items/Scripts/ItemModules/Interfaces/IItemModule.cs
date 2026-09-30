using Gameplay.Items.Scripts.PlayerItemGestion;

namespace Gameplay.Items.Scripts.ItemModules
{
    public interface IItemModule
    {
        void ResetState();
        void Initialize(ItemContext ctx);
        IItemModule Clone();
    }
}