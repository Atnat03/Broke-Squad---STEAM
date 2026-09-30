using System;
using Bus;
using Gameplay.Controller;
using Gameplay.Items.Scripts.ItemModules;
using MyPrint;
using UnityEngine;

namespace Gameplay.Items.Scripts.PlayerItemGestion
{
    public class PlayerPickUpItem : NetworkBusListener
    {
        [SerializeField] private float _range;
        [SerializeField] private Camera _camera;
        [SerializeField] private LayerMask _layerMask;
        
        private PlayerInput _inputs;
        private PlayerInventory _inventory;
        
        public override void OnNetworkSpawn()
        {
            _inputs = GetComponent<PlayerInput>();
            _inventory = GetComponent<PlayerInventory>();
        }

        private void OnEnable()
        {
            _inputs.OnInteractInput += PickUpItem;
        }

        private void OnDisable()
        {
            _inputs.OnInteractInput -= PickUpItem;
        }

        private void PickUpItem()
        {
            if (!IsOwner)
                return;
            
            bool hasItem = _inventory.HasItemInHand();
            
            if (hasItem)
            {
                _inventory.DropItem(Vector3.zero, Quaternion.identity);
            }
            else
            {
                if (Physics.Raycast(_camera.transform.position, _camera.transform.forward, out RaycastHit hit, _range ,_layerMask))
                {
                    if (hit.transform.TryGetComponent(out ItemPickup item))
                    {
                        _inventory.GrabItem(item);
                        
                        InvokeEvent(new OnInteractItemInWorld());
                    }
                }
            }
        }
    }
}