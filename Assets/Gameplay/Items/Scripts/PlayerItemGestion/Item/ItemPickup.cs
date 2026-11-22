using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using Bus;
using Gameplay.Controller.Minimap;
using Gameplay.Items.PickingUp;
using Gameplay.Other.SuspiciousSound;
using JetBrains.Annotations;
using MyPrint;
using Unity.Netcode;
using Unity.Netcode.Components;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Animations;

namespace Gameplay.Items
{
    [RequireComponent(typeof(BoxCollider))]
    [RequireComponent(typeof(NetworkTransform))]
    public class ItemPickup : NetworkBusListener, IInteractable
    {
        //Inspector
        [SerializeField] private SO_Item _itemData;
        [SerializeField] private float _rackRatio = 0.5f;

        //Components
        private NetworkTransform _nt;
        private Rigidbody _rb;
        private BoxCollider _box;
        private Outline _outline;

        //Network
        private readonly NetworkVariable<bool> _onRack = new();

        //Cached Data
        private Collider[] _collidersOffOnRack;
        private Vector3 _baseSize;
        private Vector3 _baseSizeRack;
        private Vector3 _baseScale;

        //States
        private ItemInstance _instance;
        private bool _canCollide;
        private readonly NetworkVariable<ulong> _networkInstanceId = new();
        public readonly NetworkVariable<ulong> ThrownBy = new(ulong.MaxValue);
        
        //Hover
        private IHoverItemInfo _hoverInfo;

        //Properties
        public Outline outline => _outline;
        public ItemInstance Instance => _instance ??= new ItemInstance(_itemData);

        //Actions
        public Action OnCollide;

        private void Awake()
        {
            _outline = GetComponentInChildren<Outline>();
            _rb = GetComponent<Rigidbody>();
            _box = GetComponent<BoxCollider>();
            _nt = GetComponent<NetworkTransform>();
            
            _nt.InLocalSpace = true;
            _nt.SyncScaleX = false;
            _nt.SyncScaleY = false;
            _nt.SyncScaleZ = false;
            
            _baseSizeRack = new Vector3(_box.size.x, _box.size.y / 2, _box.size.z);
            _baseSize = new Vector3(_box.size.x, _box.size.y, _box.size.z);
            _baseScale = transform.localScale;

            _hoverInfo = GetComponent<IHoverItemInfo>();

            if (_collidersOffOnRack == null || _collidersOffOnRack.Length == 0)
            {
                List<Collider> list = new List<Collider>();
                foreach (Collider c in GetComponentsInChildren<Collider>(true))
                    if (c != _box) list.Add(c);
                _collidersOffOnRack = list.ToArray();
            }
        }
        
        public override void OnNetworkSpawn()
        {
            _onRack.OnValueChanged += OnRackChanged;

            if (IsServer)
            {
                if (_instance == null)
                    _instance = new ItemInstance(_itemData);

                _networkInstanceId.Value = _instance.InstanceId;
                
                SyncByteModulesToClients();

                StartCoroutine(BufferCollision());

                if (_rb)
                {
                    _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                    _rb.interpolation = RigidbodyInterpolation.Interpolate;
                }
            }
            else
            {
                _networkInstanceId.OnValueChanged += OnInstanceIdChanged;

                if (_networkInstanceId.Value != 0)
                    OnInstanceIdChanged(0, _networkInstanceId.Value);
            }
            
            ApplyRack();
            
            if (ThrownBy.Value == NetworkManager.LocalClientId)
            {
                ThrowGhost.TryClaim(this);
            }
        }
        
        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();

            _onRack.OnValueChanged -= OnRackChanged;
            _networkInstanceId.OnValueChanged -= OnInstanceIdChanged;
        }
        
        private void OnInstanceIdChanged(ulong previous, ulong current)
        {
            if (current == 0) return;

            Instance.SetInstanceId(current);
        }

        private IEnumerator BufferCollision()
        {
            _canCollide = false;

            yield return new WaitForSeconds(0.1f);

            _canCollide = true;
        }

        private void OnRackChanged(bool previous, bool current) => ApplyRack();

