using MyPrint;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.UIElements;

namespace Gameplay.Equipment
{
    public class BackPackPhysics : NetworkBehaviour
    {
        public bool IsCarried => _isCarried.Value;
        
        [SerializeField] private Rigidbody _rb;
 
        [SerializeField] private Collider[] _colliders;
        private readonly NetworkVariable<bool> _isCarried = new NetworkVariable<bool>(false);

        NetworkTransform _nt;
        
        public override void OnNetworkSpawn()
        {
            _nt = GetComponent<NetworkTransform>();
            _isCarried.OnValueChanged += OnCarriedChanged;
            ApplyPhysics(_isCarried.Value);
        }
        
        public override void OnNetworkDespawn()
        {
            _isCarried.OnValueChanged -= OnCarriedChanged;
        }

        public void SetCarried(bool carried)
        {
            if (!IsServer)
                return;

            _isCarried.Value = carried;
            ApplyPhysics(carried);
        }

        private void OnCarriedChanged(bool previous, bool current)
        {
            ApplyPhysics(current);
        }

        private void ApplyPhysics(bool carried)
        {
            if (_rb != null) _rb.isKinematic = carried;

            foreach (Collider col in _colliders)
                if (col != null) col.enabled = !carried;

            if (_nt != null) _nt.enabled = !carried;
        }
        

        public void DropAt(Vector3 pos)
        {
            if (!IsServer) return;
            
            SetCarried(false);

            transform.position = pos;
            _nt.Teleport(pos, transform.rotation, transform.localScale);

            if (_rb != null)
            {
                _rb.linearVelocity = Vector3.zero;
                _rb.angularVelocity = Vector3.zero;
            }
        }
    }
}