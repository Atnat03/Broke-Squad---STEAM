using Gameplay.Items.Scripts.PlayerItemGestion;

namespace Gameplay.Items.Scripts.ItemModules
{
    public enum ItemInput{Left, Right}
    
    public abstract class ItemModule : IItemModule
    {
        protected ItemCore core;

        public void ResetState() => SetModule();

        public void Initialize(ItemCore core)
        {
            this.core = core;
            OnBind();
        }

        public IItemModule Clone() => (IItemModule)MemberwiseClone();

        protected virtual void SetModule() {}
        protected virtual void OnBind() {}
    }
}