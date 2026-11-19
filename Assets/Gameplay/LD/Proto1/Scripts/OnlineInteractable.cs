using System;
using System.Collections.Generic;
using Bus;
using Gameplay;
using Gameplay.Items;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

public abstract class OnlineInteractable : NetworkBusListener, IInteractable
{
    Outline Outline;

    [SerializeField] TooltipConfig _tooltipConfig;
    
    public Outline outline => Outline;

    public UnityEvent onInteractServer;
    public UnityEvent onInteractableOpen;
    public UnityEvent onInteractableClose;

    public override void OnNetworkSpawn()
    {
        if (transform.TryGetComponent(out Outline o))
        {
            Outline = o;
        }
        else
        {
            Outline = transform.GetComponentInChildren<Outline>();
        }
    }

    public void SetOutline(StartOutliningData data)
    {
        if(outline)
            outline.enabled = data.State;
    }
    
    public void Interact(ulong clientId)
    {
        onInteractServer?.Invoke();
        
        if (IsServer) ServerInteract(clientId);
        else InteractServerRpc();
    }

    public void OnInteractParent()
    {
        onInteractServer?.Invoke();
        
        if (IsServer) OnInteract();
        else OnInteractServerRpc();
    }
    
    [Rpc(SendTo.Server)]
    void InteractServerRpc(RpcParams rpcParams = default)
    {
        ServerInteract(rpcParams.Receive.SenderClientId);
    }
    
    protected abstract void ServerInteract(ulong senderClientId);
    
    [Rpc(SendTo.Server)]
    void OnInteractServerRpc()
    {
        OnInteract();
    }
    
    protected abstract void OnInteract();

    [Rpc(SendTo.Everyone)]
    protected void OpenEventsRpc() => onInteractableOpen?.Invoke();

    [Rpc(SendTo.Everyone)]
    protected void CloseEventsRpc() => onInteractableClose?.Invoke();
    
    public string GetTooltip(ItemModule[] playerModules)
    {
        return _tooltipConfig.GetTooltip(playerModules);
    }
}