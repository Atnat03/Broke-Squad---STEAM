using Gameplay;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

public class OnlineInteractable : NetworkBehaviour, IInteractable
{
    [SerializeField] Outline Outline;
    public Outline outline => Outline;
    
    public UnityEvent onInteractableOpen;
    public UnityEvent onInteractableClose;
    
    public void SetOutline(bool state)
    {
        outline.enabled = state;
    }

    public virtual void Interact(ulong clientId)
    {
        Debug.Log("Interacting with " + clientId);      
        if (!IsServer)
        {
            InteractServerRpc();
            return;
        }
    }

    [Rpc(SendTo.Server)]
    public void InteractServerRpc()
    {
        Interact(OwnerClientId);
    }
}
