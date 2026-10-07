namespace Gameplay.Items
{
    public abstract class ItemModule
    {
        protected ItemContext Context;

        public virtual void ResetState() => SetModule();

        public void Initialize(ItemContext context)
        {
            Context = context;
            OnBind();
        }

        public IItemModule Clone()
        {
            var clone = (ItemModule)MemberwiseClone();
            clone.OnCloned();
            return (IItemModule)clone;
        }

        protected virtual void OnCloned() {}
        protected virtual void SetModule() {}
        protected virtual void OnBind() {}
    }
}