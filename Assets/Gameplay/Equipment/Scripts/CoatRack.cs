using Bus;
using Gameplay.Items;
using Unity.Netcode;
using UnityEngine;

namespace Gameplay.Equipment
{

    public class CoatRack : NetworkBusListener, IInteractable
    {
        [SerializeField] private Collider _collider;

        private bool HasPickup => GetComponentInChildren<ItemPickup>() != null;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            RefreshCollider();
        }
        
        private void OnTransformChildrenChanged() => RefreshCollider();

        private void RefreshCollider()
        {
            if (_collider != null)
                _collider.enabled = !HasPickup;
        }
        
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
            if (HasPickup) return;
            
            if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client)) return;

            PlayerInventory playerSender = client.PlayerObject.GetComponentInChildren<PlayerInventory>();
            if (playerSender == null) return;

            ItemInstance item = playerSender.GetCurrentItemInHand();
            if (item == null) return;
            
            ItemPickup pickup = Instantiate(item.Data.PickupPrefab, transform.position, transform.rotation);
            NetworkObject pickupNO = pickup.GetComponent<NetworkObject>();
            
            pickupNO.Spawn();
            pickup.Setup(item);

            if (!pickupNO.TrySetParent(NetworkObject, worldPositionStays: true))
                Debug.LogWarning("Reparent failed");

            pickup.SetOnRack();

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
}
