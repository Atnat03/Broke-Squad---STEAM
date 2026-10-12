using System;
using System.Collections.Generic;
using Bus;
using Gameplay;
using Gameplay.Items;
using MyPrint;
using Unity.Netcode;
using UnityEditor;
using UnityEngine;

public class PlayerEquipment : NetworkBusListener, IInteractable
{
    [SerializeField] private Transform _itemPos;
    
    [Header("BackPack Parent")]
    [SerializeField] private GameObject _backPack;
    
    
    public void Interact(ulong clientId)
    {
        if (IsServer)
            HandleInteract(clientId);
        else
            InteractServerRpc(clientId);
    }

    [Rpc(SendTo.Server)]
    private void InteractServerRpc(ulong clientId) => HandleInteract(clientId);

    private void HandleInteract(ulong clientId)
    {
        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client)) return;

        PlayerInventory playerSender = client.PlayerObject.GetComponentInChildren<PlayerInventory>();
        if (playerSender == null) return;

        ItemInstance item = playerSender.GetCurrentItemInHand();
        if (item == null) return;

        Transform slot = _itemPos;

        ItemPickup pickup = Instantiate(item.Data.PickupPrefab, slot.position, slot.rotation, slot);
        pickup.Setup(item);
        pickup.GetComponent<NetworkObject>().Spawn();
        
        if (!pickup.GetComponent<NetworkObject>().TrySetParent(NetworkObject, worldPositionStays: true))
            Debug.LogWarning("Reparent failed");
        
        pickup.GetComponent<Rigidbody>().useGravity = false;
        pickup.GetComponent<Rigidbody>().isKinematic = true;
        
        pickup.transform.GetChild(0).GetComponent<Collider>().enabled = false;
        
        playerSender.ClearCurrentSlot();
    }

    //Outline 
    [SerializeField] private Outline _outline;
    public Outline outline => _outline;
    public void SetOutline(bool state)
    {
        outline.enabled = state;
    }
}
