using System;
using Gameplay.IA.Scripts;
using UnityEngine;

namespace Gameplay.Items
{
    [Serializable]
    public class StunModule : ItemModule, IPassif
    {
        [SerializeField] private float _minVelocityForStun = 2;
        [SerializeField] private float _stunDuration = 1f;
        
        private Rigidbody _rb;
        
        public void OnCollide(Collision collision, ItemPickup item)
        {
            if (_rb == null && !item.TryGetComponent(out _rb)) return;

            float speed = collision.impulse.magnitude / _rb.mass;
            
            if(speed < _minVelocityForStun) return;
            
            if (collision.gameObject.TryGetComponent<IStunnable>(out var stun))
            {
                stun.ApplyStun(_stunDuration);
            }        
        }

        public void OnThrow(Rigidbody rb)
        {
            _rb = rb;
        }
        public void OnStartHolding() { }
        public void OnStopHolding() { }
    }
}