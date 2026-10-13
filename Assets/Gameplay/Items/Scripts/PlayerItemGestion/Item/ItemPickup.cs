using System;
using System.Collections.Generic;
using Bus;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace Gameplay.Items
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(BoxCollider))]
    [RequireComponent(typeof(NetworkTransform))]
    public class ItemPickup : NetworkBusListener, IInteractable
    {
        [SerializeField] private SO_Item _itemIndex; 
        private Outline _outline;
        
        public Outline outline => _outline;
        
        private ItemInstance _instance;
        
        public ItemInstance Instance
        {
            get
            {
                    _instance = new ItemInstance(_itemIndex);

                return _instance;
            }
        }

        private void Awake()
        {
            _outline = GetComponentInChildren<Outline>();
        }

        public override void OnNetworkSpawn()
        {
            SetOutline(false);
        }

        public void SetOutline(bool state)
        {
            if(outline != null)
                outline.enabled = state;
        }

        public void Interact(ulong clientId)
        {
            InvokeEvent(new OnPickupItem_EVENT
            {
                ClientId = clientId,
                ItemPickUpRef = NetworkObject
            });
        }
        
        public void Setup(ItemInstance instance) => _instance = instance;
        
        public void OnCollisionEnter(Collision collision)
        {
            if(_instance == null)
                return;

            foreach (var passif in _instance.Passifs)
            {
                passif.OnCollide(collision, this);
            }
        }
    }
}