using Bus;
using UnityEngine;

namespace Gameplay.Other
{
    public class TruckQG : NetworkBusListener, IInteractable
    {
        [SerializeField] private Outline _outline;
        
        public Outline outline => _outline;
        public void SetOutline(bool state)
        {
            _outline.enabled = state;
        }

        public void Interact(ulong clientId)
        {
            InvokeEvent(new OnTryToWin_EVENT());
        }
    }
}