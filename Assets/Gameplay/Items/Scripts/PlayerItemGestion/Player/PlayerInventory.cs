using System;
using System.Numerics;
using Bus;
using Gameplay.Controller;
using MyPrint;
using Unity.Netcode;
using UnityEditor;
using UnityEngine;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;

namespace Gameplay.Items
{
    [RequireComponent(typeof(PlayerInventoryReplication))]
    public class PlayerInventory : NetworkBusListener
    {
        public SO_ItemDataBase DataBase => _database;
        
        [Header("References")]
        [SerializeField] private PlayerController _playerController;
        [SerializeField] private PlayerInput _playerInput;
        [SerializeField] private Camera _camera;

        [Header("Pickup / Drop")]
        [SerializeField] private SO_ItemDataBase _database;
        [SerializeField] private Transform _localHand;
        [SerializeField] private Transform _remoteHand;
        
        [Header("Inventory")]
        [SerializeField] private int _itemCount = 3;
        private ItemInstance[] _slots;
        private readonly NetworkVariable<int> _selectedSlot = new(0);
        
        private readonly NetworkVariable<int> _heldItemId = new(-1);
        private ItemCore _currentItem;
        
        //Actions
        public Action<int> OnSetupInventory;
        public Action OnPickupItem;
        public Action OnDropItem;
        public Action<int, int> OnUpdateSlots;
        public Action<bool> OnSlotSelectedChanged;
        
        public override void OnNetworkSpawn()
        {
            _slots = new ItemInstance[Mathf.Max(1, _itemCount)];
            
            OnSetupInventory?.Invoke(_slots.Length);
            
            _heldItemId.OnValueChanged += OnHeldItemChanged;
            _selectedSlot.OnValueChanged += OnSelectedSlotChanged;
                
            if (_heldItemId.Value != -1)
                OnHeldItemChanged(-1, _heldItemId.Value);
            
            if (IsOwner)
            {
                ListenToEvent<OnPickupItem_EVENT>(TryPickupItem);
                _playerInput.OnDropInput += OnDrop;
            }
            
            _playerInput.OnMouseRoll += OnSelectedItemChange;
        }

        public void OnSelectedItemChange(int ratio)
        {
            if (!IsOwner) return;
            AskForChangeSelectedItemRpc(ratio);
        }
        
        [Rpc(SendTo.Server)]
        private void AskForChangeSelectedItemRpc(int ratio)
        {
            int next = ((_selectedSlot.Value + ratio) % _itemCount + _itemCount) % _itemCount;
            SelectSlot(next);
        }
        
        private void SelectSlot(int index)
        {
            _selectedSlot.Value = index;
            RefreshHeldItem();
        }

        private void RefreshHeldItem()
        {
            ItemInstance item = GetCurrentItemInHand();
            int newId = item != null ? item.Data.ID : -1;

            //PublishUses();

            if (_heldItemId.Value == newId && newId != -1)
                OnHeldItemChanged(newId, newId);

            _heldItemId.Value = newId;
        }
        
        public ItemInstance GetCurrentItemInHand() => _slots[_selectedSlot.Value];
        
        private void OnSelectedSlotChanged(int previousValue, int newValue)
        {
            OnSlotSelectedChanged?.Invoke(true);
        }

        private void OnHeldItemChanged(int oldId, int newId)
        {
            if (IsOwner) OnSlotSelectedChanged?.Invoke(true);
            
            if (_currentItem != null)
            {
                UpdateHandWhenDrop();
            }

            if (newId == -1)
                return;
            
            UpdateHandWhenPickup(newId);
        }

        private void UpdateHandWhenPickup(int id)
        {
            OnPickupItem?.Invoke();
            
            SO_Item data = _database.GetItemData(id);

            Transform parent = IsOwner ? _localHand : _remoteHand;

            var go = new GameObject(data.ItemName);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;

            ItemCore core = go.AddComponent<ItemCore>();
            GameObject visual = Instantiate(data.VisualPrefab, core.transform);
            visual.transform.localPosition = Vector3.zero;
            
            ItemInstance instance = NetworkManager.Singleton.IsServer ? GetCurrentItemInHand() : new ItemInstance(data);
            core.SetInstance(instance, _camera, this);
            
            foreach (IPassif passif in instance.Passifs)
            {
                passif.OnStartHolding();
            }
            
            SetCurrentItemInHand(core);
        }

        private void UpdateHandWhenDrop()
        {
            OnDropItem?.Invoke();
            
            foreach (IPassif passif in _currentItem.Instance.Passifs)
            {
                passif.OnStopHolding();
            }
                
            if (IsOwner) 
                _currentItem.UnsubscribeFromInput(_playerInput);

            if (!IsServer) 
                _currentItem.Instance?.Cleanup();
                
            Destroy(_currentItem.gameObject);
            _currentItem = null;
        }

        public void SetCurrentItemInHand(ItemCore core)
        {
            _currentItem = core;
            
            if (IsOwner)
                _currentItem.SubscribeToInput(_playerInput);
        }
        
        private void LateUpdate()
        {
            if (!IsOwner) return;
            
            OnSlotSelectedChanged?.Invoke(false);

            int selected = _selectedSlot.Value;
            int id = _heldItemId.Value;
            
            OnUpdateSlots?.Invoke(selected, id);
        }
        
        private void TryPickupItem(OnPickupItem_EVENT data)
        {
            if (OwnerClientId != data.ClientId) return;

            PickUpRpc(data.ItemPickUpRef);
        }
        
        [Rpc(SendTo.Server)]
        private void PickUpRpc(NetworkObjectReference pickupRef)
        {
            if (!pickupRef.TryGet(out NetworkObject netObj)) return;
            if (!netObj.TryGetComponent(out ItemPickup pickup)) return;
            
            if (pickup.Instance == null) return;

            int slot = FindFreeSlot();
            if (slot == -1) return;
            
            _slots[slot] = pickup.Instance;
            netObj.Despawn();

            SelectSlot(slot);
        }
        
        private int FindFreeSlot()
        {
            if (_slots[_selectedSlot.Value] == null)
                return _selectedSlot.Value;

            for (int i = 0; i < _slots.Length; i++)
                if (_slots[i] == null) return i;

            return -1;
        }
        
        private void OnDrop()
        {
            if (_playerController.IsDown) return;
            
            ItemInstance item = GetCurrentItemInHand();
            if (item == null) return;
            
            Vector3 prefabSize = Vector3.up * (item.Data.PickupPrefab.transform.position.y / 2);
            Vector3 pos = transform.position + transform.forward + prefabSize;
            
            if (Physics.Raycast(_camera.transform.position, _camera.transform.forward, out var hit, 1000, ~0, QueryTriggerInteraction.Ignore))
            {
                pos = hit.point + prefabSize;
            }
            
            if (HasItemInHand())
                DropItemRpc(pos);
        }
        
        public bool HasItemInHand() => _heldItemId.Value != -1;
        
        [Rpc(SendTo.Server)]
        private void DropItemRpc(Vector3 pos)
        {
            ItemInstance item = GetCurrentItemInHand();
            if (item == null) return;
            
            ItemPickup pickup = Instantiate(item.Data.PickupPrefab, pos, transform.rotation);
            pickup.Setup(item);
            pickup.GetComponent<NetworkObject>().Spawn();
                
            ClearCurrentSlot();
        }
        
        public void ClearCurrentSlot()
        {
            _slots[_selectedSlot.Value] = null;
            RefreshHeldItem();
        }
    }
}