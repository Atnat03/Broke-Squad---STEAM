using Assets.Gameplay.LD.Scripts;
using Bus;
using Gameplay.Controller;
using Gameplay.Items.Scripts;
using Gameplay.Items.Scripts.ItemData;
using Gameplay.Items.Scripts.ItemModules.ConcreteModules;
using Gameplay.Items.Scripts.PlayerItemGestion;
using Gameplay.LD.Scripts;
using System;
using System.Collections.Generic;
using System.Linq;
using Gameplay.Items.Scripts.ItemModules;
using MyPrint;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering;
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
        [SerializeField] private PlayerController _playerController;

        [Header("Picking")]
        [SerializeField] private Camera _camera;
        [SerializeField] private float _range;
        [SerializeField] private LayerMask _layerMask;

        [Header("Inventory")]
        [SerializeField] private int _itemCount = 3;
        private ItemInstance[] _slots;
        private readonly NetworkVariable<int> _selectedSlot = new(0);
        
        [SerializeField] private ItemSlotUI _slotPrefabUI;
        [SerializeField] private Transform _parentInventory;
        [SerializeField] private Sprite _emptyIcon;
        private ItemSlotUI[] _slotsUIList;
        private bool _uiDirty;
        
        [Header("UI")]
        [Header("Throw")]
        [SerializeField] private GameObject _throwUI;
        [SerializeField] private Image _throwImage;

        [Header("Electic")]
        [SerializeField] private GameObject _electicUI;
        [SerializeField] private TextMeshProUGUI _electicPercent;
        
        [Header("Use")]
        [SerializeField] private TextMeshProUGUI _useAmountText;

        [Header("Audio")] 
        [SerializeField, SoundName] private string _pickUpSFX;

        private readonly NetworkVariable<int> _heldItemId = new(-1);
        private readonly NetworkVariable<float> _electricPercent = new(0f);
        
        private readonly NetworkVariable<Vector2Int> _uses = new(new Vector2Int(-1, -1));
        public Vector2Int Uses => _uses.Value;
        
        private PlayerInput _input;
        private ItemCore _currentItem;
        private Outliner _currentHoverItem = null;
        
        public override void OnNetworkSpawn()
        {
            if(_playerController ==null) _playerController.GetComponent<PlayerController>();
            _slots = new ItemInstance[Mathf.Max(1, _itemCount)];
            _slotsUIList = new ItemSlotUI[_slots.Length];
            _input = GetComponent<PlayerInput>();

            _heldItemId.OnValueChanged += OnHeldItemChanged;
            _electricPercent.OnValueChanged += OnElectricPercentChanged;

            _selectedSlot.OnValueChanged += OnSelectedSlotChanged;
            
            _uses.OnValueChanged += OnUsesChanged;

            if (_heldItemId.Value != -1)
                OnHeldItemChanged(-1, _heldItemId.Value);

            if (IsOwner)
            {
                _input.OnInteractInput += OnInteract;
                _input.OnInteractInput += CheckOpenDoor;
                
                _input.OnDropInput += OnDrop;
                
                for (int i = 0; i < _slotsUIList.Length; i++)
                {
                    _slotsUIList[i] = Instantiate(_slotPrefabUI, _parentInventory);
                    _slotsUIList[i].SetIcon(_emptyIcon);
                }

                _uiDirty = true;
            }

            EnableBar(false);
            EnableElectricInfo(false);
            EnableUseText(false);

            _input.OnMouseRoll += OnSelectedItemChange;
        }

        public override void OnNetworkDespawn()
        {
            _heldItemId.OnValueChanged -= OnHeldItemChanged;
            _electricPercent.OnValueChanged -= OnElectricPercentChanged;
            _input.OnMouseRoll -= OnSelectedItemChange;
            
            _selectedSlot.OnValueChanged -= OnSelectedSlotChanged;

            _uses.OnValueChanged -= OnUsesChanged;

            if (_input != null)
            {
                if (IsOwner)
                {
                    _input.OnInteractInput -= CheckOpenDoor;
                    _input.OnInteractInput -= OnInteract;
                    _input.OnDropInput -= OnDrop;
                }
            }
        }

        private void OnInteract()
        {
            if (_playerController.IsDown) return;

            if (Physics.Raycast(_camera.transform.position, _camera.transform.forward,
                    out RaycastHit hit, _range, _layerMask, QueryTriggerInteraction.Ignore))
            {
                if (hit.transform.TryGetComponent(out ItemPickup pickup) &&
                    pickup.TryGetComponent(out NetworkObject netObj))
                {
                    PickUpRpc(netObj);
                    return;
                }

                IInteractable interactable = hit.collider.GetComponentInParent<IInteractable>();
                if (interactable != null)
                    interactable.Interact();
            }
        }

        private void OnDrop()
        {
            if (_playerController.IsDown) return;

            if (HasItemInHand())
                DropItemRpc();
        }
        
        private void CheckOpenDoor()
        {
            if (Physics.Raycast(_camera.transform.position, _camera.transform.forward,
                    out RaycastHit hit, _range, _layerMask, QueryTriggerInteraction.Ignore))
            {
                Door door = hit.collider.GetComponentInParent<Door>();
                if (door == null) return;

                if (!door.CanOpenWithoutKey) return;

                if (door.TryGetComponent(out NetworkObject netObj))
                    ToggleDoorRpc(netObj);
            }
        }

        [Rpc(SendTo.Server)]
        private void ToggleDoorRpc(NetworkObjectReference doorRef)
        {
            if (!doorRef.TryGet(out NetworkObject netObj)) return;
            if (!netObj.TryGetComponent(out Door door)) return;
            if (Vector3.Distance(transform.position, netObj.transform.position) > _range + 1.5f) return;

            door.TryOpen(new Vector2Int(-1, -1));
        }
        
        private void Update()
        {
            if (!IsOwner || _camera == null) return;

            Outliner target = null;

            if (Physics.Raycast(_camera.transform.position, _camera.transform.forward,
                    out RaycastHit hit, _range, _layerMask, QueryTriggerInteraction.Ignore))
            {
                target = hit.collider.GetComponentInParent<Outliner>();

                if (target != null && _parent != null && target.transform.IsChildOf(_parent))
                    target = null;
            }

            if (target == _currentHoverItem) return;

            if (_currentHoverItem != null)
                _currentHoverItem.SetOutline(false);

            _currentHoverItem = target;

            if (_currentHoverItem != null)
                _currentHoverItem.SetOutline(true);
        }

        [Rpc(SendTo.Server)]
        private void PickUpRpc(NetworkObjectReference pickupRef)
        {
            if (!pickupRef.TryGet(out NetworkObject netObj)) return;
            if (!netObj.TryGetComponent(out ItemPickup pickup)) return;
            if (pickup.Instance == null) return;
            if (Vector3.Distance(transform.position, netObj.transform.position) > _range + 1.5f) return;

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


        [Rpc(SendTo.Server)]
        private void DropItemRpc()
        {
            ItemInstance item = GetCurrentItemInHand();
            if (item == null) return;
            
            Vector3 pos = _parent.position + transform.forward * 0.5f;
            ItemPickup pickup = Instantiate(item.Data.pickUpPrefab, pos, transform.rotation);
            pickup.Setup(item);
            pickup.GetComponent<NetworkObject>().Spawn();

            ClearCurrentSlot();
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

            _currentItem?.ThrowItem();
            item.Cleanup();

            ClearCurrentSlot();
        }

        [Rpc(SendTo.Server)]
        private void DestroyItemInHandRpc() => DestroyItemInHand();

        private void OnHeldItemChanged(int oldId, int newId)
        {
            if (IsOwner) _uiDirty = true;
            
            if (_currentItem != null)
            {
                foreach (IPassif passif in _currentItem.Instance.Passifs)
                {
                    passif.OnStopHolding();
                }
                
                if (IsOwner) 
                    _currentItem.UnsubscribeFromInput(_input);

                if (!IsServer) 
                    _currentItem.Instance?.Cleanup();
                

                EnableBar(false);
                EnableElectricInfo(false);
                EnableUseText(false);
                
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

            ItemInstance instance = IsServer ? GetCurrentItemInHand() : new ItemInstance(data);
            core.SetInstance(instance, this);
            
            foreach (IPassif passif in instance.Passifs)
            {
                passif.OnStartHolding();
            }
            
            if (IsOwner) 
                UpdateElectricInfo(_electricPercent.Value); 

            _currentItem = core;
            if (IsOwner) 
                _currentItem.SubscribeToInput(_input);
        }
        
        private void OnUsesChanged(Vector2Int prev, Vector2Int cur)
        {
            if (IsOwner && cur.y > 0)
                UpdateTextUse(cur.x + "/" + cur.y);
        }

        public void RequestUseItem() => UseItemRpc();

        [Rpc(SendTo.Server)]
        private void UseItemRpc()
        {
            ItemInstance item = GetCurrentItemInHand();
            if (item == null) return;

            NumberOfUse module = item.Conditions.OfType<NumberOfUse>().FirstOrDefault();
            if (module == null || !module.Consume()) return;

            PublishUses();

            if (module.ShouldDestroy)
                DestroyItemInHand();
        }

        private void PublishUses()
        {
            NumberOfUse m = GetCurrentItemInHand()?.Conditions.OfType<NumberOfUse>().FirstOrDefault();
            _uses.Value = m != null ? new Vector2Int(m.CurrentUse, m.MaxUse) : new Vector2Int(-1, -1);
        }
        
        private void LateUpdate()
        {
            if (!IsOwner || !_uiDirty || _slotsUIList == null) return;
            _uiDirty = false;

            int selected = _selectedSlot.Value;
            if (selected < 0 || selected >= _slotsUIList.Length) return;

            for (int i = 0; i < _slotsUIList.Length; i++)
                _slotsUIList[i].SetSelectedImage(i == selected);

            Sprite icon = _emptyIcon;

            int id = _heldItemId.Value;
            if (id != -1)
            {
                SO_Item data = _database.GetItem(id);
                if (data != null && data.icon != null)
                    icon = data.icon;
                else
                    Debug.LogWarning($"L'item {id} n'a pas d'icône assignée.", this);
            }
            
            _slotsUIList[selected].SetIcon(icon);
        }

        public void RequestThrow(float charge01, Vector3 camPos, Quaternion camRot)
        {
            //Audio
            InvokeEvent(new PlaySoundEvent
            {
                soundName = _pickUpSFX,
                position = transform.position,
                volume = 0.5f,
            });
            
            ThrowRpc(charge01, camPos, camRot);
        }
           

        [Rpc(SendTo.Server)]
        private void ThrowRpc(float charge01, Vector3 camPos, Quaternion camRot)
        {
            if (GetCurrentItemInHand() == null) return;

            ThrowModule module = GetCurrentItemInHand().RightClicks.OfType<ThrowModule>().FirstOrDefault();
            if (module == null) return;

            if (Vector3.Distance(camPos, transform.position) > 3f)
                camPos = _parent.position;

            Vector3 spawnPos = camPos + camRot * Vector3.forward * module.SpawnDistance;

            ItemPickup thrown = ServerSpawnPickup(spawnPos, camRot);
            module.ApplyThrow(thrown, charge01, camRot);
        }


        private ItemPickup ServerSpawnPickup(Vector3 pos, Quaternion rot)
        {
            ItemInstance item = GetCurrentItemInHand();
            if (item == null || item.Data.pickUpPrefab == null)
                return null;

            ItemPickup pickup = Instantiate(item.Data.pickUpPrefab, pos, rot);
            pickup.Setup(item);
            pickup.GetComponent<NetworkObject>().Spawn();

            ClearCurrentSlot();
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
            if (GetCurrentItemInHand() == null) return;

            ChargeItemModule module = GetCurrentItemInHand().LeftClicks.OfType<ChargeItemModule>().FirstOrDefault();
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

        public void EnableUseText(bool state)
        {
            if (IsOwner) _useAmountText.transform.parent.gameObject.SetActive(state);
        }

        public void UpdateTextUse(string str) {if(IsOwner)  _useAmountText.text = str; }
        
        public void RequestOpenDoor(NetworkObjectReference door) => OpenDoorRpc(door);

        [Rpc(SendTo.Server)]
        private void OpenDoorRpc(NetworkObjectReference doorRef)
        {
            if (GetCurrentItemInHand() == null) return;

            OpenDoorModule module = GetCurrentItemInHand().LeftClicks.OfType<OpenDoorModule>().FirstOrDefault();
            if (module == null) return;

            if (!doorRef.TryGet(out NetworkObject netObj)) return;
            if (!netObj.TryGetComponent(out Door door)) return;

            if (Vector3.Distance(transform.position, netObj.transform.position) > module.Range + 1.5f) return;

            if (module.ApplyOpen(door))
            { }
        }
        
        #region Inventory

        public void RequestHeal(NetworkObjectReference target) => HealRpc(target);

        [Rpc(SendTo.Server)]
        private void HealRpc(NetworkObjectReference targetRef)
        {
            if (GetCurrentItemInHand() == null) return;

            HealModule module = GetCurrentItemInHand().LeftClicks.OfType<HealModule>().FirstOrDefault();
            if (module == null) return;

            NumberOfUse uses = GetCurrentItemInHand().Conditions.OfType<NumberOfUse>().FirstOrDefault();
            if (uses != null && uses.CurrentUse <= 0) return;

            if (!targetRef.TryGet(out NetworkObject netObj)) return;
            if (!netObj.TryGetComponent(out PlayerData.PlayerHealth target)) return;

            if (Vector3.Distance(transform.position, netObj.transform.position) > module.Range + 1.5f) return;

            module.ApplyHeal(target);
        }
        
        private ItemInstance GetCurrentItemInHand()
            => _slots[_selectedSlot.Value];

        private void ClearCurrentSlot()
        {
            _slots[_selectedSlot.Value] = null;
            RefreshHeldItem();
        }

        private void SelectSlot(int index)
        {
            _selectedSlot.Value = index;
            RefreshHeldItem();
        }

        private void RefreshHeldItem()
        {
            ItemInstance item = GetCurrentItemInHand();
            int newId = item != null ? item.Data.id : -1;

            PublishUses();

            if (_heldItemId.Value == newId && newId != -1)
                OnHeldItemChanged(newId, newId);

            _heldItemId.Value = newId;
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

        public void SelectSlotDirect(int index)
        {
            if (!IsOwner || index < 0 || index >= _itemCount) return;
            SelectSlotRpc(index);
        }

        [Rpc(SendTo.Server)]
        private void SelectSlotRpc(int index)
        {
            if (index < 0 || index >= _itemCount) return;
            SelectSlot(index);
        }
        
        private void OnSelectedSlotChanged(int prev, int cur) => _uiDirty = true;

        public void TryPlaySound(string sound)
        {
            if (!IsServer)
            {
                AskServerToPlaySoundRpc(sound);
                return;
            }
            
            InvokeEvent(new PlaySoundEvent
                {
                soundName = sound,
                position = transform.position,
                volume = 0.25f,
            });
        }

        [Rpc(SendTo.Server)]
        private void AskServerToPlaySoundRpc(string sound) => TryPlaySound(sound);

        #endregion

        #region Goal

        public void TryNotifyGoal(bool hasGoal)
        {
            if (!IsServer)
            {
                TryNotifyGoalServerRpc(hasGoal);
                return;
            }
            
            TryNotifyGoalClientRpc(hasGoal);
        }

        [Rpc(SendTo.Server)]
        private void TryNotifyGoalServerRpc(bool hasGoal)
        {
            TryNotifyGoal(hasGoal);
        }
        
        [Rpc(SendTo.Everyone)]
        private void TryNotifyGoalClientRpc(bool hasGoal)
        {
            if(hasGoal)
                InvokeEvent(new OnGrabGoal());
            else
            {
                InvokeEvent(new OnDropGoal());
            }
        }

        #endregion
    }
}