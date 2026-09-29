using Gameplay.Items.Scripts.PlayerItemGestion;

namespace Gameplay.Items.Scripts.ItemModules
{
    public enum ItemInput{Left, Right}
    
    public abstract class ItemModule : IItemModule
    {
        protected ItemCore core;

        public void Initialize(ItemCore core)
        {
            this.core = core;
            
            SetModule();
        }
        
        protected virtual void SetModule(){}
    }
}