        public void SetOnRack()
        {
            if (!IsServer) return;
            _onRack.Value = true;
            ApplyRack();
        }

        void ApplyRack()
        {
            bool on = _onRack.Value;

            if (on)
            {
                if (_rb != null)
                {
                    Destroy(_rb);
                    _rb = null;
                }
            }
            else
            {
                if (_rb == null)
                    _rb = gameObject.AddComponent<Rigidbody>();

                _rb.isKinematic = !IsServer;
                _rb.useGravity = true;
            }

            foreach (var c in _collidersOffOnRack)
            {
                if (c != null)
                    c.enabled = !on;
            }

            _box.size = on ? new Vector3(_baseSizeRack.x / _rackRatio, 
                (_baseSizeRack.y / _rackRatio) / 2,
                _baseSizeRack.z / _rackRatio) : _baseSize;
            transform.localScale = on ? _baseScale * _rackRatio : _baseScale;
        }

        public void SetOutline(StartOutliningData data)
        {
            if(outline)
                outline.enabled = data.State;

            if (_hoverInfo != null)
            {
                _hoverInfo.OnHoverUpdate(data.State);
            }
        }


        public void Interact(ulong clientId)
        {
            if (DetectNotGrabbable())
                return;

            if (NetworkObject == null || !NetworkObject.IsSpawned)
            {
                return;
            }

            InvokeEvent(new OnPickupItem_EVENT
            {
                ClientId = clientId,
                ItemPickUpRef = new NetworkObjectReference(NetworkObject)
            });
        }

        
        public void Setup(ItemInstance instance)
        {
            _instance = instance;
            instance?.SetAnchor(transform, NetworkObjectId);
            
            GetComponent<BoxCollider>().isTrigger = true;
            
            if (IsServer && IsSpawned)
                SyncByteModulesToClients();
        }
        
        public void OnCollisionEnter(Collision collision)
        {
            if(_instance == null) return;
            if(!_canCollide)return;
            
            OnCollide?.Invoke();
            
            foreach (var passif in _instance.Passifs)
            {
                passif.OnCollide(collision, this);
            }
        }

        bool DetectNotGrabbable()
        {
            if (Instance != null)
            {
                foreach (IPassif passif in _instance.Passifs)
                {
                    if (passif is NotGrabbableModule)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public void CreateSuspiciousSound(ulong clientId, float force)
        {
            if (!IsServer)
            {
                CreateSuspiciousSoundRpc(clientId, force);
                return;
            }
            
            InvokeEvent(new OnCreateSuspiciousSound_EVENT
            {
                FromClientID = clientId,
                Force = force,
                Position = transform.position
            });
        }
        
        [Rpc(SendTo.Server)]
        void CreateSuspiciousSoundRpc(ulong clientId, float force) => CreateSuspiciousSound(clientId, force);
        
        public Tooltip GetTooltip(ItemModule[] playerModules)
        {
            Tooltip tooltip = new Tooltip
            {
                Message = string.Empty,
                Icon = Instance.Data.TooltipInHand.DefaultTooltip.Icon
            };
            
            if(playerModules != null)
            {
                tooltip = Instance.Data.TooltipInHand.GetTooltip(playerModules);
            }
            
            if (tooltip.Message.Equals(string.Empty))
            {
                tooltip.Message = "E to pick " + Instance.Data.ItemName;
            }
            
            return tooltip;
        }
        
        public void SyncByteModulesToClients()
        {
            if (!IsServer || !IsSpawned || _instance == null)
                return;

            int index = 0;

            foreach (var module in _instance.AllModules())
            {
                if (module is IReplicatedByteModule byteModule)
                {
                    SyncByteModuleRpc(index, byteModule.GetByteState());
                }

                index++;
            }
        }
        
        [Rpc(SendTo.ClientsAndHost)]
        private void SyncByteModuleRpc(int moduleIndex, byte[] data)
        {
            if (data == null)
                return;

            int index = 0;

            foreach (var module in Instance.AllModules())
            {
                if (index == moduleIndex)
                {
                    if (module is IReplicatedByteModule byteModule)
                        byteModule.SetByteState(data);

                    return;
                }

                index++;
            }
        }
    }
}