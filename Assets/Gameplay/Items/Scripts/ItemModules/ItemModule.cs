using Bus;
using Gameplay.Items.Scripts.PlayerItemGestion;
using UnityEngine;

namespace Gameplay.Items.Scripts.ItemModules
{
    public enum ItemInput{Left, Right}
    
    public abstract class ItemModule : IItemModule
    {
        protected ItemContext Context { get; private set; }

        public void ResetState() => SetModule();

        public void Initialize(ItemContext context)
        {
            Context = context;
            OnBind();
        }

        public IItemModule Clone() => (IItemModule)MemberwiseClone();

        protected virtual void SetModule() {}
        protected virtual void OnBind() {}
    }
    
    public class ItemContext
    {
        public ItemCore Core { get; }
        public PlayerInventory Inventory { get; }
        public Camera Camera { get; }

        public ItemContext(ItemCore core, PlayerInventory inventory, Camera camera)
        {
            Core = core;
            Inventory = inventory;
            Camera = camera;
        }
    }
}