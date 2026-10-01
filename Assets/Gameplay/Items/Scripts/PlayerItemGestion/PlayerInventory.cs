using Assets.Gameplay.LD.Scripts;
using Bus;
using Gameplay.Controller;
using Gameplay.Items.Scripts;
using Gameplay.Items.Scripts.ItemData;
using Gameplay.Items.Scripts.ItemModules.ConcreteModules;
using Gameplay.Items.Scripts.PlayerItemGestion;
using Gameplay.LD.Scripts;
using System;
using System.Linq;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace Gameplay.Items.Scripts
{
    public class PlayerInventory : NetworkBusListener
    {
        public Camera PlayerCamera => _camera;
        public ItemCore CurrentItem => _currentItem;

        [SerializeField] private Transform _parent;
        [SerializeField] private Transform _remoteParent;
        [SerializeField] private SO_ItemList _database;

        [Header("Picking")]
        [SerializeField] private Camera _camera;
        [SerializeField] private float _range;
        [SerializeField] private LayerMask _layerMask;

        [Header("UI")]
        [Header("Throw")]
        [SerializeField] private GameObject _throwUI;
        [SerializeField] private Image _throwImage;

        [Header("Electic")]
        [SerializeField] private GameObject _electicUI;
        [SerializeField] private TextMeshProUGUI _electicPercent;

        private readonly NetworkVariable<int> _heldItemId = new(-1);
        private readonly NetworkVariable<float> _electricPercent = new(0f);
        
        private ItemInstance _serverInstance;

        private PlayerInput _input;
        private ItemCore _currentItem;
        private Outliner _currentHoverItem = null;

        public override void OnNetworkSpawn()
        {
            _input = GetComponent<PlayerInput>();

            _heldItemId.OnValueChanged += OnHeldItemChanged;
            _electricPercent.OnValueChanged += OnElectricPercentChanged;

            if (_heldItemId.Value != -1)
                OnHeldItemChanged(-1, _heldItemId.Value);

            if (IsOwner)
                _input.OnInteractInput += OnInteract;

            EnableBar(false);
            EnableElectricInfo(false);

            _input.OnStartLeftInput += CheckOpenDoor;
        }
        
        public override void OnNetworkDespawn()
        {
            _heldItemId.OnValueChanged -= OnHeldItemChanged;
            _electricPercent.OnValueChanged -= OnElectricPercentChanged;

            if (IsOwner && _input != null)
                _input.OnInteractInput -= OnInteract;
            
            _input.OnStartLeftInput -= CheckOpenDoor;
        }

        private void OnInteract()
        {
            if (HasItemInHand())
            {
                DropItemRpc();
                return;
            }

            if (Physics.Raycast(_camera.transform.position, _camera.transform.forward, out RaycastHit hit, _range, _layerMask, QueryTriggerInteraction.Ignore))
            {
                if(hit.transform.TryGetComponent(out ItemPickup pickup) && pickup.TryGetComponent(out NetworkObject netObj))
                {
                    PickUpRpc(netObj);
                    InvokeEvent(new OnInteractItemInWorld());
                }
            }

            IInteractable interactable = hit.collider.GetComponentInParent<IInteractable>();
            interactable?.Interact();
        }
        
        private void CheckOpenDoor()
        {
            if (Physics.Raycast(_camera.transform.position, _camera.transform.forward, out RaycastHit hit, _range, _layerMask, QueryTriggerInteraction.Ignore))
            {
                if (hit.transform.TryGetComponent(out Door door))
                {
                    if (!door.CanOpenWithoutKey)
                        return;
                    
                    door.TryOpen(new Vector2Int(-1, -1));
                }
            }
        }


        private void Update()
        {
            if (HasItemInHand()) return;
            
            if (Physics.Raycast(_camera.transform.position, _camera.transform.forward,
                    out RaycastHit hit, _range, _layerMask, QueryTriggerInteraction.Ignore)
                && hit.transform.TryGetComponent(out Outliner pickup))
            {
                if(_currentHoverItem == null)
                {
                    _currentHoverItem = pickup;
                    _currentHoverItem.SetOutline(true);
                }
            }
            else
            {
                if (_currentHoverItem != null)
                {
                    _currentHoverItem.SetOutline(false);
                    _currentHoverItem = null;
                }
            }
        }

        [Rpc(SendTo.Server)]
        private void PickUpRpc(NetworkObjectReference pickupRef)
        {
            if (_heldItemId.Value != -1) return;
            if (!pickupRef.TryGet(out NetworkObject netObj)) return;
            if (!netObj.TryGetComponent(out ItemPickup pickup)) return;
            if (pickup.Instance == null) return;
            if (Vector3.Distance(transform.position, netObj.transform.position) > _range + 1.5f) return;

            _serverInstance = pickup.Instance;
            _heldItemId.Value = pickup.Instance.Data.id;
            netObj.Despawn();
        }

        [Rpc(SendTo.Server)]
        private void DropItemRpc()
        {
            if (_serverInstance == null) return;

            Vector3 pos = _parent.position + transform.forward * 0.5f;
            ItemPickup pickup = Instantiate(_serverInstance.Data.pickUpPrefab, pos, transform.rotation);
            pickup.Setup(_serverInstance);
            pickup.GetComponent<NetworkObject>().Spawn();

            _serverInstance = null;
            _heldItemId.Value = -1;
        }

        public void DestroyItemInHand()
        {
            if (!IsServer || _serverInstance == null) return;

            _currentItem?.ThrowItem();

            _serverInstance.Cleanup();
            _serverInstance = null;
            _heldItemId.Value = -1;
        }

        private void OnHeldItemChanged(int oldId, int newId)
        {
            if (_currentItem != null)
            {
                if (IsOwner) 
                    _currentItem.UnsubscribeFromInput(_input);

                if (!IsServer) 
                    _currentItem.Instance?.Cleanup();

                EnableBar(false);
                EnableElectricInfo(false);
                
                Destroy(_currentItem.gameObject);
                _currentItem = null;
            }

            if (newId == -1) 
                return;

            SO_Item data = _database.GetItem(newId);

            Transform parent = IsOwner ? _parent : _remoteParent;

            var go = new GameObject(data.itemName);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;

            ItemCore core = go.AddComponent<ItemCore>();
            GameObject visual = Instantiate(data.visualPrefab, core.transform);
            visual.transform.localPosition = Vector3.zero;

            ItemInstance instance = IsServer ? _serverInstance : new ItemInstance(data);
            core.SetInstance(instance, this);
            
            if (IsOwner) 
                UpdateElectricInfo(_electricPercent.Value); 

            _currentItem = core;
            if (IsOwner) 
                _currentItem.SubscribeToInput(_input);
        }
        
        public void RequestThrow(float charge01, Vector3 camPos, Quaternion camRot)
            => ThrowRpc(charge01, camPos, camRot);

        [Rpc(SendTo.Server)]
        private void ThrowRpc(float charge01, Vector3 camPos, Quaternion camRot)
        {
            if (_serverInstance == null) return;

            ThrowModule module = _serverInstance.RightClicks.OfType<ThrowModule>().FirstOrDefault();
            if (module == null) return;

            if (Vector3.Distance(camPos, transform.position) > 3f)
                camPos = _parent.position;

            Vector3 spawnPos = camPos + camRot * Vector3.forward * module.SpawnDistance;

            ItemPickup thrown = ServerSpawnPickup(spawnPos, camRot);
            module.ApplyThrow(thrown, charge01, camRot);
        }

        private ItemPickup ServerSpawnPickup(Vector3 pos, Quaternion rot)
        {
            if(_serverInstance.Data.pickUpPrefab == null)
                return null;
            
            ItemPickup pickup = Instantiate(_serverInstance.Data.pickUpPrefab, pos, rot);
            pickup.Setup(_serverInstance);
            pickup.GetComponent<NetworkObject>().Spawn();

            _serverInstance = null;
            _heldItemId.Value = -1;
            return pickup;
        }
        
        private void OnElectricPercentChanged(float _, float value)
        {
            if (IsOwner) UpdateElectricInfo(value);
        }

        public void SetElectricPercent(float percent)
        {
            if (IsServer) _electricPercent.Value = percent;
        }
        
        public void RequestChargePickup(NetworkObjectReference target) => ChargePickupRpc(target);

        [Rpc(SendTo.Server)]
        private void ChargePickupRpc(NetworkObjectReference target)
        {
            if (_serverInstance == null) return;

            ChargeItemModule module = _serverInstance.LeftClicks.OfType<ChargeItemModule>().FirstOrDefault();
            if (module == null) return;

            if (!target.TryGet(out NetworkObject netObj)) return;
            if (!netObj.TryGetComponent(out ItemPickup pickup)) return;

            if (Vector3.Distance(transform.position, netObj.transform.position) > module.Range + 1.5f) return;

            module.ApplyCharge(pickup);
        }
        
        public bool HasItemInHand() => _heldItemId.Value != -1;
        public void UpdateBar(float t) => _throwImage.fillAmount = t;
        public void UpdateElectricInfo(float percent) => _electicPercent.text = (int)percent + " %";
        public void EnableElectricInfo(bool state) { if (IsOwner) _electicUI.SetActive(state); }
        public void EnableBar(bool state)          { if (IsOwner) _throwUI.SetActive(state); }
        
        public void RequestOpenDoor(NetworkObjectReference door) => OpenDoorRpc(door);

        [Rpc(SendTo.Server)]
        private void OpenDoorRpc(NetworkObjectReference doorRef)
        {
            if (_serverInstance == null) return;

            OpenDoorModule module = _serverInstance.LeftClicks.OfType<OpenDoorModule>().FirstOrDefault();
            if (module == null) return;

            if (!doorRef.TryGet(out NetworkObject netObj)) return;
            if (!netObj.TryGetComponent(out Door door)) return;

            if (Vector3.Distance(transform.position, netObj.transform.position) > module.Range + 1.5f) return;

            if (module.ApplyOpen(door))
            { }
        }
    }
}