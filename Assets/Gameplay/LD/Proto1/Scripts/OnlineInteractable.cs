using System;
using Bus;
using Gameplay;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

public abstract class OnlineInteractable : NetworkBusListener, IInteractable
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

    public void OnInteractParent()
    {
        if (IsServer) OnInteract();
        else OnInteractServerRpc();
    }
    
    [Rpc(SendTo.Server)]
    void InteractServerRpc(RpcParams rpcParams = default) => ServerInteract(rpcParams.Receive.SenderClientId);
    
    protected abstract void ServerInteract(ulong senderClientId);
    
    [Rpc(SendTo.Server)]
    void OnInteractServerRpc() => OnInteract();
    
    protected abstract void OnInteract();

    [Rpc(SendTo.Everyone)]
    protected void OpenEventsRpc() => onInteractableOpen?.Invoke();

    [Rpc(SendTo.Everyone)]
    protected void CloseEventsRpc() => onInteractableClose?.Invoke();
}