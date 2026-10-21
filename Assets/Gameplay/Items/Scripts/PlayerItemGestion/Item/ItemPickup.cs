using System;
using System.Collections.Generic;
using Bus;
using Gameplay.Controller.Minimap;
using Gameplay.Other.SuspiciousSound;
using MyPrint;
using Unity.Netcode;
using Unity.Netcode.Components;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Animations;

namespace Gameplay.Items
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(BoxCollider))]
    [RequireComponent(typeof(NetworkTransform))]
    public class ItemPickup : NetworkBusListener, IInteractable
    {
        [SerializeField] private float _rackRatio = 0.5f;
        private Collider[] _collidersOffOnRack;
        
        private NetworkTransform _nt;
        private Rigidbody _rb;
        private BoxCollider _box;
        readonly NetworkVariable<bool> _onRack = new();
        Vector3 _baseSize, _baseScale;
        
        [SerializeField] private SO_Item _itemData;
        private Outline _outline;
        
        [Header("Minimap")]
        private Vector3 _iconPos = Vector3.up * 5;
        private bool _alreadySeen = false;
        
        public Outline outline => _outline;
        
        private ItemInstance _instance;
        
        public ItemInstance Instance => _instance ??= new ItemInstance(_itemData);
        
        GameObject _minimapIcon = null;
        
        public Action OnCollide;
        
        public override void OnNetworkDespawn()
        {
            _onRack.OnValueChanged -= OnRackChanged;
        }

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
            
            _baseSize = _box.size;
            _baseScale = transform.localScale;

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
            
            ApplyRack();
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

            _rb.isKinematic = on;
            _rb.useGravity = !on;
            foreach (var c in _collidersOffOnRack)
            {
                if (c != null)
                    c.enabled = !on;
            }
            
            _box.size = on ? _baseSize / _rackRatio : _baseSize;
            transform.localScale = on ? _baseScale * _rackRatio : _baseScale;
        }

        public void SetOutline(bool state)
        {
            if (!_alreadySeen)
            {
                _alreadySeen = true;
                CreateSprite();
            }
            
            if(outline)
                outline.enabled = state;
        }

        private void CreateSprite()
        {
            if (_minimapIcon) return;
            
            _minimapIcon = new GameObject("MinimapIcon")
            {
                transform =
                {
                    localPosition = _iconPos + Vector3.up * 10,
                    localRotation = Quaternion.Euler(90, 0, 0)
                }
            };
            
            _minimapIcon.transform.localScale *= 0.4f;
            _minimapIcon.layer = LayerMask.NameToLayer("TargetMiniMap");
                
            _minimapIcon.AddComponent<SpriteRenderer>().sprite = _itemData.Icon;

            _minimapIcon.AddComponent<FreezeRotationMinimap>().SetTarget(transform);
        }

        public void Interact(ulong clientId)
        {
            if (DetectNotGrabbable()) return;
            
            InvokeEvent(new OnPickupItem_EVENT
            {
                ClientId = clientId,
                ItemPickUpRef = NetworkObject
            });
            
            Destroy(_minimapIcon.gameObject);
        }
        
        public void Setup(ItemInstance instance)
        {
            _instance = instance;
            instance?.SetAnchor(transform, NetworkObjectId);
            
            _alreadySeen = true;
            CreateSprite();
        }
        
        public void OnCollisionEnter(Collision collision)
        {
            if(_instance == null)
                return;
            
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
    }
}