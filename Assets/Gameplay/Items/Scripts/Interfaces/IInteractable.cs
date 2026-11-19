using Gameplay.Items;
using UnityEngine;

namespace Gameplay
{
    public struct StartOutliningData
    {
        public bool State;
        public PlayerInventory Inventory;
    }
    
    public interface IInteractable
    {
        string GetTooltip(ItemModule[] playerModules);
        public Outline outline { get; }

        public void SetOutline(StartOutliningData data);
        
        public void Interact(ulong clientId);
    }
    
    public interface IHoldInteractable : IInteractable
    {
        void HoldStart(ulong clientId);
        void HoldEnd(ulong clientId);
    }
}