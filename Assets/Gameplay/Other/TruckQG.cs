using Bus;
using Gameplay.Items;
using UnityEngine;

namespace Gameplay.Other
{
    public class TruckQG : NetworkBusListener, IInteractable
    {
        [SerializeField] private Outline _outline;

        [SerializeField] TooltipConfig _tooltipConfig;
        
        public string GetTooltip(ItemModule[] playerModules)
        {
            return _tooltipConfig.GetTooltip(playerModules);
        }

        public Outline outline => _outline;
        public void SetOutline(StartOutliningData data)
        {
            _outline.enabled = data.State;
        }

        public void Interact(ulong clientId)
        {
            InvokeEvent(new OnTryToWin_EVENT());
        }
    }
}