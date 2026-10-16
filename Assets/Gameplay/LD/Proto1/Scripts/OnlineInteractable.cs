using Gameplay;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

public abstract class OnlineInteractable : NetworkBehaviour, IInteractable
{
    [SerializeField] Outline Outline;
    public Outline outline => Outline;

    public UnityEvent onInteractableOpen;
    public UnityEvent onInteractableClose;

    public void SetOutline(bool state) => outline.enabled = state;
    
    public void Interact(ulong clientId)
    {
        if (IsServer) ServerInteract(clientId);
        else InteractServerRpc();
    }
    
    [Rpc(SendTo.Server)]
    void InteractServerRpc() => ServerInteract(OwnerClientId);
    
    protected abstract void ServerInteract(ulong senderClientId);

    [Rpc(SendTo.Everyone)]
    protected void OpenEventsRpc() => onInteractableOpen?.Invoke();

    [Rpc(SendTo.Everyone)]
    protected void CloseEventsRpc() => onInteractableClose?.Invoke();
}