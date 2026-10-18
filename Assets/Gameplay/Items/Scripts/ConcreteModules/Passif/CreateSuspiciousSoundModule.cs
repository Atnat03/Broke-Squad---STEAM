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
        
        private Vector3 _lastVelocity;
        
        public void OnCollide(Collision collision, ItemPickup item)
        {
            float speed = Mathf.Abs(Vector3.Dot(_lastVelocity, collision.GetContact(0).normal));
            
            if (speed <= _miniVelocityToDoSound) return;
            
            item.CreateSuspiciousSound(
                item.OwnerClientId, 
                _suspiciousSoundSettings.Force,
                _suspiciousSoundSettings.MaxDistance);
        }

        public void OnThrow(Rigidbody rb)
        { }
        
        public void OnFixedUpdate(Rigidbody rb)
        {
            _lastVelocity = rb.linearVelocity;
        }

        public void OnStartHolding()
        { }

        public void OnStopHolding()
        { }
    }
}