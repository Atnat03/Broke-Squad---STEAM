using Bus;
using MyPrint;
using UnityEngine;

namespace Gameplay.Equipment
{
    public class BackPack : NetworkBusListener, IInteractable
    {
        public void Interact(ulong clientId)
        {
            ABPrint.Print("Interact BP",ABColor.Yellow);
            
            InvokeEvent(new OnTryToPickUpBackPack_EVENT
            {
                ClientId = clientId,
                BackPackNOR = NetworkObject
            });
        }
        
        [SerializeField] private Outline _outline;
        
        public Outline outline => _outline;
        public void SetOutline(bool state)
        {
            _outline.enabled = state;
        }
    }
}