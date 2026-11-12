using System;
using System.Numerics;
using Bus;
using Gameplay.Controller;
using MyPrint;
using Unity.Netcode;
using Unity.VisualScripting;
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
        public PlayerController Controller => _playerController;
        
        [Header("References")]
        [SerializeField] private PlayerController _playerController;
        [SerializeField] private PlayerInput _playerInput;
        [SerializeField] private Camera _camera;

        [Header("Pickup / Drop")]
        [SerializeField] private SO_ItemDataBase _database;
        [SerializeField] private Transform _localHand;
        [SerializeField] private Transform _remoteHand;
        [SerializeField] private float _dropRange = 2;
        
        [Header("Inventory")]
        [SerializeField] private int _itemCount = 3;
        private ItemInstance[] _slots;
        private readonly NetworkVariable<int> _selectedSlot = new(0);

        private readonly NetworkList<int> _slotIds = new();
        
        [Header("SFX")] 
        [SerializeField, SoundName] private string _sfxPickUp;
        [SerializeField, SoundName] private string _sfxDrop;
        [SerializeField, Range(0, 1)] private float _volume = 0.5f;
        
        [Header("Dropping")]
        [SerializeField] private Material _previewMaterial;
        private bool _isPreviewDrop = false;
        private Vector3 _dropPos;
        private Quaternion _dropRot;
        private float _dropAngle = 0f;
        private GameObject _previewVisual;
        private float _previewBottomOffset;
        
        private PlayerInventoryReplication _replication;
        private readonly NetworkVariable<int> _heldItemId = new(-1);
        private ItemCore _currentItem;
        
        public ItemInstance GetHeldInstance() => _currentItem != null ? _currentItem.Instance : null;
        
        public int SlotCount => _slotIds.Count;
        public int SelectedSlot => _selectedSlot.Value;
        public int GetSlotItemId(int index) => index >= 0 && index < _slotIds.Count ? _slotIds[index] : -1;
        
        //Actions
        public Action<int> OnSetupInventory;
        public Action OnPickupItem;
        public Action OnDropItem;
        public Action<int, int> OnUpdateSlots;
        public Action<bool> OnSlotSelectedChanged;
        public Action<bool, string> OnTooltipChange;
        
        public Action OnSlotsChanged;
        
        public static event Action<PlayerInventory> OnInventorySpawned;
        public static event Action<PlayerInventory> OnInventoryDespawned;
        
        #region Net Spawn/Despawn
        
        public override void OnNetworkSpawn()
        {
            _replication = GetComponent<PlayerInventoryReplication>();
            
            _slots = new ItemInstance[Mathf.Max(1, _itemCount)];
            
            if (IsServer)
            {
                _slotIds.Clear();
                for (int i = 0; i < _slots.Length; i++)
                    _slotIds.Add(-1);
            }
            
            _slotIds.OnListChanged += OnSlotIdsChanged;
            OnSetupInventory?.Invoke(_slots.Length);
            
            _heldItemId.OnValueChanged += OnHeldItemChanged;
            _selectedSlot.OnValueChanged += OnSelectedSlotChanged;
                
            if (_heldItemId.Value != -1)
                OnHeldItemChanged(-1, _heldItemId.Value);
            
            if (IsOwner)
            {
                ListenToEvent<OnPickupItem_EVENT>(TryPickupItem);
                _playerInput.OnDropInput += OnDropPreviewStart;
                _playerInput.OnDropInputCanceled += OnPerformDrop;
            }
            
            _playerInput.OnMouseRoll += OnSelectedItemChange;
            
            OnInventorySpawned?.Invoke(this);
        }
        
        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            
            _slotIds.OnListChanged -= OnSlotIdsChanged;
            OnInventoryDespawned?.Invoke(this);
        }
        
        #endregion
        
        #region Change Current Item Slot
        
        private void SetCurrentItemInHand(ItemCore core)
        {
            _currentItem = core;
            
            if (IsOwner)
            {
                _currentItem.SubscribeToInput(_playerInput);

                bool isItem = _currentItem.Instance.Data.TooltipInHand.IsItem;
                
                OnTooltipChange?.Invoke(isItem, _currentItem.Instance.Data.TooltipInHand.TooltipInHand);
            }
        }
        
        private void OnSlotIdsChanged(NetworkListEvent<int> changeEvent)
        {
            OnSlotsChanged?.Invoke();
        }

        public void OnSelectedItemChange(int ratio)
        {
            if (!IsOwner) return;

            if (!_isPreviewDrop)
            {
                AskForChangeSelectedItemRpc(ratio);
            }
            else
            {
                UpdateDropRotate();
            }
        }

        [Rpc(SendTo.Server)]
        private void AskForChangeSelectedItemRpc(int ratio)
        {
            int next = ((_selectedSlot.Value + ratio) % _itemCount + _itemCount) % _itemCount;
            SelectSlot(next);
        }
        
        private void SelectSlot(int index)
        {
            if (index != _selectedSlot.Value)
            {
                ItemInstance previous = GetCurrentItemInHand();

                NotifyNotInHand(previous);
                
                if (previous != null)
                {
                    UnableCondition(previous);
                }
            }
            
            _selectedSlot.Value = index;
            RefreshHeldItem();
        }
        
        private void SetSlot(int index, ItemInstance item)
        {
            _slots[index] = item;
            
            if (IsServer && index < _slotIds.Count)
                _slotIds[index] = item != null ? item.Data.ID : -1;
        }

        private void RefreshHeldItem()
        {
            ItemInstance item = GetCurrentItemInHand();
            int newId = item != null ? item.Data.ID : -1;
            
            _replication.PublishHeldItem(item);
            
            if (_heldItemId.Value == newId && newId != -1)
                OnHeldItemChanged(newId, newId);

            _heldItemId.Value = newId;
        }
        
        public ItemInstance GetCurrentItemInHand() => _slots[_selectedSlot.Value];
        
        private void OnSelectedSlotChanged(int previousValue, int newValue)
        {
            OnSlotSelectedChanged?.Invoke(true);
            OnSlotsChanged?.Invoke();
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

        #endregion
        
        #region Pickup Item

        private void TryPickupItem(OnPickupItem_EVENT data)
        {
            if (OwnerClientId != data.ClientId) return;

            PickUpRpc(data.ItemPickUpRef);
        }

        public void TryAddItem(ItemInstance instance)
        {
            int slot = FindFreeSlot();
            if (slot == -1) return;
            
            SetSlot(slot, instance);

            SelectSlot(slot);
        }
        
        [Rpc(SendTo.Server)]
        private void PickUpRpc(NetworkObjectReference pickupRef)
        {
            if (!pickupRef.TryGet(out NetworkObject netObj)) return;
            if (!netObj.TryGetComponent(out ItemPickup pickup)) return;
            
            if (pickup.Instance == null) return;
            
            //SFX
            InvokeEvent(new PlaySoundEvent
            {
                position = transform.position,
                volume = _volume,
                soundName = _sfxPickUp
            });
            
            int slot = FindFreeSlot();
            if (slot == -1) return;
            
            SetSlot(slot, pickup.Instance);
            netObj.Despawn();

            SelectSlot(slot);
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
            visual.transform.localPosition = data.OffsetInHand;

            if (IsOwner)
            {
                visual.gameObject.layer = LayerMask.NameToLayer("ItemInHand");
                
                foreach (Transform child in visual.transform)
                {
                    child.gameObject.layer = LayerMask.NameToLayer("ItemInHand");
                }
            }
            
            ItemInstance instance;
            if (NetworkManager.Singleton.IsServer)
            {
                instance = GetCurrentItemInHand();
            }
            else
            {
                instance = new ItemInstance(data);
                _replication.ApplyTo(instance);
            }
            
            core.SetInstance(instance, _camera, this);
            
            foreach (IPassif passif in instance.Passifs)
            {
                passif.OnStartHolding();
            }
            
            SetCurrentItemInHand(core);
        }
        
        private int FindFreeSlot()
        {
            if (_slots[_selectedSlot.Value] == null)
                return _selectedSlot.Value;

            for (int i = 0; i < _slots.Length; i++)
                if (_slots[i] == null) return i;

            return -1;
        }
        
        #endregion

        #region  Drop Item
        
        private void OnDropPreviewStart()
        {
            ItemInstance item = GetCurrentItemInHand();
            if (item == null) return;

            _isPreviewDrop = true;
            _dropPos = transform.position;

            _previewVisual = Instantiate(item.Data.VisualPrefab);
            foreach (var r in _previewVisual.GetComponentsInChildren<Renderer>())
                r.sharedMaterial = _previewMaterial;

            _previewBottomOffset = GetBottomOffset(_previewVisual);
        }
        
        private void UpdatePreviewDrop()
        {
            if (_previewVisual == null) return;

            if (Physics.Raycast(_camera.transform.position, _camera.transform.forward,
                    out RaycastHit hit, _dropRange, ~0, QueryTriggerInteraction.Ignore))
            {
                _dropPos = hit.point;
            }

            _previewVisual.transform.SetPositionAndRotation(_dropPos + Vector3.up * (_previewBottomOffset + 0.01f), _dropRot);
        }
        
        private void UpdateDropRotate()
        {
            _dropAngle = (_dropAngle + 90f) % 360f;
            _dropRot = Quaternion.Euler(0f, _dropAngle, 0f);
        }
        
        private void OnPerformDrop()
        {
            _isPreviewDrop = false;

            if (_previewVisual != null)
            {
                Destroy(_previewVisual);
                _previewVisual = null;
            }

            if (HasItemInHand())
                DropItemRpc(_dropPos, _dropRot);
        }
        
        [Rpc(SendTo.Server)]
        private void DropItemRpc(Vector3 groundPoint, Quaternion rot)
        {
            ItemInstance item = GetCurrentItemInHand();
            if (item == null) return;

            float maxDist = _dropRange + 2f;
            if ((groundPoint - transform.position).sqrMagnitude > maxDist * maxDist) return;

            ItemPickup pickup = Instantiate(item.Data.PickupPrefab, groundPoint, rot);
            Physics.SyncTransforms();

            float offset = GetBottomOffset(pickup.gameObject);
            pickup.transform.position = groundPoint + Vector3.up * (offset + 0.01f);

            pickup.GetComponent<NetworkObject>().Spawn();
            pickup.Setup(item);

            InvokeEvent(new PlaySoundEvent
            {
                position = transform.position,
                volume = _volume,
                soundName = _sfxDrop
            });

            ClearCurrentSlot();
        }
        
        private void UpdateHandWhenDrop()
        {
            OnDropItem?.Invoke();
            
            foreach (IPassif passif in _currentItem.Instance.Passifs)
            {
                passif?.OnStopHolding();
            }
            
            if (IsOwner)
            {
                _currentItem.UnsubscribeFromInput(_playerInput);
                OnTooltipChange?.Invoke(false, null);
            }

            if (!IsServer)
                _currentItem.Instance?.Cleanup();
                
            Destroy(_currentItem.gameObject);
            _currentItem = null;
        }
        
        private float GetBottomOffset(GameObject go)
        {
            Bounds bounds = default;
            bool has = false;

            foreach (Collider c in go.GetComponentsInChildren<Collider>())
            {
                if (c.isTrigger) 
                    continue;
                
                if (!has)
                {
                    bounds = c.bounds;
                    has = true;
                }
                else 
                    bounds.Encapsulate(c.bounds);
            }

            if (!has)
            {
                foreach (Renderer r in go.GetComponentsInChildren<Renderer>())
                {
                    if (!has)
                    {
                        bounds = r.bounds; 
                        has = true;
                    }
                    else 
                        bounds.Encapsulate(r.bounds);
                }
            }

            return has ? go.transform.position.y - bounds.min.y : 0f;
        }
        
        #endregion

        #region Clear Slot
        
        public void ClearCurrentSlot()
        {
            ItemInstance item = GetCurrentItemInHand();
            if (item == null) return;
            
            UnableCondition(item);

            SetSlot(_selectedSlot.Value, null);
            RefreshHeldItem();
        }

        private void UnableCondition(ItemInstance item)
        {
            foreach (IFirstAction f in item.FirstActionList)
                f?.Condition?.DisableItemInHand();
            
            foreach (ISecondAction s in item.SecondActionList)
                s?.Condition?.DisableItemInHand();

        }
        
        public void DestroyItemInHand()
        {
            if (!IsServer)
            {
                DestroyItemInHandRpc();
                return;
            }

            ItemInstance item = GetCurrentItemInHand();
            if (item == null) return;
            
            item.Cleanup();

            ClearCurrentSlot();
        }

        [Rpc(SendTo.Server)]
        private void DestroyItemInHandRpc() => DestroyItemInHand();
        
        private void NotifyNotInHand(ItemInstance item)
        {
            if (item == null) return;

            foreach (IFirstAction f in item.FirstActionList)
                f?.OnNotInHand();

            foreach (ISecondAction s in item.SecondActionList)
                s?.OnNotInHand();
        }
        
        #endregion
        
        private bool HasItemInHand() => _heldItemId.Value != -1;
        
        public void ApplyReplicatedStates()
        {
            if (IsServer || _currentItem == null) return;
            _replication.ApplyTo(_currentItem.Instance);
        }
        
        private void LateUpdate()
        {
            if (!IsOwner) return;
            
            OnSlotSelectedChanged?.Invoke(false);

            int selected = _selectedSlot.Value;
            int id = _heldItemId.Value;
            
            OnUpdateSlots?.Invoke(selected, id);

            if (_isPreviewDrop)
            {
                UpdatePreviewDrop();
            }
        }
    }
}