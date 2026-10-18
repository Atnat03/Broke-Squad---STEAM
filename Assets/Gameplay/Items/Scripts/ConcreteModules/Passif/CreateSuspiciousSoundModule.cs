using System;
using Gameplay.Other.SuspiciousSound;
using MyPrint;
using UnityEngine;

namespace Gameplay.Items
{
    [Serializable]
    public class CreateSuspiciousSoundModule : ItemModule, IPassif
    {
        [SerializeField] private float _miniVelocityToDoSound = 1;
        [SerializeField] private SO_SuspiciousSoundSettings _suspiciousSoundSettings;
        
        private Rigidbody _rb;
        
        public void OnCollide(Collision collision, ItemPickup item)
        {
            if (_rb == null && !item.TryGetComponent(out _rb)) return;

            float speed = collision.impulse.magnitude / _rb.mass;
            
            if (speed <= _miniVelocityToDoSound) return;
            
            item.CreateSuspiciousSound(
                item.OwnerClientId, 
                _suspiciousSoundSettings.Force,
                _suspiciousSoundSettings.MaxDistance);
        }

        public void OnThrow(Rigidbody rb)
        {
            _rb = rb;
        }
        
        public void OnStartHolding()
        { }

        public void OnStopHolding()
        { }
    }
}