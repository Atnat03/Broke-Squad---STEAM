using Gameplay.Items;

namespace Gameplay
{
    public interface IInteractable
    {
        string GetTooltip(ItemModule[] playerModules);
        public Outline outline { get; }

        public void SetOutline(bool state);
        
        public void Interact(ulong clientId);
    }
    
    public interface IHoldInteractable : IInteractable
    {
        void HoldStart(ulong clientId);
        void HoldEnd(ulong clientId);
    }
}