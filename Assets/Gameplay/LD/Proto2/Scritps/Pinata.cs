using System;
using System.Runtime.InteropServices.WindowsRuntime;
using Gameplay.Items;
using Gameplay.LD.Scripts;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using Random = UnityEngine.Random;

namespace Gameplay.LD.Proto2
{
    public class Pinata : NetworkBehaviour, IDamageable
    {
        [SerializeField] private float _baseHp = 50;
        [SerializeField] private UnityEvent _hitEvent;
        [SerializeField] private float _randomForceRange = 2; 

        [Header("Drop")]
        [SerializeField] private UnityEvent _dropEvent;
        [SerializeField] private ItemPickup _itemPickupDrop;
        
        [Header("Rope")]
        [SerializeField] private float _ropeLength = 0.5f;
        [SerializeField] private LineRenderer _rope;
        [SerializeField] private Transform _ropePositionStart;

        private readonly NetworkVariable<float> _currentHP = new();

        private ConfigurableJoint _joint;
        private Rigidbody _rb;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _joint = GetComponent<ConfigurableJoint>();

            if (_joint)
            {
                SoftJointLimit limit = _joint.linearLimit;
                limit.limit = _ropeLength;

                _joint.linearLimit = limit;
            }
        }

        public override void OnNetworkSpawn()
        {
            _currentHP.OnValueChanged += OnHpChange;
            
            if(IsServer)
                _currentHP.Value = _baseHp;

            _rope.gameObject.SetActive(true);
            
            if (_rope && _ropePositionStart)
            {
                _rope.SetPosition(0, _ropePositionStart.localPosition);
                _rope.SetPosition(1, transform.localPosition);
            }
        }

        private void Update()
        {
            _rope.SetPosition(1, transform.localPosition);
        }
        

        private void OnHpChange(float previousValue, float newValue)
        { }

        public void ApplyDamage(float damage, NetworkObject source = null)
        {
            if (!IsServer) return;
            
            _currentHP.Value -= damage;

            if (_rb)
            {
                if (source)
                {
                    Vector3 direction = transform.position - source.transform.position;
                    
                    Vector3 finalForce = direction.normalized * _randomForceRange;
                    
                    _rb.AddForce(finalForce, ForceMode.Impulse);
                }
                
            }
            
            if (_currentHP.Value <= 0)
            {
                NetworkObject n = Instantiate(_itemPickupDrop, transform.position, Quaternion.identity).NetworkObject;
                n?.Spawn();

                DropEventClientRpc();
                
                NetworkObject.Despawn();
                
                return;
            }
            
            ApplyHitEventClientRpc();
            
            
        }

        [Rpc(SendTo.Everyone)]
        private void DropEventClientRpc()
        {
            _dropEvent?.Invoke();
        }

        [Rpc(SendTo.Everyone)]
        private void ApplyHitEventClientRpc()
        {
            _hitEvent?.Invoke();
        }
    }
}